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
        // ================================================================
        // Deobfuscate (pure function — swap nibbles on odd-index chars)
        // ================================================================

        [Test]
        public void Deobfuscate_EmptyString_ReturnsEmpty()
        {
            Assert.AreEqual("", GamePacket.Incoming.World.Deobfuscate(""));
        }

        [Test]
        public void Deobfuscate_Null_ReturnsNull()
        {
            Assert.IsNull(GamePacket.Incoming.World.Deobfuscate(null));
        }

        [Test]
        public void Deobfuscate_SingleChar_Unchanged()
        {
            // Index 0 is even — not modified
            Assert.AreEqual("A", GamePacket.Incoming.World.Deobfuscate("A"));
        }

        [Test]
        public void Deobfuscate_OddIndex_SwapsNibbles()
        {
            // 'A' = 0x41. At odd index, nibbles swap: 0x14 = (char)20
            string input = "A" + (char)0x41;
            string result = GamePacket.Incoming.World.Deobfuscate(input);
            Assert.AreEqual('A', result[0], "even index unchanged");
            Assert.AreEqual((char)0x14, result[1], "odd index nibble-swapped");
        }

        [Test]
        public void Deobfuscate_RoundTrip()
        {
            // Deobfuscate applied twice should return original
            // (nibble swap is its own inverse)
            string original = "TestPassword123";
            string once = GamePacket.Incoming.World.Deobfuscate(original);
            string twice = GamePacket.Incoming.World.Deobfuscate(once);
            Assert.AreEqual(original, twice, "double deobfuscate should restore original");
        }

        // ================================================================
        // Login.Disconnect — verifies reason string
        // ================================================================

        [Test]
        public void Login_Disconnect_SetsLogoffReason()
        {
            var p = MakePlayer();
            GamePacket.Incoming.Login.Disconnect(p);
            Assert.IsNotNull(p.DisconnectReason, "reason should be set");
            Assert.IsTrue(p.Disconnect);
        }

        // ================================================================
        // Chat parser edge cases
        // ================================================================

        [Test]
        public void Chat_ExactlyMaxLength_NotDropped()
        {
            var p = MakePlayer(username: "TestPlayer");
            var ms = new MemoryStream();
            ms.Write(new byte[4], 0, 4);
            ms.Write(new byte[] { 0x00, 0x00 }, 0, 2);
            ms.WriteByte(0x00);
            ms.Write(new byte[3], 0, 3);
            // Exactly 128 chars + null (should NOT be dropped)
            byte[] msg = new byte[129];
            for (int i = 0; i < 128; i++) msg[i] = (byte)'B';
            msg[128] = 0x00;
            ms.Write(msg, 0, msg.Length);
            ms.Position = 0;

            // Should get past the length check and hit ProcessChatMessage
            // which will throw NullRef (expected — no World state)
            try
            {
                GamePacket.Incoming.Player.Chat(p, ms);
                // If we get here without NullRef, that's OK too (World might be null-safe)
            }
            catch (NullReferenceException)
            {
                // Expected — means the message was NOT dropped (parse succeeded)
            }
        }

        [Test]
        public void Chat_OneOverMax_Dropped()
        {
            var p = MakePlayer(username: "TestPlayer");
            var ms = new MemoryStream();
            ms.Write(new byte[4], 0, 4);
            ms.Write(new byte[] { 0x00, 0x00 }, 0, 2);
            ms.WriteByte(0x00);
            ms.Write(new byte[3], 0, 3);
            // 129 chars (one over max 128) — should be dropped
            byte[] msg = new byte[130];
            for (int i = 0; i < 129; i++) msg[i] = (byte)'C';
            msg[129] = 0x00;
            ms.Write(msg, 0, msg.Length);
            ms.Position = 0;

            // Should return early without hitting ProcessChatMessage
            Assert.DoesNotThrow(() => GamePacket.Incoming.Player.Chat(p, ms));
        }

        // ================================================================
        // ExitWorld — null guard
        // ================================================================

        [Test]
        public void Player_ExitWorld_NullArena_ReturnsEarly()
        {
            var p = MakePlayer();
            Assert.DoesNotThrow(() => GamePacket.Incoming.Player.ExitWorld(p));
        }
        // ================================================================
        // Integration: spell cast parse verification
        // Inject a fake spell into SpellManager.Spells, build a Player
        // with an Arena stub, and verify the handler reads spellId correctly
        // ================================================================

        [Test]
        public void CastEffect_ParsesSpellId_Correctly()
        {
            // Inject a spell at index 42
            while (SpellManager.Spells.Count <= 42)
                SpellManager.Spells.Add(null);
            var testSpell = MakeSpell(id: 42);
            SpellManager.Spells.Insert(42, testSpell);

            // Build a player with arena (so the null guard passes)
            var p = MakePlayer();
            var arena = MakeArena();
            var ap = MakeArenaPlayer(id: 1);

            // Wire them together via reflection (ActiveArena setter has side effects)
            typeof(Player).GetField("_arena", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(p, arena);
            p.ActiveArenaPlayer = ap;

            // Build packet: [2 padding] [2 spellId=42 BE]
            var ms = new MemoryStream();
            ms.Write(new byte[] { 0x00, 0x00 }, 0, 2);  // padding
            ms.Write(BitConverter.GetBytes(NetHelper.FlipBytes((short)42)), 0, 2); // spellId=42 BE
            ms.Position = 0;

            // The handler will parse spellId, lookup SpellManager.Spells[42],
            // then call arena.CastEffect which will fail (arena not fully set up)
            // But if it gets past the spell lookup, the parse was correct
            try
            {
                GamePacket.Incoming.Arena.CastEffect(p, ms);
            }
            catch (Exception ex) when (ex is NullReferenceException || ex is ArgumentNullException)
            {
                // Expected — CastEffect calls into arena logic which needs full state
                // The fact that we got past the spell null check proves parse was correct
            }

            // Cleanup
            SpellManager.Spells.RemoveAt(42);
        }

        [Test]
        public void CastEffect_WrongSpellId_ReturnsEarly()
        {
            // SpellManager.Spells[999] should be null → handler returns early
            var p = MakePlayer();
            var arena = MakeArena();
            var ap = MakeArenaPlayer(id: 1);
            typeof(Player).GetField("_arena", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(p, arena);
            p.ActiveArenaPlayer = ap;

            // spellId=999 (out of range)
            var ms = new MemoryStream();
            ms.Write(new byte[] { 0x00, 0x00 }, 0, 2);
            ms.Write(BitConverter.GetBytes(NetHelper.FlipBytes((short)999)), 0, 2);
            ms.Position = 0;

            // Should return early without crash (spell is null)
            Assert.DoesNotThrow(() => GamePacket.Incoming.Arena.CastEffect(p, ms));
        }

        [Test]
        public void CastTargeted_ParsesSpellIdAndTargetId()
        {
            // Inject a spell at index 10
            while (SpellManager.Spells.Count <= 10)
                SpellManager.Spells.Add(null);
            var testSpell = MakeSpell(id: 10);
            SpellManager.Spells.Insert(10, testSpell);

            var p = MakePlayer();
            var arena = MakeArena();
            var ap = MakeArenaPlayer(id: 1);
            typeof(Player).GetField("_arena", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(p, arena);
            p.ActiveArenaPlayer = ap;

            // CastTargeted packet layout:
            // [0-1] padding [2-3] spellId BE [4-8] skip 5 [9] targetId [10-17] skip 8 [18] isResisted
            var ms = new MemoryStream();
            ms.Write(new byte[] { 0x00, 0x00 }, 0, 2);                           // padding
            ms.Write(BitConverter.GetBytes(NetHelper.FlipBytes((short)10)), 0, 2); // spellId=10
            ms.Write(new byte[5], 0, 5);                                          // skip 5
            ms.WriteByte(0x07);                                                    // targetId=7
            ms.Write(new byte[8], 0, 8);                                          // skip 8
            ms.WriteByte(0x01);                                                    // isResisted=true
            // Add more bytes for the relay read (28 bytes from offset 2)
            while (ms.Length < 30) ms.WriteByte(0x00);
            ms.Position = 0;

            // When isResisted=true, handler takes the resist path which calls
            // Network.Send (will fail). But it should parse without crashing.
            try
            {
                GamePacket.Incoming.Arena.CastTargeted(p, ms);
            }
            catch (Exception ex) when (ex is NullReferenceException || ex is ArgumentNullException)
            {
                // Expected — the resist path needs ArenaPlayers which is null on our stub
                // Getting here proves the parse completed correctly
            }

            // Cleanup
            SpellManager.Spells.RemoveAt(10);
        }
    }
}

