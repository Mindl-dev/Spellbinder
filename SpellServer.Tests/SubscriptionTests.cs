using System;
using System.Reflection;
using NUnit.Framework;

namespace SpellServer.Tests
{
    [TestFixture]
    public class SubscriptionTests
    {
        private static Player MakePlayer(int accountId, string username = "TestUser", AdminLevel admin = AdminLevel.None, string serial = "TEST_SERIAL")
        {
            var player = (Player)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(Player));
            typeof(Player).GetField("AccountId", BindingFlags.Public | BindingFlags.Instance).SetValue(player, accountId);
            typeof(Player).GetField("Username", BindingFlags.Public | BindingFlags.Instance).SetValue(player, username);
            typeof(Player).GetField("Disconnect", BindingFlags.Public | BindingFlags.Instance).SetValue(player, false);
            typeof(Player).GetField("DisconnectReason", BindingFlags.Public | BindingFlags.Instance).SetValue(player, "");
            typeof(Player).GetField("Admin", BindingFlags.Public | BindingFlags.Instance).SetValue(player, admin);
            typeof(Player).GetField("Serial", BindingFlags.Public | BindingFlags.Instance).SetValue(player, serial);
            return player;
        }

        // ================================================================
        // KickGhostSessions tests
        // ================================================================

        [Test]
        public void KickGhost_FlagsGhostForDisconnect()
        {
            var ghost = MakePlayer(100, "Ghost");
            var newPlayer = MakePlayer(0, "NewLogin");
            var players = new PlayerManager();
            players.Add(ghost);

            Subscription.KickGhostSessions(newPlayer, 100, players);

            Assert.IsTrue(ghost.Disconnect);
        }

        [Test]
        public void KickGhost_RemovesGhostFromList()
        {
            var ghost = MakePlayer(100, "Ghost");
            var newPlayer = MakePlayer(0, "NewLogin");
            var players = new PlayerManager();
            players.Add(ghost);

            Subscription.KickGhostSessions(newPlayer, 100, players);

            Assert.IsNull(players.FindByAccountId(100), "Ghost should be removed from player list");
        }

        [Test]
        public void KickGhost_DoesNotKickSelf()
        {
            var player = MakePlayer(100, "Self");
            var players = new PlayerManager();
            players.Add(player);

            Subscription.KickGhostSessions(player, 100, players);

            Assert.IsFalse(player.Disconnect);
            Assert.AreEqual(1, players.Count);
        }

        [Test]
        public void KickGhost_NoGhost_DoesNothing()
        {
            var newPlayer = MakePlayer(0, "NewLogin");
            var players = new PlayerManager();

            Assert.DoesNotThrow(() => Subscription.KickGhostSessions(newPlayer, 100, players));
        }

        // ================================================================
        // CheckMultibox tests
        // ================================================================

        [Test]
        public void CheckMultibox_SameSerial_Blocks()
        {
            var existing = MakePlayer(100, "Alice", serial: "SERIAL_ABC");
            var players = new PlayerManager();
            players.Add(existing);

            var error = Subscription.CheckMultibox("SERIAL_ABC", AdminLevel.None, players);

            Assert.AreEqual(Subscription.ErrorType.LoggedIn, error);
        }

        [Test]
        public void CheckMultibox_DifferentSerial_Allows()
        {
            var existing = MakePlayer(100, "Alice", serial: "SERIAL_ABC");
            var players = new PlayerManager();
            players.Add(existing);

            var error = Subscription.CheckMultibox("SERIAL_XYZ", AdminLevel.None, players);

            Assert.AreEqual(Subscription.ErrorType.None, error);
        }

        [Test]
        public void CheckMultibox_AdminBypassesBlock()
        {
            var existing = MakePlayer(100, "Alice", serial: "SERIAL_ABC");
            var players = new PlayerManager();
            players.Add(existing);

            var error = Subscription.CheckMultibox("SERIAL_ABC", AdminLevel.Developer, players);

            Assert.AreEqual(Subscription.ErrorType.None, error);
        }

        [Test]
        public void CheckMultibox_NotFoundSerial_Skips()
        {
            var existing = MakePlayer(100, "Alice", serial: "SERIAL_ABC");
            var players = new PlayerManager();
            players.Add(existing);

            var error = Subscription.CheckMultibox("Not_Found", AdminLevel.None, players);

            Assert.AreEqual(Subscription.ErrorType.None, error);
        }

        [Test]
        public void CheckMultibox_VMWareSerial_Skips()
        {
            var existing = MakePlayer(100, "Alice", serial: "SERIAL_ABC");
            var players = new PlayerManager();
            players.Add(existing);

            var error = Subscription.CheckMultibox("VMWare", AdminLevel.None, players);

            Assert.AreEqual(Subscription.ErrorType.None, error);
        }

        // ================================================================
        // Ghost then multibox — the real bug
        // ================================================================

        [Test]
        public void KickGhost_ThenMultibox_DoesNotBlock()
        {
            // The bug: ghost gets kicked but stays in list, serial check blocks new login
            // After fix: ghost is removed, serial check finds nothing
            var ghost = MakePlayer(100, "Ghost", serial: "SAME_SERIAL");
            var newPlayer = MakePlayer(0, "NewLogin", serial: "SAME_SERIAL");
            var players = new PlayerManager();
            players.Add(ghost);

            // Step 1: kick ghost
            Subscription.KickGhostSessions(newPlayer, 100, players);

            // Step 2: multibox check should NOT find the ghost anymore
            var error = Subscription.CheckMultibox("SAME_SERIAL", AdminLevel.None, players);

            Assert.AreEqual(Subscription.ErrorType.None, error,
                "After ghost is kicked and removed, serial check should pass");
        }

        // ================================================================
        // CheckServerLock tests
        // ================================================================

        [Test]
        public void CheckServerLock_Locked_BlocksNonAdmin()
        {
            var error = Subscription.CheckServerLock(true, AdminLevel.None);

            Assert.AreEqual(Subscription.ErrorType.ServerLocked, error);
        }

        [Test]
        public void CheckServerLock_Locked_AllowsAdmin()
        {
            var error = Subscription.CheckServerLock(true, AdminLevel.Developer);

            Assert.AreEqual(Subscription.ErrorType.None, error);
        }

        [Test]
        public void CheckServerLock_Unlocked_AllowsEveryone()
        {
            var error = Subscription.CheckServerLock(false, AdminLevel.None);

            Assert.AreEqual(Subscription.ErrorType.None, error);
        }

        // ================================================================
        // CheckServerFull tests
        // ================================================================

        [Test]
        public void CheckServerFull_Full_BlocksNonAdmin()
        {
            var error = Subscription.CheckServerFull(101, AdminLevel.None, false);

            Assert.AreEqual(Subscription.ErrorType.ServerFull, error);
        }

        [Test]
        public void CheckServerFull_Full_AllowsAdmin()
        {
            var error = Subscription.CheckServerFull(101, AdminLevel.Staff, false);

            Assert.AreEqual(Subscription.ErrorType.None, error);
        }

        [Test]
        public void CheckServerFull_NotFull_AllowsEveryone()
        {
            var error = Subscription.CheckServerFull(50, AdminLevel.None, false);

            Assert.AreEqual(Subscription.ErrorType.None, error);
        }

        [Test]
        public void CheckServerFull_Full_AllowsPlus()
        {
            var error = Subscription.CheckServerFull(101, AdminLevel.None, true);

            Assert.AreEqual(Subscription.ErrorType.None, error);
        }
    }
}
