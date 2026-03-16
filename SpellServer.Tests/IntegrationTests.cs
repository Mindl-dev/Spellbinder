using System;
using System.Drawing;
using System.IO;
using NUnit.Framework;
using Helper;
using Helper.Network;

namespace SpellServer.Tests
{
    /// <summary>Pre-PR integration tests verifying critical fixes don't regress.</summary>
    [TestFixture]
    public class IntegrationTests
    {
        // === Test 1: PlayerMoveState round-trip source_id ===

        [Test]
        public void PlayerMoveState_Relay_HasCorrectSourceId()
        {
            // Build a PlayerMoveState via the actual outgoing packet builder,
            // then wrap in Packet with an ArenaPlayerId, and verify the header.
            byte[] relayData = new byte[] { 0x03, 0xE8, 0xF1, 0x20, 0x07, 0x7C, 0x0E, 0x72, 0x24, 0xC8, 0x00, 0x01 };

            // Simulate what Outgoing.Arena.PlayerMoveState builds:
            // [0x00, func_id(0x01), 12 bytes relay data]
            var inStream = new MemoryStream();
            inStream.WriteByte(0x00);
            inStream.WriteByte((byte)PacketOutFunction.PlayerMoveState);
            inStream.Write(relayData, 0, 12);

            byte arenaPlayerId = 5;
            var packet = new Packet(inStream, arenaPlayerId);

            // Header: [0-1] length, [2-3] source_id (BE), [4] func_id
            UInt16 sourceId = (UInt16)((packet.PacketData[2] << 8) | packet.PacketData[3]);
            Assert.AreEqual(arenaPlayerId, sourceId,
                "Relayed PlayerMoveState must have sender's ArenaPlayerId as source_id");
            Assert.AreEqual((byte)PacketOutFunction.PlayerMoveState, packet.PacketData[4],
                "func_id must be 0x01 (PlayerMoveState)");
        }

        [Test]
        public void PlayerMoveState_Relay_DataIntact()
        {
            // Verify the 12-byte relay payload survives the Packet wrapping
            byte[] relayData = new byte[] { 0x03, 0xE8, 0xF1, 0x20, 0x07, 0x7C, 0x0E, 0x72, 0x24, 0xC8, 0x00, 0x01 };

            var inStream = new MemoryStream();
            inStream.WriteByte(0x00);
            inStream.WriteByte((byte)PacketOutFunction.PlayerMoveState);
            inStream.Write(relayData, 0, 12);

            var packet = new Packet(inStream, 5);

            // Data starts at offset 5: [0-1] len, [2-3] source, [4] func, [5+] data
            for (int i = 0; i < 12; i++)
            {
                Assert.AreEqual(relayData[i], packet.PacketData[5 + i],
                    $"Relay byte {i} must be preserved after Packet wrapping");
            }
        }

        // === Test 2: Non-arena packets use source_id=0 ===

        [Test]
        public void ArenaState_HasZeroSourceId()
        {
            var inStream = new MemoryStream();
            inStream.WriteByte(0x00);
            inStream.WriteByte((byte)PacketOutFunction.ArenaState);
            inStream.Write(new byte[80], 0, 80);

            // No sourceId parameter — should default to 0
            var packet = new Packet(inStream);

            UInt16 sourceId = (UInt16)((packet.PacketData[2] << 8) | packet.PacketData[3]);
            Assert.AreEqual(0, sourceId, "ArenaState must use source_id=0 (system packet)");
        }

        [Test]
        public void SuccessfulArenaEntry_HasZeroSourceId()
        {
            var inStream = new MemoryStream();
            inStream.WriteByte(0x00);
            inStream.WriteByte((byte)PacketOutFunction.SuccessfulArenaEntry);

            var packet = new Packet(inStream);

            UInt16 sourceId = (UInt16)((packet.PacketData[2] << 8) | packet.PacketData[3]);
            Assert.AreEqual(0, sourceId, "SuccessfulArenaEntry must use source_id=0");
        }

        [Test]
        public void PlayerJoin_HasCorrectSourceId()
        {
            // PlayerJoin is arena-relayed, so it should carry source_id
            var inStream = new MemoryStream();
            inStream.WriteByte(0x00);
            inStream.WriteByte((byte)PacketOutFunction.PlayerJoin);
            inStream.Write(new byte[20], 0, 20);

            var packet = new Packet(inStream, 3);

            UInt16 sourceId = (UInt16)((packet.PacketData[2] << 8) | packet.PacketData[3]);
            Assert.AreEqual(3, sourceId, "PlayerJoin must carry the sender's ArenaPlayerId");
        }

        [TestCase((UInt16)0)]
        [TestCase((UInt16)1)]
        [TestCase((UInt16)15)]
        [TestCase((UInt16)127)]
        [TestCase((UInt16)255)]
        public void Packet_SourceId_AllValidArenaPlayerIds(UInt16 id)
        {
            // ArenaPlayerId ranges from 1-255 (byte). Verify all work.
            var inStream = new MemoryStream();
            inStream.WriteByte(0x00);
            inStream.WriteByte((byte)PacketOutFunction.PlayerMoveState);
            inStream.Write(new byte[12], 0, 12);

            var packet = new Packet(inStream, id);

            UInt16 actual = (UInt16)((packet.PacketData[2] << 8) | packet.PacketData[3]);
            Assert.AreEqual(id, actual, $"source_id={id} must survive Packet construction");
        }

        // === Test 4: INI cache correctness ===

        [Test]
        public void IniCache_MultipleFiles_IndependentCaches()
        {
            // Two different INI files should not cross-contaminate
            string file1 = Path.GetTempFileName();
            string file2 = Path.GetTempFileName();
            try
            {
                File.WriteAllText(file1, "[section]\nkey=value1\n");
                File.WriteAllText(file2, "[section]\nkey=value2\n");

                string result1 = NativeMethods.GetPrivateProfileString("section", "key", file1);
                string result2 = NativeMethods.GetPrivateProfileString("section", "key", file2);

                Assert.AreEqual("value1", result1);
                Assert.AreEqual("value2", result2);
            }
            finally
            {
                File.Delete(file1);
                File.Delete(file2);
            }
        }

        [Test]
        public void IniCache_SpellFormat_ParsesCorrectly()
        {
            // Simulate a real Spells.dat entry with all field types
            string file = Path.GetTempFileName();
            try
            {
                File.WriteAllText(file, @"[spelldefs]
numspells=400

[spell01]
name=Flame Streak I
type=projectile
power=10
fatigue=5
min_damage=3
max_damage=8
velocity=200
gravity=false
fire_timer=150
cast_timer=100
overlay=0
");
                Assert.AreEqual(400, NativeMethods.GetPrivateProfileInt32("spelldefs", "numspells", file));
                Assert.AreEqual("Flame Streak I", NativeMethods.GetPrivateProfileString("spell01", "name", file));
                Assert.AreEqual("projectile", NativeMethods.GetPrivateProfileString("spell01", "type", file));
                Assert.AreEqual(10, NativeMethods.GetPrivateProfileInt32("spell01", "power", file));
                Assert.AreEqual(3, NativeMethods.GetPrivateProfileInt32("spell01", "min_damage", file));
                Assert.AreEqual(200, NativeMethods.GetPrivateProfileInt32("spell01", "velocity", file));
                Assert.IsFalse(NativeMethods.GetPrivateProfileBoolean("spell01", "gravity", file));
                Assert.AreEqual(150, NativeMethods.GetPrivateProfileInt32("spell01", "fire_timer", file));
                Assert.AreEqual(0, NativeMethods.GetPrivateProfileInt32("spell01", "overlay", file));
            }
            finally
            {
                File.Delete(file);
            }
        }

        [Test]
        public void IniCache_MissingFile_ReturnsDefaults()
        {
            string fakePath = Path.Combine(Path.GetTempPath(), "nonexistent_" + Guid.NewGuid() + ".ini");
            Assert.AreEqual("", NativeMethods.GetPrivateProfileString("section", "key", fakePath));
            Assert.AreEqual(-1, NativeMethods.GetPrivateProfileInt32("section", "key", fakePath));
            Assert.IsFalse(NativeMethods.GetPrivateProfileBoolean("section", "key", fakePath));
        }

        // === Test 5: Headless mode logging ===

        [Test]
        public void ConsoleLogBox_WriteMessage_DoesNotThrow()
        {
            var log = new ConsoleLogBox("Test");
            Assert.DoesNotThrow(() => log.WriteMessage("test", Color.Red));
            Assert.DoesNotThrow(() => log.WriteMessage("", Color.Blue));
            Assert.DoesNotThrow(() => log.WriteMessage(null, Color.Green));
        }

        [Test]
        public void ProgramLog_AllCategories_DoNotThrow()
        {
            bool origHeadless = Program.Headless;
            var origLog = Program.HeadlessMainLog;
            try
            {
                Program.Headless = true;
                Program.HeadlessMainLog = new ConsoleLogBox("TestAll");

                string[] categories = { "Main", "Chat", "Cheat", "Admin", "Whisper", "Report", "Misc" };
                foreach (var cat in categories)
                {
                    Assert.DoesNotThrow(() => Program.Log($"test {cat}", Color.White, cat),
                        $"Program.Log with category '{cat}' must not throw in headless mode");
                }
            }
            finally
            {
                Program.Headless = origHeadless;
                Program.HeadlessMainLog = origLog;
            }
        }

        [Test]
        public void ProgramLog_NullServerForm_DoesNotThrow()
        {
            bool origHeadless = Program.Headless;
            var origForm = Program.ServerForm;
            var origLog = Program.HeadlessMainLog;
            try
            {
                // Simulate GUI mode with null form (startup race condition)
                Program.Headless = false;
                Program.ServerForm = null;
                Program.HeadlessMainLog = null;

                // Should fall through to Console.WriteLine, not crash
                Assert.DoesNotThrow(() => Program.Log("test", Color.Red));
                Assert.DoesNotThrow(() => Program.Log("test", Color.Red, "Cheat"));
            }
            finally
            {
                Program.Headless = origHeadless;
                Program.ServerForm = origForm;
                Program.HeadlessMainLog = origLog;
            }
        }

        // === Test 7: LogBox drain order ===
        // Note: LogBox extends RichTextBox (WinForms control) so we can't instantiate
        // it in a headless test. Instead we test the principle: messages queued A,B,C
        // should be processed in that order. We test the ConsoleLogBox equivalent.

        [Test]
        public void ConsoleLogBox_MessagesProcessedInOrder()
        {
            // ConsoleLogBox writes synchronously, so order is guaranteed.
            // This test documents the expected behavior and catches if someone
            // changes it to async/batched incorrectly.
            var log = new ConsoleLogBox("OrderTest");
            var output = new System.Collections.Generic.List<string>();

            // Capture console output
            var origOut = Console.Out;
            var sw = new StringWriter();
            Console.SetOut(sw);
            try
            {
                log.WriteMessage("first", Color.Red);
                log.WriteMessage("second", Color.Blue);
                log.WriteMessage("third", Color.Green);

                string result = sw.ToString();
                int pos1 = result.IndexOf("first");
                int pos2 = result.IndexOf("second");
                int pos3 = result.IndexOf("third");

                Assert.Greater(pos1, -1, "first should appear in output");
                Assert.Greater(pos2, pos1, "second should appear after first");
                Assert.Greater(pos3, pos2, "third should appear after second");
            }
            finally
            {
                Console.SetOut(origOut);
            }
        }
    }
}
