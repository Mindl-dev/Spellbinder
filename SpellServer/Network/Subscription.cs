using SpellServer.Properties;
using System;
using System.Data;
using System.Drawing;
using System.Globalization;

namespace SpellServer
{
    public static class Subscription
    {
        public enum ErrorType
        {
            None,
            InvalidPassword,
            InvalidAccount,
            ServerLocked,
            UnknownError,
            InvalidVersion,
            AccessError,
            AccountDoesNotExist,
            ServerFull,
            NoMagestormAccess,
            BannedComputer,
            LoggedIn,
        }

        public static readonly String SubscriptionPage;
        public static readonly Byte[] GameVersion;

        static Subscription()
        {
            SubscriptionPage = String.Format("https://{0}/subscription.php", Settings.Default.SubscriptionHost);
            GameVersion = new[] { Convert.ToByte(49), Convert.ToByte(Settings.Default.ServerVersion.Split('.')[0]), Convert.ToByte(Settings.Default.ServerVersion.Split('.')[1]), Convert.ToByte(Settings.Default.ServerVersion.Split('.')[2]) };
        }

        // ================================================================
        // Data bag — no logic, just holds credential lookup results
        // ================================================================

        public class AccountData
        {
            public Int32 AccountId;
            public ErrorType Error;
            public AdminLevel Admin;
            public String Username;
            public Boolean MagestormPlus;
        }

        // ================================================================
        // Individual checks — each testable independently
        // ================================================================

        /// <summary>Look up account credentials from the database.</summary>
        public static AccountData ValidateCredentials(string username, string password)
        {
            var result = new AccountData
            {
                AccountId = 0,
                Username = "",
                Admin = AdminLevel.None,
                Error = ErrorType.AccessError,
            };

            try
            {
                DataTable query = MySQL.Accounts.GetAccountData(username);
                DataRow row = query.Rows[0];

                if (PasswordHasher.Verify(password, row["password"].ToString()))
                {
                    if ((int)row["AccountID"] > 0)
                    {
                        result.AccountId = (int)row["AccountID"];
                        result.Username = row["username"].ToString();
                        result.Admin = (AdminLevel)row["Admin"];
                        result.Error = ErrorType.None;
                    }
                    else
                    {
                        result.Error = ErrorType.InvalidAccount;
                    }
                }
                else
                {
                    result.Error = ErrorType.InvalidPassword;
                }
            }
            catch (Exception)
            {
                result.Error = ErrorType.AccessError;
            }

            return result;
        }

        /// <summary>Kick any existing session with the same account ID and remove from player list.</summary>
        public static void KickGhostSessions(Player newPlayer, int accountId, PlayerManager players)
        {
            Player ghost = players.FindByAccountId(accountId);
            if (ghost != null && ghost != newPlayer)
            {
                // Clean up DB state before removing — prevents "Duplicate entry" on reconnect
                try { MySQL.OnlineAccounts.SetOffline(ghost.AccountId); } catch { }
                if (ghost.ActiveCharacter != null)
                    try { MySQL.OnlineCharacters.SetOffline(ghost.ActiveCharacter.CharacterId); } catch { }

                // Flag for disconnect and close socket — don't call Network.Disconnect
                // as it calls Arena.PlayerLeft which acquires lock(SyncRoot),
                // deadlocking if the arena thread holds it. Socket close will cause
                // the ghost's ProcessReceive to error out and clean up arena state.
                ghost.DisconnectReason = Resources.Strings_Disconnect.MultipleLogin;
                ghost.Disconnect = true;
                try { ghost.TcpClient.Client.Close(); } catch { }
                players.Remove(ghost);
            }
        }

        /// <summary>Check if another player with the same hardware serial is already connected.</summary>
        public static ErrorType CheckMultibox(string serial, AdminLevel newPlayerAdmin, PlayerManager players)
        {
            if (serial == "Not_Found" || serial == "VMWare" || serial == "VirtualPC")
                return ErrorType.None;

            Player connectedPlayer = players.FindBySerial(serial);
            if (connectedPlayer != null)
            {
                if (!connectedPlayer.IsAdmin && newPlayerAdmin == AdminLevel.None)
                    return ErrorType.LoggedIn;
            }
            return ErrorType.None;
        }

        /// <summary>Check if the server is locked to non-admins.</summary>
        public static ErrorType CheckServerLock(bool isLocked, AdminLevel admin)
        {
            if (isLocked && admin == AdminLevel.None)
                return ErrorType.ServerLocked;
            return ErrorType.None;
        }

        /// <summary>Check if the server is full for non-premium non-admin users.</summary>
        public static ErrorType CheckServerFull(int freePlayerCount, AdminLevel admin, bool isMagestormPlus)
        {
            if (freePlayerCount > 100 && !isMagestormPlus && admin == AdminLevel.None)
                return ErrorType.ServerFull;
            return ErrorType.None;
        }

        // ================================================================
        // Orchestrator — calls each check in order, fails fast
        // ================================================================

        private static void RejectLogin(Player player, ErrorType error, string serial, string username)
        {
            Program.Log(String.Format("(PID: {0}, IP: {1}, S/N: {2}) Login Error: {3}, Username: {4}",
                player.PlayerId, player.IpAddress, serial, error, username), Color.DarkOrange);

            Network.Send(player, GamePacket.Outgoing.Login.Error(error));
            player.DisconnectReason = Resources.Strings_Disconnect.AuthenticationError;
            player.Disconnect = true;
        }

        public static void Authenticate(Player player, String username, String password, String serial, Byte[] version)
        {
            // 1. Validate credentials against database
            AccountData creds = ValidateCredentials(username, password);
            if (creds.Error != ErrorType.None) { RejectLogin(player, creds.Error, serial, username); return; }

            // 2. Server capacity check
            ErrorType fullError = CheckServerFull(PlayerManager.Players.GetFreePlayerCount(), creds.Admin, creds.MagestormPlus);
            if (fullError != ErrorType.None) { RejectLogin(player, fullError, serial, username); return; }

            // 3. Kick ghost sessions (removes from player list before serial check)
            KickGhostSessions(player, creds.AccountId, PlayerManager.Players);

            // 4. Multibox check (same hardware serial)
            ErrorType multiboxError = CheckMultibox(serial, creds.Admin, PlayerManager.Players);
            if (multiboxError != ErrorType.None) { RejectLogin(player, multiboxError, serial, username); return; }

            // 5. Ban check
            if (MySQL.BannedSerials.IsBanned(serial)) { RejectLogin(player, ErrorType.BannedComputer, serial, username); return; }

            // 6. Server lock check
            ErrorType lockError = CheckServerLock(Settings.Default.Locked, creds.Admin);
            if (lockError != ErrorType.None) { RejectLogin(player, lockError, serial, username); return; }

            // All checks passed — apply login
            player.AccountId = creds.AccountId;
            player.Serial = serial;
            player.Username = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(creds.Username);
            player.Admin = creds.Admin;
            player.Flags |= creds.MagestormPlus ? PlayerFlag.MagestormPlus : PlayerFlag.None;

            if (BitConverter.ToInt32(version, 0) != BitConverter.ToInt32(GameVersion, 0))
            {
                Program.Log(String.Format("(PID: {0}, AID: {1}) {2} version mismatch: client={3} server={4}, allowing.",
                    player.PlayerId, player.AccountId, player.Username,
                    BitConverter.ToString(version), BitConverter.ToString(GameVersion)), Color.DarkOrange);
            }

            Network.Send(player, GamePacket.Outgoing.Login.Connected(player));
            Network.Send(player, GamePacket.Outgoing.Player.SendPlayerId(player));

            MySQL.OnlineAccounts.SetOnline(player.AccountId, player.Username);

            Program.Log(String.Format("(PID: {0}, AID: {1}, S/N: {2}) {3} has connected.",
                player.PlayerId, player.AccountId, serial, player.Username), Color.MediumSlateBlue);
        }
    }
}
