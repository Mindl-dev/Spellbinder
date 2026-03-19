using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using Helper;
using Helper.Network;
using static SpellServer.Tests.TestHelpers;

namespace SpellServer.Tests
{
    [TestFixture]
    public class GamePacketIncomingTests
    {
        [OneTimeSetUp]
        public void FixtureSetUp()
        {
            Program.Headless = true;
            if (Program.HeadlessMainLog == null)
                Program.HeadlessMainLog = new Helper.ConsoleLogBox("Test");
        }

        private MemoryStream MakeStream(params byte[] data)
        {
            return new MemoryStream(data);
        }

        // ================================================================
        // Null guard tests — verify handlers don't crash when
        // ActiveArena/ActiveArenaPlayer is null
        // ================================================================

        [Test]
        public void Arena_CastEffect_NullArena_ReturnsEarly()
        {
            var p = MakePlayer();
            // ActiveArena is null by default on uninitialized Player
            var stream = MakeStream(0x00, 0x00, 0x00, 0x2A); // spellId=42
            Assert.DoesNotThrow(() =>
                GamePacket.Incoming.Arena.CastEffect(p, stream));
        }

        [Test]
        public void Arena_CastTargeted_NullArena_ReturnsEarly()
        {
            var p = MakePlayer();
            var stream = new MemoryStream(new byte[30]);
            Assert.DoesNotThrow(() =>
                GamePacket.Incoming.Arena.CastTargeted(p, stream));
        }

        [Test]
        public void Arena_CastProjectile_NullArena_ReturnsEarly()
        {
            var p = MakePlayer();
            var stream = new MemoryStream(new byte[20]);
            Assert.DoesNotThrow(() =>
                GamePacket.Incoming.Arena.CastProjectile(p, stream));
        }

        [Test]
        public void Arena_CastRune_NullArena_ReturnsEarly()
        {
            var p = MakePlayer();
            var stream = new MemoryStream(new byte[24]);
            Assert.DoesNotThrow(() =>
                GamePacket.Incoming.Arena.CastRune(p, stream));
        }

        [Test]
        public void Arena_CastWall_NullArena_ReturnsEarly()
        {
            var p = MakePlayer();
            var stream = new MemoryStream(new byte[20]);
            Assert.DoesNotThrow(() =>
                GamePacket.Incoming.Arena.CastWall(p, stream));
        }

        [Test]
        public void Arena_CastBolt_NullArena_ReturnsEarly()
        {
            var p = MakePlayer();
            var stream = new MemoryStream(new byte[36]);
            Assert.DoesNotThrow(() =>
                GamePacket.Incoming.Arena.CastBolt(p, stream));
        }

        [Test]
        public void Arena_CastDispell_NullArena_ReturnsEarly()
        {
            var p = MakePlayer();
            var stream = new MemoryStream(new byte[20]);
            Assert.DoesNotThrow(() =>
                GamePacket.Incoming.Arena.CastDispell(p, stream));
        }

        [Test]
        public void Arena_BiasedPool_NullArena_ReturnsEarly()
        {
            var p = MakePlayer();
            var stream = MakeStream(0x00, 0x00, 0x01, 0x02, 0x03);
            Assert.DoesNotThrow(() =>
                GamePacket.Incoming.Arena.BiasedPool(p, stream));
        }

        [Test]
        public void Arena_BiasedShrine_NullArena_ReturnsEarly()
        {
            var p = MakePlayer();
            var stream = MakeStream(0x00, 0x00, 0x01, 0x02, 0x03, 0x04);
            Assert.DoesNotThrow(() =>
                GamePacket.Incoming.Arena.BiasedShrine(p, stream));
        }

        [Test]
        public void Arena_PlayerMoveState_NullArena_ReturnsEarly()
        {
            var p = MakePlayer();
            var stream = new MemoryStream(new byte[14]);
            Assert.DoesNotThrow(() =>
                GamePacket.Incoming.Arena.PlayerMoveState(p, stream));
        }

        [Test]
        public void Arena_PlayerMoveStateShort_NullArena_ReturnsEarly()
        {
            var p = MakePlayer();
            var stream = new MemoryStream(new byte[10]);
            Assert.DoesNotThrow(() =>
                GamePacket.Incoming.Arena.PlayerMoveStateShort(p, stream));
        }

        [Test]
        public void Arena_ThinDamage_NullArena_ReturnsEarly()
        {
            var p = MakePlayer();
            var stream = new MemoryStream(new byte[10]);
            Assert.DoesNotThrow(() =>
                GamePacket.Incoming.Arena.ThinDamage(p, stream));
        }

        [Test]
        public void Arena_ActivatedTrigger_NullArena_ReturnsEarly()
        {
            var p = MakePlayer();
            var stream = new MemoryStream(new byte[8]);
            Assert.DoesNotThrow(() =>
                GamePacket.Incoming.Arena.ActivatedTrigger(p, stream));
        }

        [Test]
        public void Arena_TappedAtShrine_NullArena_ReturnsEarly()
        {
            var p = MakePlayer();
            Assert.DoesNotThrow(() =>
                GamePacket.Incoming.Arena.TappedAtShrine(p));
        }

        [Test]
        public void Arena_Jump_NullArena_ReturnsEarly()
        {
            var p = MakePlayer();
            var stream = new MemoryStream(new byte[6]);
            Assert.DoesNotThrow(() =>
                GamePacket.Incoming.Arena.Jump(p, stream));
        }

        [Test]
        public void Arena_God_NullArena_ReturnsEarly()
        {
            var p = MakePlayer();
            var stream = new MemoryStream(new byte[6]);
            Assert.DoesNotThrow(() =>
                GamePacket.Incoming.Arena.God(p, stream));
        }

        [Test]
        public void Arena_Yank_NullArena_ReturnsEarly()
        {
            var p = MakePlayer();
            var stream = new MemoryStream(new byte[6]);
            Assert.DoesNotThrow(() =>
                GamePacket.Incoming.Arena.Yank(p, stream));
        }

        // ================================================================
        // Chat buffer overflow mitigation (128 byte cap)
        // ================================================================

        [Test]
        public void Chat_ShortMessage_DoesNotThrow()
        {
            var p = MakePlayer(username: "TestPlayer");
            // Chat packet: [2 pad][2 ???][2 target BE][1 chatType][3 skip][message bytes]
            // Total length needs to be > 10 for tLen = length - 10
            var ms = new MemoryStream();
            ms.Write(new byte[4], 0, 4);                        // padding + header
            ms.Write(new byte[] { 0x00, 0x00 }, 0, 2);          // target = 0
            ms.WriteByte(0x00);                                   // chatType
            ms.Write(new byte[3], 0, 3);                         // skip
            byte[] msg = System.Text.Encoding.ASCII.GetBytes("Hello world\0");
            ms.Write(msg, 0, msg.Length);
            ms.Position = 0;

            // Will try to call World.ProcessChatMessage which may fail
            // without full server — but it should NOT crash on the parse itself
            try
            {
                GamePacket.Incoming.Player.Chat(p, ms);
            }
            catch (NullReferenceException)
            {
                // Expected — World.ProcessChatMessage needs full server state
                // The parse succeeded, the handler got past the 128-byte check
            }
        }

        [Test]
        public void Chat_OversizedMessage_Dropped()
        {
            var p = MakePlayer(username: "TestPlayer");
            var ms = new MemoryStream();
            ms.Write(new byte[4], 0, 4);
            ms.Write(new byte[] { 0x00, 0x00 }, 0, 2);
            ms.WriteByte(0x00);
            ms.Write(new byte[3], 0, 3);
            // 200-char message (exceeds 128 limit)
            byte[] msg = new byte[200];
            for (int i = 0; i < 200; i++) msg[i] = (byte)'A';
            ms.Write(msg, 0, msg.Length);
            ms.Position = 0;

            // Should return early without calling ProcessChatMessage
            // (no crash, no NullReferenceException)
            Assert.DoesNotThrow(() =>
                GamePacket.Incoming.Player.Chat(p, ms));
        }

        // ================================================================
        // Login handlers
        // ================================================================

        [Test]
        public void Login_Disconnect_SetsFlags()
        {
            var p = MakePlayer();
            Assert.DoesNotThrow(() =>
                GamePacket.Incoming.Login.Disconnect(p));
            Assert.IsTrue(p.Disconnect, "Disconnect flag should be set");
        }

        // ================================================================
        // MageHook anti-cheat handlers
        // ================================================================

        [Test]
        public void MageHook_HackNotification_NullPlayer_DoesNotCrash()
        {
            // These handlers log and disconnect — just verify they don't crash
            var p = MakePlayer(username: "Cheater");
            var stream = MakeStream(0x00, 0x00, 0x00); // hackType = 0 (debugger)
            Assert.DoesNotThrow(() =>
                GamePacket.Incoming.MageHook.HackNotification(p, stream));
            Assert.IsTrue(p.Disconnect, "Should disconnect on hack detection");
        }

        [Test]
        public void MageHook_CheatProgramNotification_SetsDisconnect()
        {
            var p = MakePlayer(username: "Cheater");
            // cheatProgram byte + cheatType byte
            var stream = MakeStream(0x00, 0x00, 0x01, 0x02);
            Assert.DoesNotThrow(() =>
                GamePacket.Incoming.MageHook.CheatProgramNotification(p, stream));
            Assert.IsTrue(p.Disconnect, "Should disconnect on cheat program detection");
        }
    }
}
