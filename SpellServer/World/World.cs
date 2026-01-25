using Helper;
using Helper.Timing;
using SpellServer.Properties;
using SpellServer.Sound;
using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;

namespace SpellServer
{
    public enum ChatType
    {
        All,
        Dead,
        Team,
        Whisper,
    }

    public static class World
    {
        
        public static void SendSystemMessage(Player player, String message)
        {
            Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, String.Format("[System] {0}", message)));
        }

        public static void ProcessChatMessage(Player player, Int16 target, ChatType targetType, String message, bool UDP = false)
        {
            if (player.IsInArena)
            {
                player.ActiveArenaPlayer.IsAwayFromKeyboard = false;
            }

            if (ParseGameCommand(player, targetType, message)) return;

            if (player.Flags.HasFlag(PlayerFlag.Muted) && targetType != ChatType.Whisper)
            {
                SendSystemMessage(player, Resources.Strings_Common.Muted);
                return;
            }

            switch (targetType)
            {
                case ChatType.All:
                {
                    if (player.Flags.HasFlag(PlayerFlag.ChatDisabled))
                    {
                        SendSystemMessage(player, Resources.Strings_Common.ChatDisabled);
                        return;
                    }

                    if (player.Flags.HasFlag(PlayerFlag.Hidden))
                    {
                        if (player.IsInArena)
                        {
                            for (Int32 i = 0; i < player.ActiveArena.ArenaPlayers.Count; i++)
                            {
                                Player chatPlayer = player.ActiveArena.ArenaPlayers[i].WorldPlayer;
                                if (chatPlayer == player) continue;

                                if (!chatPlayer.Flags.HasFlag(PlayerFlag.ChatDisabled) || player.IsAdmin)
                                {
                                    Network.Send(chatPlayer, GamePacket.Outgoing.System.DirectTextMessage(player, String.Format("{0}: {1}", player.ActiveCharacter.Name, message)));
                                }
                            }
                        }
                        else
                        {
                            for (Int32 i = 0; i < PlayerManager.Players.Count; i++)
                            {
                                Player chatPlayer = PlayerManager.Players[i];
                                if (player == chatPlayer || player.TableId != chatPlayer.TableId) continue;

                                if (!chatPlayer.Flags.HasFlag(PlayerFlag.ChatDisabled) || player.IsAdmin)
                                {
                                    Network.Send(chatPlayer, GamePacket.Outgoing.System.DirectTextMessage(player, String.Format("{0}: {1}", player.ActiveCharacter.Name, message)));
                                }
                            }
                        }
                    }
                    else
                    {
                        if (player.TableId == 255)
                        {
                            Int32 playerLevel = 0;

                            if (player.Flags.HasFlag(PlayerFlag.MagestormPlus))
                            {
                                playerLevel = 1;
                            }

                            if (player.IsAdmin)
                            {
                                playerLevel = 2;
                            }

                            WebChat.QueueWebChatMessage(new WebChat.WebChatMessage(player.ActiveCharacter.Name, message, playerLevel, player.AccountId, DateTime.Now.GetUnixTime()));
                        }

                        if (player.IsInArena)
                        {
                            for (Int32 i = 0; i < player.ActiveArena.ArenaPlayers.Count; i++)
                            {
                                Player chatPlayer = player.ActiveArena.ArenaPlayers[i].WorldPlayer;
                                if (chatPlayer == player) continue;

                                if (!chatPlayer.Flags.HasFlag(PlayerFlag.ChatDisabled) || player.IsAdmin)
                                {
                                    Network.Send(chatPlayer, GamePacket.Outgoing.Player.Chat(player, target, targetType, message, UDP));
                                }
                            }
                        }
                        else
                        {
                            for (Int32 i = 0; i < PlayerManager.Players.Count; i++)
                            {
                                Player chatPlayer = PlayerManager.Players[i];
                                if (chatPlayer == player || player.TableId != chatPlayer.TableId) continue;

                                if (!chatPlayer.Flags.HasFlag(PlayerFlag.ChatDisabled) || player.IsAdmin)
                                {
                                    Network.Send(chatPlayer, GamePacket.Outgoing.Player.Chat(player, target, targetType, message, UDP));
                                }
                            }
                        }
                    }

                    Program.ServerForm.ChatLog.WriteMessage(String.Format("[{0}] ({1}[{2}]){3}: {4}", player.WorldLocationString, player.AccountId, player.ActiveCharacter.CharacterId, player.ActiveCharacter.Name, message), player.ChatColor);

                    break;
                }
                case ChatType.Dead:
                {
                    if (player.Flags.HasFlag(PlayerFlag.ChatDisabled))
                    {
                        SendSystemMessage(player, Resources.Strings_Common.ChatDisabled);
                        return;
                    }

                    if (player.IsInArena)
                    {
                        for (Int32 i = 0; i < player.ActiveArena.ArenaPlayers.Count; i++)
                        {
                            Player chatPlayer = player.ActiveArena.ArenaPlayers[i].WorldPlayer;
                            if (chatPlayer == player) continue;

                            if (!chatPlayer.Flags.HasFlag(PlayerFlag.ChatDisabled) || player.IsAdmin)
                            {
                                Network.Send(chatPlayer, GamePacket.Outgoing.Player.Chat(player, target, targetType, message, UDP));
                            }
                        }
                    }
                    
                    Program.ServerForm.ChatLog.WriteMessage(String.Format("[{0}] ({1}[{2}]){3}: {4}", player.WorldLocationString, player.AccountId, player.ActiveCharacter.CharacterId, player.ActiveCharacter.Name, message), player.ChatColor);
                    break;
                }
                case ChatType.Team:
	            {
		            Team targetTeam = (Team) target;

                    if (player.Flags.HasFlag(PlayerFlag.ChatDisabled))
                    {
                        SendSystemMessage(player, Resources.Strings_Common.ChatDisabled);
                        return;
                    }

                    if (player.IsInArena)
                    {
                        for (Int32 i = 0; i < player.ActiveArena.ArenaPlayers.Count; i++)
                        {
                            Player chatPlayer = player.ActiveArena.ArenaPlayers[i].WorldPlayer;
                            if (chatPlayer == player) continue;

							if ((!chatPlayer.Flags.HasFlag(PlayerFlag.ChatDisabled) || player.IsAdmin) && chatPlayer.ActiveTeam == targetTeam)
                            {
                                Network.Send(chatPlayer, GamePacket.Outgoing.Player.Chat(player, target, targetType, message, UDP));
                            }
                        }
                    }

                    Program.ServerForm.ChatLog.WriteMessage(String.Format("[{0}] ({1}[{2}]){3}: {4}", player.WorldLocationString, player.AccountId, player.ActiveCharacter.CharacterId, player.ActiveCharacter.Name, message), player.ChatColor);
                    break;
                }
                case ChatType.Whisper:
                {
                    Player targetPlayer;

                    if (player.ActiveArena == null)
                    {
                        targetPlayer = PlayerManager.Players.FindById(target);
                    }
                    else
                    {
                        ArenaPlayer arenaPlayer = player.ActiveArena.ArenaPlayers.FindById((Byte)target);
                        if (arenaPlayer == null) return;

                        target = arenaPlayer.ArenaPlayerId;
                        targetPlayer = arenaPlayer.WorldPlayer;
                    }

                    if (targetPlayer != null)
                    {
                        if (targetPlayer.ActiveArena != null && player.ActiveArena == null)
                        {
                            Network.Send(targetPlayer, GamePacket.Outgoing.Player.Chat(null, target, targetType, message, UDP));
                        }
                        else
                        {
                            Network.Send(targetPlayer, GamePacket.Outgoing.Player.Chat(player, target, targetType, message, UDP));
                        }

                        Program.ServerForm.WhisperLog.WriteMessage(String.Format("[Whisper] ({0}[{1}]){2} -> ({3}[{4}]){5}: {6}", player.AccountId, player.ActiveCharacter.CharacterId, player.ActiveCharacter.Name, targetPlayer.AccountId, targetPlayer.ActiveCharacter.CharacterId, targetPlayer.ActiveCharacter.Name, message), player.ChatColor);
                    }

                    break;
                }
            }
        }
        
        private static Boolean ParseGameCommand(Player player, ChatType targetType, String message, bool UDP = false)
        {
            if (targetType == ChatType.Whisper) return false;

            ChatCommand cmd = new ChatCommand(message);
            if (cmd.Command == null) return false;

            // Generic Administrator Commands
            if (player.ActiveCharacter.OpLevel > 1)
            {
                switch (cmd.Command)
                {
                    case "eventexp":
                    {
                        if (cmd.Arguments.Count < 1)
                        {
                            SendSystemMessage(player, Resources.Strings_Commands.EventExp_SpecifyAmount);
                            return true;
                        }

                        Int32 amount;

                        try
                        {
                            amount = Convert.ToInt32(cmd.Arguments[0]);
                        }
                        catch (Exception)
                        {
                            SendSystemMessage(player, Resources.Strings_Commands.EventExp_InvalidAmount);
                            return true;
                        }

                        if (amount <= 0 || amount > 100000)
                        {
                            SendSystemMessage(player, Resources.Strings_Commands.EventExp_InvalidAmount);
                            return true;
                        }

                        player.PreferredEventExp = amount;

                        SendSystemMessage(player, String.Format(Resources.Strings_Commands.EventExp_Success, player.PreferredEventExp));

                        return true;
                    }
                    case "broadcast":
                    case "b":
                    {
                        if (cmd.Arguments.Count < 1)
                        {
                            SendSystemMessage(player, Resources.Strings_Commands.Broadcast_TooShort);
                            return true;
                        }

                        String broadcastMessage = cmd.GetStringFromArgs(0);

                        if (broadcastMessage.Length <= 0)
                        {
                            SendSystemMessage(player, Resources.Strings_Commands.Broadcast_TooShort);
                            return true;
                        }

                        WebChat.QueueWebChatMessage(new WebChat.WebChatMessage(String.Format("[Broadcast] {0}", player.ActiveCharacter.Name), broadcastMessage, 2, player.AccountId, DateTime.Now.GetUnixTime()));

                        Network.SendTo(GamePacket.Outgoing.System.DirectTextMessage(player, String.Format("[Broadcast] {0}: {1}", player.ActiveCharacter.Name, broadcastMessage)), Network.SendToType.All);
                        
                        Program.ServerForm.ChatLog.WriteMessage(String.Format("[Broadcast] ({0}[{1}]){2}: {3}", player.AccountId, player.ActiveCharacter.CharacterId, player.ActiveCharacter.Name, broadcastMessage), player.ChatColor);
                        
                        return true;
                    }
                    case "motd":
                    {
                        if (cmd.Arguments.Count < 1)
                        {
                            SendSystemMessage(player, Resources.Strings_Commands.Motd_TooShort);
                            return true;
                        }

                        String motdMessage = cmd.GetStringFromArgs(0);

                        if (motdMessage.Length <= 0)
                        {
                            SendSystemMessage(player, Resources.Strings_Commands.Motd_TooShort);
                            return true;
                        }

	                    Settings.Default.MessageOfTheDay = motdMessage;
						Settings.Default.Save();

						SendSystemMessage(player, String.Format(Resources.Strings_Commands.Motd_Success, motdMessage));

						Program.ServerForm.AdminLog.WriteMessage(String.Format("[Admin] ({0}[{1}]){2} -> Set MOTD to \"{3}\" ", player.AccountId, player.ActiveCharacter.CharacterId, player.ActiveCharacter.Name, motdMessage), Color.Blue);
                        
                        return true;
                    }
                    case "kill":
                    {
                        if (cmd.Arguments.Count < 1)
                        {
                            SendSystemMessage(player, Resources.Strings_Commands.Kill_SpecifyPlayer);
                            return true;
                        }

                        String targetToken = cmd.Arguments[0];

                        if (player.IsInArena)
                        {
                            ListCollection<ArenaPlayer> targetArenaPlayers = player.ActiveArena.ArenaPlayers.FindArenaPlayers(targetToken);

                            if (targetArenaPlayers.Count > 0)
                            {
                                foreach (ArenaPlayer targetArenaPlayer in targetArenaPlayers)
                                {
                                    if (!targetArenaPlayer.IsAlive) continue;

                                    if (player.Admin < targetArenaPlayer.WorldPlayer.Admin)
                                    {
                                        SendSystemMessage(player, Resources.Strings_Commands.Kill_HigherPriviledges);
                                        return true;
                                    }

                                    player.ActiveArena.AdminKillPlayer(targetArenaPlayer);

                                    SendSystemMessage(targetArenaPlayer.WorldPlayer, Resources.Strings_Commands.Kill_Killed);
                                    SendSystemMessage(player, String.Format(Resources.Strings_Commands.Kill_TargetKilled, targetArenaPlayer.ActiveCharacter.Name));

                                    Program.ServerForm.AdminLog.WriteMessage(String.Format("[Admin] ({0}[{1}]){2} -> {3} has been killed.", player.AccountId, player.ActiveCharacter.CharacterId, player.ActiveCharacter.Name, targetArenaPlayer.ActiveCharacter.Name), Color.Blue);
                                }
                            }
                            else
                            {
                                SendSystemMessage(player, Resources.Strings_Commands.General_NoTargetsFound);
                            }
                        }
                        else
                        {
                            SendSystemMessage(player, Resources.Strings_Commands.General_NotInArena);
                        }

                        return true;
                    }
                    case "raise":
                    {
                        if (cmd.Arguments.Count < 1)
                        {
                            Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, "[System] Invalid syntax. Usage: !raise <targetname>"));
                            return true;
                        }

                        String targetToken = cmd.Arguments[0];

                        if (player.IsInArena)
                        {
                            ListCollection<ArenaPlayer> targetArenaPlayers = player.ActiveArena.ArenaPlayers.FindArenaPlayers(targetToken);

                            if (targetArenaPlayers.Count > 0)
                            {
                                foreach (ArenaPlayer targetArenaPlayer in targetArenaPlayers)
                                {
                                    if (targetArenaPlayer.IsAlive) continue;

                                    player.ActiveArena.AdminRaisePlayer(targetArenaPlayer);

                                    Network.Send(targetArenaPlayer.WorldPlayer, GamePacket.Outgoing.System.DirectTextMessage(targetArenaPlayer.WorldPlayer, "[System] You were raised by a Moderator."));
                                    Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, String.Format("[System] {0} has been raised.", targetArenaPlayer.ActiveCharacter.Name)));

                                    Program.ServerForm.AdminLog.WriteMessage(String.Format("[Admin] ({0}[{1}]){2} -> {3} has been raised.", player.AccountId, player.ActiveCharacter.CharacterId, player.ActiveCharacter.Name, targetArenaPlayer.ActiveCharacter.Name), Color.Blue);
                                }
                            }
                            else
                            {
                                SendSystemMessage(player, Resources.Strings_Commands.General_NoTargetsFound);
                            }
                        }
                        else
                        {
                            SendSystemMessage(player, Resources.Strings_Commands.General_NotInArena);
                        }

                        return true;
                    }
                    case "kick":
                    {
                        if (cmd.Arguments.Count < 1)
                        {
                            Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, "[System] Invalid syntax. Usage: !kick <playername>"));
                            return true;
                        }

                        String playerName = cmd.Arguments[0];
                        String kickReason = cmd.Arguments.Count > 1 ? cmd.GetStringFromArgs(1) : "Kicked by an Administrator";

                        lock (PlayerManager.Players.SyncRoot)
                        {
                            Player targetPlayer = PlayerManager.Players.FindByCharacterName(playerName) ?? PlayerManager.Players.FindByUsername(playerName);

                            if (targetPlayer != null)
                            {
                                if (player.Admin < targetPlayer.Admin)
                                {
                                    Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, "[System] You cannot kick an admin with higher privileges."));
                                    return true;
                                }

                                TableManager.Tables.ClearSavedInvites(targetPlayer);

                                targetPlayer.DisconnectReason = kickReason;
                                targetPlayer.Disconnect = true;

                                Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, String.Format("[System] {0} has been kicked.", playerName)));

                                Program.ServerForm.AdminLog.WriteMessage(String.Format("[Admin] ({0}[{1}]){2} -> Kicked {3}.", player.AccountId, player.ActiveCharacter.CharacterId, player.ActiveCharacter.Name, targetPlayer.ActiveCharacter.Name), Color.Blue);
                            }
                            else
                            {
                                Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, String.Format("[System] {0} was not found.", playerName)));
                            }
                        }

                        return true;
                    }
                    case "rename":
                    {
                        if (cmd.Arguments.Count < 2)
                        {
                            Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, "[System] Invalid syntax. Usage: !rename <targetname> <newname>"));
                            return true;
                        }

                        String playerName = cmd.Arguments[0];
                        String newName = cmd.Arguments[1];

                        if (newName.Length < 3 || newName.Length > 11)
                        {
                            Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, "[System] Invalid name length."));
                            return true;
                        }

                        lock (PlayerManager.Players.SyncRoot)
                        {
                            Player targetPlayer = PlayerManager.Players.FindByCharacterName(playerName);

                            if (targetPlayer != null)
                            {
                                playerName = targetPlayer.ActiveCharacter.Name;

                                if (!targetPlayer.IsInArena)
                                {
                                    if (Character.IsNameTaken(newName))
                                    {
                                        Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, "[System] Unable to rename player.  That name is taken."));
                                        return true;
                                    }

                                    if (Character.IsNameValid(newName, true))
                                    {
                                        Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, "[System] Unable to rename player.  That name is invalid."));
                                        return true;
                                    }

                                    targetPlayer.ActiveCharacter.Name = newName;

                                    Character.Save(targetPlayer, null);

                                    Network.SendTo(targetPlayer, GamePacket.Outgoing.World.PlayerLeave(targetPlayer, UDP), Network.SendToType.Tavern, false);
                                    Network.SendTo(targetPlayer, GamePacket.Outgoing.World.PlayerJoin(targetPlayer, UDP), Network.SendToType.Tavern, false);

                                    Network.Send(targetPlayer, GamePacket.Outgoing.System.DirectTextMessage(targetPlayer, String.Format("[System] You have been renamed to {0}. You must go to the Study for player to take effect.", newName)));

                                    Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, "[System] Player renamed successfully."));

                                    Program.ServerForm.AdminLog.WriteMessage(String.Format("[Admin] ({0}[{1}]){2} -> Renamed [{3}]{4} to [{3}]{5}.", player.AccountId, player.ActiveCharacter.CharacterId, player.ActiveCharacter.Name, targetPlayer.ActiveCharacter.CharacterId, playerName, newName), Color.Blue);
                                }
                                else
                                {
                                    Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, String.Format("[System] {0} is in an Arena.", playerName)));
                                }
                            }
                            else
                            {
                                Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, String.Format("[System] {0} was not found.", playerName)));
                            }
                        }

                        return true;
                    }
                    case "exp":
                    case "giveexp":
                    {
                        if (cmd.Arguments.Count < 2)
                        {
                            Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, "[System] Invalid syntax. Usage: !giveexp <targetname> <amount>"));
                            return true;
                        }

                        String playerName;
                        Int32 amount;

                        try
                        {
                            playerName = cmd.Arguments[0];
                            amount = Convert.ToInt32(cmd.Arguments[1]);
                        }
                        catch (Exception)
                        {
                            Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, "[System] Invalid syntax. Usage: !giveexp <targetname> <amount>"));
                            return true;
                        }

                        lock (PlayerManager.Players.SyncRoot)
                        {
                            Player targetPlayer = PlayerManager.Players.FindByCharacterName(playerName);

                            if (targetPlayer != null)
                            {
                                if (!targetPlayer.IsInArena)
                                {
                                    if (amount > 0)
                                    {
                                        targetPlayer.ActiveCharacter.AwardExp += amount;

                                        Network.Send(targetPlayer, GamePacket.Outgoing.System.DirectTextMessage(targetPlayer, String.Format("[System] You have been awarded {0} experience.", amount)));
                                        Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, String.Format("[System] You have awarded {0} {1} experience.", targetPlayer.ActiveCharacter.Name, amount)));

                                        Program.ServerForm.AdminLog.WriteMessage(String.Format("[Admin] ({0}[{1}]){2} -> Gave [{3}]{4} {5} experience.", player.AccountId, player.ActiveCharacter.CharacterId, player.ActiveCharacter.Name, targetPlayer.ActiveCharacter.CharacterId, targetPlayer.ActiveCharacter.Name, amount), Color.Blue);
                                    }
                                    else
                                    {
                                        Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, "[System] Invalid amount."));
                                    }
                                }
                                else
                                {
                                    Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, String.Format("[System] {0} is in an Arena.", playerName)));
                                }
                            }
                            else
                            {
                                Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, String.Format("[System] {0} was not found.", playerName)));
                            }
                        }

                        return true;
                    }
                    case "info":
                    {
                        if (cmd.Arguments.Count < 1)
                        {
                            Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, "[System] Invalid syntax. Usage: !info <targetname>"));
                            return true;
                        }

                        String playerName = cmd.Arguments[0];

                        lock (PlayerManager.Players.SyncRoot)
                        {
                            Player targetPlayer = PlayerManager.Players.FindByCharacterName(playerName);

                            if (targetPlayer != null)
                            {
                                Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, String.Format("________Player Information________")));
                                Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, String.Format("Account ID: {0}, Character ID: {1}, Player ID: {2}", targetPlayer.AccountId, targetPlayer.ActiveCharacter.CharacterId, targetPlayer.PlayerId)));
                                Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, String.Format("Account Name: {0}, Character Name: {1}", targetPlayer.Username, targetPlayer.ActiveCharacter.Name)));
                                Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, String.Format("Arena ID: {0}, Table ID: {1}", targetPlayer.ActiveArena != null ? targetPlayer.ActiveArena.ArenaId : 0, targetPlayer.TableId)));
                                
                                if (player.Admin >= AdminLevel.Staff)
                                {
                                    Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, String.Format("IP: {0}, Serial: {1}", targetPlayer.IpAddress, targetPlayer.Serial)));
                                }

                                Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, String.Format("Admin Level: {0}, Ping: {1}ms", targetPlayer.Admin, targetPlayer.Ping)));
                                
                                Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, String.Format("Flags: {0}", targetPlayer.Flags)));

                                Program.ServerForm.AdminLog.WriteMessage(String.Format("[Admin] ({0}){1} -> Retrieved information on {2}", player.AccountId, player.ActiveCharacter.Name, targetPlayer.Username), Color.Blue);
                            }
                            else
                            {
                                Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, String.Format("[System] {0} was not found.", playerName)));
                            }
                        }

                        return true;
                    }
                }
            }

            Program.ServerForm.MainLog.WriteMessage(String.Format("Character: {0}, OpLevel: {1}, AdminLevel: {2}, command: {3}", player.ActiveCharacter.Name, player.ActiveCharacter.OpLevel, player.Admin.ToString(), cmd.Command), Color.Blue);

            // Staff Commands and Developer Only Commands
            if (player.ActiveCharacter.OpLevel >= 3)
            {
                switch (cmd.Command)
                {
                    case "respec":
                    {
                        if (cmd.Arguments.Count < 1)
                        {
                            Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, "[System] Invalid syntax. Usage: !respec <targetname>"));
                            return true;
                        }

                        String playerName = cmd.Arguments[0];

                        lock (PlayerManager.Players.SyncRoot)
                        {
                            Player targetPlayer = PlayerManager.Players.FindByCharacterName(playerName);

                            if (targetPlayer != null)
                            {
                                if (!targetPlayer.IsInArena)
                                {
                                    targetPlayer.ActiveCharacter.PendingFlags |= Character.PendingFlag.ListReset;

                                    Network.Send(targetPlayer, GamePacket.Outgoing.System.DirectTextMessage(targetPlayer, "[System] Your spell lists have been reset. You must go to the Study for it to take effect."));
                                    Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, String.Format("[System] {0}'s spell lists have been reset successfully.", targetPlayer.ActiveCharacter.Name)));

                                    Program.ServerForm.AdminLog.WriteMessage(String.Format("[Admin] ({0}){1} -> Reset {2}'s spell lists.", player.AccountId, player.ActiveCharacter.Name, targetPlayer.ActiveCharacter.Name), Color.Blue);
                                }
                                else
                                {
                                    Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, String.Format("[System] {0} is in an Arena.", playerName)));
                                }
                            }
                            else
                            {
                                Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, String.Format("[System] {0} was not found.", playerName)));
                            }
                        }

                        return true;
                    }
                    case "hidden":
                    {
                        bool targetState;

                        if (cmd.Arguments.Count < 1)
                        {
                            // Toggle logic
                            targetState = !player.Flags.HasFlag(PlayerFlag.Hidden);
                        }
                        else
                        {
                            // Explicit logic
                            string arg = cmd.Arguments[0].ToLower();
                            if (arg == "true" || arg == "on") targetState = true;
                            else if (arg == "false" || arg == "off") targetState = false;
                            else
                            {
                                SendSystemMessage(player, "[System] Invalid argument. Use: !hidden true/false");
                                return true;
                            }
                        }

                        player.Flags = targetState ? (player.Flags | PlayerFlag.Hidden) : (player.Flags & ~PlayerFlag.Hidden);

                        if (player.IsInArena)
                        {
                            var syncPacket = targetState ?
                                    GamePacket.Outgoing.Arena.PlayerLeave(player.ActiveArenaPlayer) :
                                    GamePacket.Outgoing.Arena.PlayerJoin(player.ActiveArenaPlayer);

                            lock (player.ActiveArena.ArenaPlayers.SyncRoot)
                            {
                                foreach (ArenaPlayer ap in player.ActiveArena.ArenaPlayers)
                                {
                                    // 1. Tell EVERYONE to spawn/despawn the Admin's 3D model
                                    if (ap.WorldPlayer != player)
                                        Network.Send(ap.WorldPlayer, syncPacket);

                                    // 2. Refresh EVERYONE'S player list so names appear/disappear from the UI
                                    // This "Pushes" the new list to each client
                                    UpdateAllArenaPlayers(ap.WorldPlayer);
                                }
                            }
                        }
                        else
                        {
                            var syncPacket = targetState ?
                                    GamePacket.Outgoing.World.PlayerLeave(player) :
                                    GamePacket.Outgoing.World.PlayerJoin(player);
                            
                            lock (PlayerManager.Players.SyncRoot)
                            {
                                foreach (Player p in PlayerManager.Players)
                                {
                                    // 1. Tell EVERYONE to spawn/despawn the Admin's 3D model
                                    if (p != player)
                                        Network.Send(p, syncPacket);

                                    // 2. Refresh EVERYONE'S player list so names appear/disappear from the UI
                                    // This "Pushes" the new list to each client
                                    UpdateAllPlayers(p);
                                } 
                            }
                        }

                        SendSystemMessage(player, $"[System] Stealth mode: {(targetState ? "ON" : "OFF")}");
                        return true;
                    }
                        /*if (targetState) // Enabling Stealth
                        {
                            player.Flags |= PlayerFlag.Hidden;
                                if (player.IsInArena)
                                {
                                    var syncPacket = targetState ?
                                        GamePacket.Outgoing.Arena.PlayerLeave(player.ActiveArenaPlayer) :
                                        GamePacket.Outgoing.Arena.PlayerJoin(player.ActiveArenaPlayer);
                                    
                                    lock (player.ActiveArena.ArenaPlayers.SyncRoot)
                                    {
                                        foreach (ArenaPlayer ap in player.ActiveArena.ArenaPlayers)
                                        {
                                            // 1. Tell EVERYONE to spawn/despawn the Admin's 3D model
                                            if (ap.WorldPlayer != player)
                                                Network.Send(ap.WorldPlayer, syncPacket);

                                            // 2. Refresh EVERYONE'S player list so names appear/disappear from the UI
                                            // This "Pushes" the new list to each client
                                            UpdateAllArenaPlayers(ap.WorldPlayer, false);
                                        }
                                    }

                                    Network.SendToArena(player.ActiveArenaPlayer, GamePacket.Outgoing.Arena.PlayerJoin(player.ActiveArenaPlayer), false);
                                    UpdateAllArenaPlayers(player, true);
                                    //Network.SendToArena(player.ActiveArenaPlayer, GamePacket.Outgoing.Arena.PlayerJoin(player.ActiveArenaPlayer), false);
                                    //Network.SendTo(player.ActiveArenaPlayer.WorldPlayer, GamePacket.Outgoing.World.PlayerJoin(player.ActiveArenaPlayer.WorldPlayer), Network.SendToType.Tavern, false);
                                }
                                else
                                {
                                    Network.SendTo(GamePacket.Outgoing.World.PlayerLeave(player), Network.SendToType.Tavern);
                                }

                                SendSystemMessage(player, "[System] Stealth mode: ON");
                        }
                        else // Disabling Stealth
                        {
                            player.Flags &= ~PlayerFlag.Hidden;
                            if (player.IsInArena)
                            {
                                    Network.SendToArena(player.ActiveArenaPlayer, GamePacket.Outgoing.Arena.PlayerLeave(player.ActiveArenaPlayer), false);
                                    UpdateAllArenaPlayers(player, false);
                                    //Network.SendTo(player.ActiveArenaPlayer.WorldPlayer, GamePacket.Outgoing.World.PlayerLeave(player.ActiveArenaPlayer.WorldPlayer), Network.SendToType.Tavern, false);
                                }
                            else
                            {
                                Network.SendTo(player, GamePacket.Outgoing.World.PlayerJoin(player), Network.SendToType.Tavern, false);
                            }

                            SendSystemMessage(player, "[System] Stealth mode: OFF");
                        }

                        return true;
                    }*/
                    case "pp":
                    {
                        if (cmd.Arguments.Count < 3)
                        {
                            Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, "[System] Invalid syntax. Usage: !poolpower <id> <power> <teamnumber>"));
                            break;
                        }

                        Int16 poolId = Convert.ToInt16(cmd.Arguments[0]);
                        Int16 poolPower = Convert.ToInt16(cmd.Arguments[1]);
                        Int16 poolTeam = Convert.ToInt16(cmd.Arguments[2]);

                        if (!player.IsInArena)
                        {
                            Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, "[System] You are not in an Arena."));
                            break;
                        }

                        Pool pool = player.ActiveArena.Grid.Pools.FindById(poolId);

                        if (pool == null)
                        {
                            Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, String.Format("[System] Pool ID {0} does not exist in player arena.", poolId)));
                            break;
                        }

                        if (poolTeam < 0 || poolTeam > 3)
                        {
                            Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, "[System] Pool Team must be between 0 and 3."));
                        }
                        else
                        {
                            if (poolPower < 1 || poolPower > 100)
                            {
                                Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, "[System] Pool Power must be between 1 and 100."));
                            }
                            else
                            {
                                lock (player.ActiveArena.SyncRoot)
                                {
                                    pool.CurrentBias = pool.MaxBias;
                                    pool.Power = poolPower;
                                    pool.Team = (Team)poolTeam;

                                    Network.SendTo(player.ActiveArena, GamePacket.Outgoing.Arena.BiasedPool(player.ActiveArenaPlayer, pool, 100, UDP), Network.SendToType.Arena);

                                    Program.ServerForm.AdminLog.WriteMessage(String.Format("[Admin] {{{0}}} {1} ({2}), Command: {3}", player.AccountId, player.Username, player.ActiveCharacter.Name, message), Color.Blue);
                                }
                            }
                        }

                        return true;
                    }
                }
            }

            // Developer Only Commands
            if (player.ActiveCharacter.OpLevel == 5)
            {
                switch (cmd.Command)
                {
                    case "lockserver":
                    {
						Settings.Default.Locked = !Settings.Default.Locked;

						Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, String.Format("[System] The server is {0} locked.", Settings.Default.Locked ? "now" : "no longer")));

                        Program.ServerForm.AdminLog.WriteMessage(String.Format("[Admin] ({0}){1} -> Toggled the server lock.", player.AccountId, player.ActiveCharacter.Name), Color.Blue);
                        return true;
                    }
                    case "trackprojectiles":
                    {
                        if (!player.IsInArena)
                        {
                            Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, "[System] This command may only be used while in a match."));
                            return true;
                        }

                        player.ActiveArena.DebugFlags ^= ArenaSpecialFlag.ProjectileTracking;

                        Network.SendTo(player.ActiveArena, GamePacket.Outgoing.System.DirectTextMessage(player, String.Format("[System] Projectile tracking is now {0} in this Arena.", player.ActiveArena.DebugFlags.HasFlag(ArenaSpecialFlag.ProjectileTracking) ? "enabled" : "disabled")), Network.SendToType.Arena);
                        return true;
                    }
                    case "trackthins":
                    {
                        if (!player.IsInArena)
                        {
                            Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, "[System] This command may only be used while in a match."));
                            return true;
                        }

                        player.ActiveArena.DebugFlags ^= ArenaSpecialFlag.ThinTracking;

                        Network.SendTo(player.ActiveArena, GamePacket.Outgoing.System.DirectTextMessage(player, String.Format("[System] Thin tracking is now {0} in this Arena.", player.ActiveArena.DebugFlags.HasFlag(ArenaSpecialFlag.ThinTracking) ? "enabled" : "disabled")), Network.SendToType.Arena);
                        return true;
                    }
                    case "trackplayers":
                    {
                        if (!player.IsInArena)
                        {
                            Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, "[System] This command may only be used while in a match."));
                            return true;
                        }

                        player.ActiveArena.DebugFlags ^= ArenaSpecialFlag.PlayerTracking;

                        Network.SendTo(player.ActiveArena, GamePacket.Outgoing.System.DirectTextMessage(player, String.Format("[System] Player tracking is now {0} in this Arena.", player.ActiveArena.DebugFlags.HasFlag(ArenaSpecialFlag.PlayerTracking) ? "enabled" : "disabled")), Network.SendToType.Arena);
                        return true;
                    }
                    case "trackrunes":
                    {
                        if (!player.IsInArena)
                        {
                            Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, "[System] This command may only be used while in a match."));
                            return true;
                        }

                        player.ActiveArena.DebugFlags ^= ArenaSpecialFlag.RuneTracking;

                        Network.SendTo(player.ActiveArena, GamePacket.Outgoing.System.DirectTextMessage(player, String.Format("[System] Rune tracking is now {0} in this Arena.", player.ActiveArena.DebugFlags.HasFlag(ArenaSpecialFlag.RuneTracking) ? "enabled" : "disabled")), Network.SendToType.Arena);
                        return true;
                    }
                    case "onedamage":
                    {
                        if (!player.IsInArena)
                        {
                            Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, "[System] This command may only be used while in a match."));
                            return true;
                        }

                        player.ActiveArena.DebugFlags ^= ArenaSpecialFlag.OneDamageToPlayers;

                        Network.SendTo(player.ActiveArena, GamePacket.Outgoing.System.DirectTextMessage(player, String.Format("[System] One damage spells to players is now {0} in this Arena.", player.ActiveArena.DebugFlags.HasFlag(ArenaSpecialFlag.OneDamageToPlayers) ? "enabled" : "disabled")), Network.SendToType.Arena);
                        return true;
                    }
                    case "expmulti":
                    {
                        if (cmd.Arguments.Count < 1)
                        {
                            Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, "[System] Invalid EXP Multiplier"));
                            return true;
                        }

                        Single expMulti;

                        try
                        {
                            expMulti = Convert.ToSingle(cmd.Arguments[0]);
                        }
                        catch (Exception)
                        {
                            Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, "[System] Invalid EXP Multiplier"));
                            return true;
                        }

						Settings.Default.ExpMultiplier = expMulti;
						Settings.Default.Save();

						MySQL.ServerSettings.SetExpMultiplier(Settings.Default.ExpMultiplier);

						Network.SendTo(GamePacket.Outgoing.System.DirectTextMessage(player, String.Format("[System] All Arenas now have an EXP Bonus of {0}%.", (Settings.Default.ExpMultiplier - 1f) * 100f)), Network.SendToType.All);
						Program.ServerForm.AdminLog.WriteMessage(String.Format("[Admin] ({0}){1} -> Set the EXP multiplier to {2}", player.AccountId, player.ActiveCharacter.Name, expMulti), Color.Blue);
                        return true;
                    }
                    case "laugh":
                    {
                        if (player.IsInArena)
                        {
							GamePacket.Outgoing.System.PlaySoundToArena(player.ActiveArena, GameSound.Sound.EvilLaugh);

                            Program.ServerForm.AdminLog.WriteMessage(String.Format("[Admin] {{{0}}} {1} ({2}), Command: {3}", player.AccountId, player.Username, player.ActiveCharacter.Name, message), Color.Blue);
                        }
                        else
                        {
                            Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, "[System] You are not in an Arena."));
                        }

                        return true;
                    }
                    /*case "music":
                    {
                        if (cmd.Arguments.Count < 1)
                        {
                            Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, "[System] Invalid Song Name"));
                            return true;
                        }

                        String songName = cmd.Arguments[0];

                        if (player.IsInArena)
                        {
                            for (int i = 0; i < player.ActiveArena.ArenaPlayers.Count; i++)
                            {
                                ArenaPlayer musicPlayer = player.ActiveArena.ArenaPlayers[i];
                                if (musicPlayer == null) continue;

                                if (!musicPlayer.WorldPlayer.Flags.HasFlag(PlayerFlag.MusicDisabled))
                                {
                                    Network.Send(musicPlayer.WorldPlayer, GamePacket.Outgoing.System.PlayWebMusic(songName));
                                }
                            }
                        }
                        else
                        {
                            for (int i = 0; i < PlayerManager.Players.Count; i++)
                            {
                                Player musicPlayer = PlayerManager.Players[i];
                                if (musicPlayer == null) continue;

                                if (musicPlayer.TableId == player.TableId && !musicPlayer.Flags.HasFlag(PlayerFlag.MusicDisabled))
                                {
                                    Network.Send(musicPlayer, GamePacket.Outgoing.System.PlayWebMusic(songName));
                                }
                            }
                        }

                        return true;
                    }
                    case "forcemusic":
                    {
                        if (cmd.Arguments.Count < 1)
                        {
                            Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, "[System] Invalid syntax. Usage: !respec <targetname>"));
                            return true;
                        }

                        String targetToken = cmd.Arguments[0];

                        if (player.IsInArena)
                        {
                            ListCollection<ArenaPlayer> targetArenaPlayers = player.ActiveArena.ArenaPlayers.FindArenaPlayers(targetToken);

                            if (targetArenaPlayers.Count > 0)
                            {
                                foreach (ArenaPlayer targetArenaPlayer in targetArenaPlayers)
                                {
                                    if (targetArenaPlayer.WorldPlayer.Flags.HasFlag(PlayerFlag.MusicDisabled) || cmd.Arguments.Count >= 2)
                                    {
                                        targetArenaPlayer.WorldPlayer.Flags &= ~PlayerFlag.MusicDisabled;

                                        if (cmd.Arguments.Count >= 2)
                                        {
                                            String songName = cmd.Arguments[1];
                                            Network.Send(targetArenaPlayer.WorldPlayer, GamePacket.Outgoing.System.PlayWebMusic(songName));
                                        }

                                        Network.Send(targetArenaPlayer.WorldPlayer, GamePacket.Outgoing.System.DirectTextMessage(targetArenaPlayer.WorldPlayer, "[System] Your music has been forced on by an Administrator."));
                                        Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, String.Format("[System] {0}'s music has been forced on.", targetArenaPlayer.WorldPlayer.ActiveCharacter.Name)));
                                    }
                                    else
                                    {
                                        Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, String.Format("[System] {0} does not have music disabled.", targetArenaPlayer.WorldPlayer.ActiveCharacter.Name)));
                                    }
                                }
                            }
                            else
                            {
                                Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, "[System] No targets were found."));
                            }
                        }
                        else
                        {
                            Player targetPlayer = PlayerManager.Players.FindByCharacterName(targetToken);

                            if (targetPlayer != null)
                            {
                                if (targetPlayer.Flags.HasFlag(PlayerFlag.MusicDisabled))
                                {
                                    targetPlayer.Flags &= ~PlayerFlag.MusicDisabled;

                                    Network.Send(targetPlayer, GamePacket.Outgoing.System.PlayWebMusic("play"));

                                    Network.Send(targetPlayer, GamePacket.Outgoing.System.DirectTextMessage(targetPlayer, "[System] Your music has been forced on by an Administrator."));
                                    Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, String.Format("[System] {0}'s music has been forced on.", targetPlayer.ActiveCharacter.Name)));
                                }
                                else
                                {
                                    Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, String.Format("[System] {0} does not have music disabled.", targetPlayer.ActiveCharacter.Name)));
                                }
                            }
                            else
                            {
                                Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, String.Format("[System] {0} was not found.", targetToken)));
                            }
                        }

                        return true;
                    }*/
                }
            }

            if (player.ActiveCharacter.OpLevel == 1)
            {
                switch (cmd.Command)
                {
                    case "respec":
                    {
                        lock (PlayerManager.Players.SyncRoot)
                        {
                            if (!player.IsInArena)
                            {
                                player.ActiveCharacter.PendingFlags |= Character.PendingFlag.ListReset;

                                Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, "[System] Your spell lists have been reset. You must go to the Study for it to take effect."));

                                Program.ServerForm.AdminLog.WriteMessage(String.Format("[Tester] ({0}){1} -> Reset their spell lists.", player.AccountId, player.ActiveCharacter.Name), Color.Blue);
                            }
                            else
                            {
                                Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, "[System] You cannot use this command while in an arena."));
                            }
                        }

                        return true;
                    }
                    case "exp":
                    case "giveexp":
                    {
                        if (cmd.Arguments.Count < 1)
                        {
                            Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, "[System] Invalid syntax. Usage: !giveexp <amount>"));
                            return true;
                        }

                        Int32 amount;

                        try
                        {
                            amount = Convert.ToInt32(cmd.Arguments[0]);
                        }
                        catch (Exception)
                        {
                            Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, "[System] Invalid syntax. Usage: !giveexp <amount>"));
                            return true;
                        }

                        lock (PlayerManager.Players.SyncRoot)
                        {
                            if (!player.IsInArena)
                            {
                                if (amount > 0)
                                {
                                    player.ActiveCharacter.AwardExp += amount;

                                    Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, String.Format("[System] You have been awarded {0} experience.", amount)));

                                    Program.ServerForm.AdminLog.WriteMessage(String.Format("[Tester] ({0}[{1}]){2} -> Gave themself {3} experience.", player.AccountId, player.ActiveCharacter.CharacterId, player.ActiveCharacter.Name, amount), Color.Blue);
                                }
                                else
                                {
                                    Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, "[System] Invalid amount."));
                                }
                            }
                            else
                            {
                                Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, "[System] You cannot use this command while in an arena."));
                            }
                        }

                        return true;
                    }
                }
            }
            // Normal User Commands
            switch (cmd.Command)
            {
                case "help":
                {
                    Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, "[Normal Commands]"));
                    Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, String.Format("{0}levelup", ChatCommand.CommandChar)));
                    Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, String.Format("{0}dice", ChatCommand.CommandChar)));
                    Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, String.Format("{0}mute", ChatCommand.CommandChar)));
                    Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, String.Format("{0}report [message]", ChatCommand.CommandChar)));
                    Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, String.Format("{0}lockexp", ChatCommand.CommandChar)));
                    Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, String.Format("{0}togglemusic", ChatCommand.CommandChar)));
                    Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, String.Format("{0}gs", ChatCommand.CommandChar)));
                    Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, String.Format("{0}mode [ ffa | dm | 2team | custom ]", ChatCommand.CommandChar)));
                    Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, String.Format("{0}rules [ nohinder | notapping | noraisecall | nopoolbiasing | noshrinebiasing | noteams | noregen | nosolidwalls | twoteams | fastregen | nohealother | guildrules ]", ChatCommand.CommandChar)));
                    Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, " "));
                    Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, "[Private Match Commands]"));
                    Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, String.Format("{0}matchduration [minutes]", ChatCommand.CommandChar)));
                    Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, String.Format("{0}akick [name]", ChatCommand.CommandChar)));
                    return true;
                }
                case "random":
                case "roll":
                case "dice":
                {
					if (player.Flags.HasFlag(PlayerFlag.Muted))
					{
						SendSystemMessage(player, Resources.Strings_Common.Muted);
						return true;
					}

                    try
                    {
                        Int32 min, max;

                        if (cmd.Arguments.Count < 1)
                        {
                            min = 1;
                            max = 100;
                        }
                        else if (cmd.Arguments.Count == 1)
                        {
                            min = 1;
                            max = Convert.ToInt32(cmd.Arguments[0]);

                            if (max < min) max = 2;
                        }
                        else
                        {
                            min = Convert.ToInt32(cmd.Arguments[0]);
                            max = Convert.ToInt32(cmd.Arguments[1]);

                            if (max < min) max = min + 1;
                        }

                        if (min <= 0) min = 1;
                        if (max <= 0) max = min;

                        String diceRoll = CryptoRandom.GetInt32(min, max).ToString(CultureInfo.InvariantCulture);

                        Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, String.Format("[Dice] You roll {0} {1}. ({2} to {3}).", diceRoll.GetFirstAOrAnPrefix(), diceRoll, min, max)));
                        Network.SendTo(player, GamePacket.Outgoing.System.DirectTextMessage(player, String.Format("[Dice] {0} rolls {1} {2}. ({3} to {4})", player.ActiveCharacter.Name, diceRoll.GetFirstAOrAnPrefix(), diceRoll, min, max)), player.IsInArena ? Network.SendToType.Arena : Network.SendToType.Table, false);

                        Program.ServerForm.ChatLog.WriteMessage(String.Format("[{0}] ({1}){2} rolls {3} {4}. ({5} to {6})", player.WorldLocationString, player.AccountId, player.ActiveCharacter.Name, diceRoll.GetFirstAOrAnPrefix(), diceRoll, min, max), Color.MidnightBlue);
                    }
                    catch
                    {
                        Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, "[System] Invalid syntax. Usage: !roll, !roll [max], !roll [min] [max]"));
                    }

                    return true;
                }
                case "mute":
                {
                    if (cmd.Arguments.Count < 1)
                    {
                        player.Flags ^= PlayerFlag.ChatDisabled;

                        String chatStatus = player.Flags.HasFlag(PlayerFlag.ChatDisabled) ? "disabled" : "enabled";
                        Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, String.Format("[System] Your chat has been {0}.", chatStatus)));

                        Program.ServerForm.MiscLog.WriteMessage(String.Format("[Misc] ({0}){1} -> {2} {3} chat.", player.AccountId, player.Username, player.ActiveCharacter.Name, chatStatus), Color.Blue);

                        return true;
                    }

                    if (player.IsAdmin)
                    {
                        String playerName = cmd.Arguments[0];

                        lock (PlayerManager.Players.SyncRoot)
                        {
                            Player targetPlayer = PlayerManager.Players.FindByCharacterName(playerName);

                            if (targetPlayer != null)
                            {
                                if (player.Admin < targetPlayer.Admin)
                                {
                                    Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, "[System] You cannot mute an admin with higher privileges."));
                                    return true;
                                }

                                targetPlayer.Flags ^= PlayerFlag.Muted;

                                String muteStatus = targetPlayer.Flags.HasFlag(PlayerFlag.Muted) ? "muted" : "un-muted";
                                Network.Send(targetPlayer, GamePacket.Outgoing.System.DirectTextMessage(targetPlayer, String.Format("[System] You have been {0} by a Moderator.", muteStatus)));
                                Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, String.Format("[System] {0} has been {1}.", targetPlayer.ActiveCharacter.Name, muteStatus)));

                                Program.ServerForm.AdminLog.WriteMessage(String.Format("[Admin] ({0}){1} -> {2} has been {3}.", player.AccountId, player.ActiveCharacter.Name, targetPlayer.ActiveCharacter.Name, muteStatus), Color.Blue);
                            }
                            else
                            {
                                Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, String.Format("[System] {0} was not found.", playerName)));
                            }
                        }
                    }

                    return true;
                }
                case "rules":
                {
                    if (cmd.Arguments.Count < 1)
                    {
                        Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, "[System] You must specify at least one rule."));
                        return true;
                    }

                    ArenaRuleset.ArenaRule rules = cmd.Arguments.Aggregate(ArenaRuleset.ArenaRule.None, (current, ruleString) => current | ArenaRuleset.GetRuleFromString(ruleString));
                    
                    switch (player.IsAdmin)
                    {
                        case true:
                        {
                            if (rules.HasFlag(ArenaRuleset.ArenaRule.CaptureTheFlag))
                            {
                                if (player.Admin != AdminLevel.Developer)
                                {
                                    rules = ArenaRuleset.ArenaRule.None;
                                }
                            }

                            break;
                        }
                        case false:
                        {
                            if (rules.HasFlag(ArenaRuleset.ArenaRule.CaptureTheFlag) || rules.HasFlag(ArenaRuleset.ArenaRule.ExpEvent))
                            {
                                rules = ArenaRuleset.ArenaRule.None;
                            }

                            break;
                        }
                    }

                    player.PreferredArenaRules = rules;

                    Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, String.Format("[System] Your preferred arena rules have been set to '{0}'.", player.PreferredArenaRules)));

                    return true;
                }
                case "mode":
                {
                    if (cmd.Arguments.Count < 1)
                    {
                        Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, "[System] You must specify a valid mode."));
                        return true;
                    }

                    ArenaRuleset.ArenaMode mode = ArenaRuleset.GetModeFromString(cmd.Arguments[0]);

                    switch (player.IsAdmin)
                    {
                        case true:
                        {
                            switch (mode)
                            {
                                case ArenaRuleset.ArenaMode.CaptureTheFlag:
                                {
                                    if (player.Admin != AdminLevel.Developer)
                                    {
                                        mode = ArenaRuleset.ArenaMode.Normal;
                                    }
                                    break;
                                }
                            }
                                    
                            break;
                        }
                        case false:
                        {
                            switch (mode)
                            {
                                case ArenaRuleset.ArenaMode.ExpEvent:
                                case ArenaRuleset.ArenaMode.CaptureTheFlag:
                                {
                                    mode = ArenaRuleset.ArenaMode.Normal;
                                    break;
                                }
                            }

                            break;
                        }
                    }

                    player.PreferredArenaMode = mode;

                    Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, String.Format("[System] Your preferred arena creation mode has been set to '{0}'.", player.PreferredArenaMode)));

                    return true;
                }
                case "level":
                case "levelup":
                {
                    String validLevelString = String.Format("[System] You must specify a level. Valid levels are: {0}.", "1-25");

                    if (cmd.Arguments.Count < 1)
                    {
                        Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, validLevelString));
                        return true;
                    }

                    Int32 level;

                    try
                    {
                        level = Convert.ToInt32(cmd.Arguments[0]);
                    }
                    catch (Exception)
                    {
                        Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, validLevelString));
                        return true;
                    }

                    if (level <= 0 || level > 25)
                    {
                        Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, validLevelString));
                        return true;
                    }

                    if (player.ActiveCharacter.Level > 25)
                    {
                        Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, "[System] Your level is too high to use this service."));
                        return true;
                    }

                    if (player.Flags.HasFlag(PlayerFlag.ExpLocked))
                    {
                        Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, "[System] You cannot use this service while exp locked."));
                        return true;
                    }

                    player.ActiveCharacter.GrantedLevel = level;

                    Character.Save(player, null);

                    Program.ServerForm.MiscLog.WriteMessage(String.Format("[Misc] ({0}[{1}]) {2} has been granted level {3}.", player.AccountId, player.ActiveCharacter.CharacterId, player.ActiveCharacter.Name, level), Color.Teal);

                    Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, String.Format("[System] You have been granted level {0}.", level)));
                    Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, "[System] You must go to your character sheet to see the changes."));

                    return true;
                }
                case "matchduration":
                {
                    if (!player.IsInArena)
                    {
                        Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, "[System] This command may only be used while in a match."));
                        return true;
                    }

                    if (player.ActiveArena.TableId == 0 && !player.IsAdmin)
                    {
                        Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, "[System] This command may only be used in a private match."));
                        return true;
                    }

                    if (player.ActiveArena.Founder != player.ActiveCharacter.Name && !player.IsAdmin)
                    {
                        Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, "[System] This command may only be used by the match's founder."));
                        return true;
                    }

                    if (player.ActiveArena.IsDurationLocked && !player.IsAdmin)
                    {
                        Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, "[System] This match's duration has already been set and may not be changed again."));
                        return true;
                    }

                    if (cmd.Arguments.Count < 1)
                    {
                        Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, "[System] You must specify a duration in minutes."));
                        return true;
                    }

                    Int32 duration;

                    try
                    {
                        duration = Convert.ToInt32(cmd.Arguments[0]);
                    }
                    catch (Exception)
                    {
                        Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, "[System] You must specify a duration in minutes."));
                        return true;
                    }

                    if (duration <= 0 || duration > 60)
                    {
                        Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, "[System] The duration must be longer than 0 and shorter than 60."));
                        return true;
                    }

                    player.ActiveArena.Duration = new Interval((duration * 60) * 1000, false);
                    player.ActiveArena.TimeLimit = (Int16)(duration * 60);
                    player.ActiveArena.IsDurationLocked = true;

                    Program.ServerForm.MiscLog.WriteMessage(String.Format("[Arena Duration] {{{0}}} {1} ({2}), Set Arena Duration -> {3} minutes", player.AccountId, player.Username, player.ActiveCharacter.Name, player.ActiveArena.Duration.RemainingSeconds / 60), Color.Blue);

                    Network.SendTo(player, GamePacket.Outgoing.System.DirectTextMessage(player, String.Format("[System] The match duration has been set to {0} minute(s).", duration)), Network.SendToType.Arena, true);
                    return true;
                }

                case "guildscore":
                case "gs":
                {
                    if (!player.IsInArena)
                    {
                        Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, "[System] This command may only be used while in a match."));
                        return true;
                    }

                    if (!player.ActiveArena.Ruleset.Rules.HasFlag(ArenaRuleset.ArenaRule.GuildRules))
                    {
                        Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, "[System] This match is not a guild match."));
                        return true;
                    }

                    if (player.ActiveArena.ArenaPlayers.GetTeamPlayerCount(Team.Dragon) > 0 || !player.ActiveArena.ArenaTeams.Dragon.Shrine.IsDead)
                    {
                        Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(null, String.Format("[Guild Match] Dragon: {0:0.00}", player.ActiveArena.ArenaTeams.Dragon.Shrine.GuildPoints)));
                    }

                    if (player.ActiveArena.ArenaPlayers.GetTeamPlayerCount(Team.Gryphon) > 0 || !player.ActiveArena.ArenaTeams.Gryphon.Shrine.IsDead)
                    {
                        Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(null, String.Format("[Guild Match] Gryphon: {0:0.00}", player.ActiveArena.ArenaTeams.Gryphon.Shrine.GuildPoints)));
                    }

                    if (player.ActiveArena.ArenaPlayers.GetTeamPlayerCount(Team.Pheonix) > 0 || !player.ActiveArena.ArenaTeams.Pheonix.Shrine.IsDead)
                    {
                        Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(null, String.Format("[Guild Match] Pheonix: {0:0.00}", player.ActiveArena.ArenaTeams.Pheonix.Shrine.GuildPoints)));
                    }

                    Team guildWinTeam = Team.Neutral;

                    if (player.ActiveArena.WinningTeam == Team.Neutral)
                    {
                        if (player.ActiveArena.ArenaTeams.Gryphon.Shrine.GuildPoints > player.ActiveArena.ArenaTeams.Dragon.Shrine.GuildPoints && player.ActiveArena.ArenaTeams.Gryphon.Shrine.GuildPoints > player.ActiveArena.ArenaTeams.Pheonix.Shrine.GuildPoints)
                        {
                            guildWinTeam = Team.Gryphon;
                        }

                        if (player.ActiveArena.ArenaTeams.Pheonix.Shrine.GuildPoints > player.ActiveArena.ArenaTeams.Dragon.Shrine.GuildPoints && player.ActiveArena.ArenaTeams.Pheonix.Shrine.GuildPoints > player.ActiveArena.ArenaTeams.Gryphon.Shrine.GuildPoints)
                        {
                            guildWinTeam = Team.Pheonix;
                        }

                        if (player.ActiveArena.ArenaTeams.Dragon.Shrine.GuildPoints > player.ActiveArena.ArenaTeams.Gryphon.Shrine.GuildPoints && player.ActiveArena.ArenaTeams.Dragon.Shrine.GuildPoints > player.ActiveArena.ArenaTeams.Pheonix.Shrine.GuildPoints)
                        {
                            guildWinTeam = Team.Dragon;

                        }
                    }
                    else
                    {
                        guildWinTeam = player.ActiveArena.WinningTeam;
                    }

                    Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(null, String.Format("[Guild Match] Winning Team: {0}", (guildWinTeam == Team.Neutral) ? "None" : guildWinTeam.ToString())));

                    return true;
                }

                case "arenakick":
                case "akick":
                {
                    if (!player.IsInArena)
                    {
                        Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, "[System] This command may only be used while in a match."));
                        return true;
                    }

                    if (player.ActiveArena.TableId == 0 && !player.IsAdmin)
                    {
                        Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, "[System] This command may only be used in a private match."));
                        return true;
                    }

                    if (player.ActiveArena.Founder != player.ActiveCharacter.Name && !player.IsAdmin)
                    {
                        Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, "[System] This command may only be used by the match's founder."));
                        return true;
                    }

                    if (cmd.Arguments.Count < 1)
                    {
                        Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, "[System] You must specify a player to remove from the arena."));
                        return true;
                    }

                    lock (player.ActiveArena.ArenaPlayers.SyncRoot)
                    {
                        if (!player.IsAdmin)
                        {
                            if (player.ActiveArena.ArenaPlayers.Any(arenaPlayer => arenaPlayer.WorldPlayer.IsAdmin))
                            {
                                Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, "[System] You may not use this command while a moderator is in the match."));
                                return true;
                            }
                        }

                        String playerName = cmd.Arguments[0];

                        ArenaPlayer targetArenaPlayer = player.ActiveArena.ArenaPlayers.FindByCharacterName(playerName);

                        if (targetArenaPlayer != null)
                        {
	                        Player targetPlayer = targetArenaPlayer.WorldPlayer;

							if (targetPlayer == player)
                            {
                                Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, "[System] You cannot kick yourself."));
                                return true;
                            }

							if (player.Admin < targetPlayer.Admin)
                            {
                                Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, "[System] You cannot kick an admin with higher privileges."));
                                return true;
                            }

                            Program.ServerForm.MiscLog.WriteMessage(String.Format("[Arena Kick] {{{0}}} {1} ({2}), Kicked Player -> {3}", player.AccountId, player.Username, player.ActiveCharacter.Name, targetArenaPlayer.ActiveCharacter.Name), Color.Blue);

                            Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, String.Format("[System] {0} has been removed from the arena.", targetArenaPlayer.ActiveCharacter.Name)));

                            player.ActiveArena.ArenaKickPlayer(targetArenaPlayer);

                        }
                        else
                        {
                            Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, String.Format("[System] {0} was not found.", playerName)));
                        }
                    }

                    return true;
                }
                case "report":
                {
                    String reportString = cmd.GetStringFromArgs(0);

                    if (reportString.Length < 10)
                    {
                        Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, "[System] Your report text is too short."));
                        break;
                    }

                    for (int i = 0; i < PlayerManager.Players.Count; i++)
                    {
                        Player adminPlayer = PlayerManager.Players[i];
                        if (adminPlayer == null) continue;

                        if (adminPlayer.IsAdmin)
                        {
                            Network.Send(adminPlayer, GamePacket.Outgoing.System.DirectTextMessage(adminPlayer, String.Format("[System] Player {0}({1}) has sent the following report: {2}", player.ActiveCharacter.Name, player.Username, reportString)));
                        }
                    }

                    Program.ServerForm.ReportLog.WriteMessage(String.Format("[Report] {{{0}}} {1} ({2}), Report: {3}", player.AccountId, player.Username, player.ActiveCharacter.Name, reportString), Color.DarkOrange);

                    MailManager.QueueMail("Player Report", String.Format("Account Name: {0}\nCharacter Name: {1}\nSerial: {2}\nReport Message: {3}", player.Username, player.ActiveCharacter.Name, player.Serial, reportString));

                    Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, "[System] Your report has been sent.  If you abuse this service, your account may be suspended."));

                    return true;
                }
                case "lockexp":
                {
                    player.Flags ^= PlayerFlag.ExpLocked;

                    Program.ServerForm.MiscLog.WriteMessage(String.Format("[Locked EXP] {{{0}}} {1} ({2}), Locked EXP -> Is Locked: {3}", player.AccountId, player.Username, player.ActiveCharacter.Name, player.Flags.HasFlag(PlayerFlag.ExpLocked)), Color.Blue);

                    Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, String.Format("[System] Your exp is now {0}.", player.Flags.HasFlag(PlayerFlag.ExpLocked) ? "locked" : "un-locked")));
                    return true;
                }
                /*case "togglemusic":
                {
                    player.Flags ^= PlayerFlag.MusicDisabled;

                    Program.ServerForm.MiscLog.WriteMessage(String.Format("[Togglemusic] {{{0}}} {1} ({2}), Toggled Music -> Is Toggled: {3}", player.AccountId, player.Username, player.ActiveCharacter.Name, player.Flags.HasFlag(PlayerFlag.MusicDisabled)), Color.Blue);

                    Network.Send(player, GamePacket.Outgoing.System.PlayWebMusic("stop"));
                    Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, String.Format("[System] Music has been {0}.", player.Flags.HasFlag(PlayerFlag.MusicDisabled) ? "disabled" : "enabled")));
                    return true;
                }*/
                default:
                {
                    Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, "[System] You have entered an unknown command."));
                    return true;
                }
            }

            return true;
        }
        public static void UpdateAllPlayers(SpellServer.Player targetplayer)
        {
            MemoryStream outStream = null;
            Int32 j = 0;

            if (targetplayer.IsInArena) return;

            lock (PlayerManager.Players.SyncRoot)
            {
                for (Int32 i = 0; i < PlayerManager.Players.Count; i++)
                {
                    Player player = PlayerManager.Players[i];

                    if (player == null) continue;

                    // 1. Don't send the target player to themselves (usually handled by the client)
                    if (targetplayer == player) continue;

                    // 2. THE VISIBILITY RULE
                    // Disabled for now...need to test more. Random player invis
                    /*if (player.Flags.HasFlag(PlayerFlag.Hidden))
                    {
                        // ...ONLY show them if the person RECEIVING the list is an Admin.
                        if (!targetplayer.IsAdmin) continue;
                    }*/

                    outStream = GamePacket.Outgoing.World.PlayerEnterLarge(player, outStream);

                    j++;

                    if (j == 10)
                    {
                        Network.Send(targetplayer, outStream);
                        outStream = null;
                        j = 0;
                    }
                }

                // Handle the Remainder (1-9 players)
                if (j > 0 && outStream != null)
                {
                    for (Int32 x = 10 - j; x > 0; x--)
                    {
                        for (Int32 r = 0; r < 24; r++)
                        {
                            outStream.WriteByte(0x00);
                        }
                    }
                    
                    Network.Send(targetplayer, outStream);
                }
            }
        }
        public static void UpdateAllArenaPlayers(SpellServer.Player targetplayer)
        {
            MemoryStream outStream = null;
            Int32 j = 0;

            if (!targetplayer.IsInArena) return;

            lock (targetplayer.ActiveArena.ArenaPlayers.SyncRoot)
            {
                for (Int32 i = 0; i < targetplayer.ActiveArena.ArenaPlayers.Count; i++)
                {
                    ArenaPlayer arenaPlayer = targetplayer.ActiveArena.ArenaPlayers[i];
                    if (arenaPlayer == null) continue;

                    // 1. Don't send the target player to themselves (usually handled by the client)
                    if (targetplayer.ActiveArenaPlayer == arenaPlayer) continue;

                    // 2. THE VISIBILITY RULE
                    // Disabled for now...need to test more. Random player invis
                    /*if (arenaPlayer.WorldPlayer.Flags.HasFlag(PlayerFlag.Hidden))
                    {
                        // ...ONLY show them if the person RECEIVING the list is an Admin.
                        if (!targetplayer.IsAdmin) continue;
                    }*/

                    outStream = GamePacket.Outgoing.Arena.ArenaPlayerEnterLarge(arenaPlayer, outStream);

                    j++;

                    if (j == 10)
                    {                        
                        Network.Send(targetplayer, outStream);
                        outStream = null;
                        j = 0;
                    }
                }

                // Handle the Remainder (1-9 players)
                if (j > 0 && outStream != null)
                {
                    for (Int32 x = 10 - j; x > 0; x--)
                    {
                        for (Int32 r = 0; r < 24; r++)
                        {
                            outStream.WriteByte(0x00);
                        }
                    }
                    
                    Network.Send(targetplayer, outStream);
                }
            }
        }
        public static void PlayerEnteredWorld(Player player, Byte worldId, Team team, String charName, bool UDP = false)
        {
            player.ActiveTeam = team;

            switch (worldId)
            {
                // Study
                case 0:
                {
                    player.TableId = worldId;

                    if (player.ActiveCharacter != null)
                    {
                        Character.Save(player, null);

						MySQL.OnlineCharacters.SetOffline(player.ActiveCharacter.CharacterId);

                        player.ActiveCharacter = null;
                    }

                    Network.SendTo(player, GamePacket.Outgoing.World.PlayerLeave(player, UDP), Network.SendToType.Tavern, false);
                    Network.Send(player, GamePacket.Outgoing.Player.SendPlayerId(player, UDP));
                    break;
                }

                // Tavern
                case 255:
                {
                    if (player.ActiveCharacter != null)
                    {
                        Character.Save(player, null);
                    }
                    else
                    {
                        player.ActiveCharacter = Character.LoadByNameAndAccountId(player, charName);

                        if (player.ActiveCharacter != null)
                        {
							MySQL.OnlineCharacters.SetOnline(player.ActiveCharacter.CharacterId, player.TableId, 0, "");
                        }
                    }

                    if (player.ActiveCharacter == null)
                    {
	                    player.DisconnectReason = Resources.Strings_Disconnect.ErrorLoadingCharacter;
                        player.Disconnect = true;
                        return;
                    }

                    if (player.IsAdmin)
                    {
                        if (player.ActiveCharacter.OpLevel == 5)
                        {
                            Network.Send(player, GamePacket.Outgoing.System.SendAdminStatus(true));
                        }
                        else if (player.ActiveCharacter.OpLevel == 3)
                        {
                            Network.Send(player, GamePacket.Outgoing.System.SendAdminStatus(false));
                        }
                    }

                    player.Flags |= player.ActiveCharacter.PlayerFlags;

                    if (player.ActiveCharacter.OpLevel >= 3)
                    {
                        player.Flags |= PlayerFlag.Hidden;
                    }
                    else
                    {
                        player.Flags &= ~PlayerFlag.Hidden;
                    }

                    player.TableId = worldId;   
                                        

                    Network.Send(player, GamePacket.Outgoing.Player.SendPlayerId(player));                    

                    //if (!player.Flags.HasFlag(PlayerFlag.Hidden))
                    //{
                        Network.SendTo(player, GamePacket.Outgoing.World.PlayerJoin(player), Network.SendToType.Tavern, false);
                    //}

                    Network.Send(player, GamePacket.Outgoing.Player.HasEnteredWorld());
                    Network.Send(player, GamePacket.Outgoing.Arena.SuccessfulArenaEntry());

                    break;
                }

                default:
                {
                    // Arenas
                    if (worldId >= 1 && worldId <= 16)
                    {
                        Arena arena = ArenaManager.Arenas.FindById(worldId);

                        if (arena != null)
                        {
                            new ArenaPlayer(player, arena);

                        }
                    }
                    break;
                }
            }
        }

        public static void SpawnPlayer(Player player, bool UDP = false)
        {
            Network.Send(player, GamePacket.Outgoing.World.SpawnPlayer(player, UDP));
        }
    }
}
