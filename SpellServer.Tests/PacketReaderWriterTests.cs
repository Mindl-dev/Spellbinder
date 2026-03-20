using System;
using System.IO;
using System.Text;
using NUnit.Framework;
using Helper.Network;

namespace SpellServer.Tests
{
    [TestFixture]
    public class PacketReaderTests
    {
        private MemoryStream MakeStream(params byte[] data)
        {
            return new MemoryStream(data);
        }

        // --- Constructor / Padding ---

        [Test]
        public void Constructor_SkipsPaddingByDefault()
        {
            // First 2 bytes are padding, third byte is the data
            var reader = new PacketReader(MakeStream(0xAA, 0xBB, 0x42));
            Assert.AreEqual(0x42, reader.ReadByte());
        }

        [Test]
        public void Constructor_NoSkipPadding()
        {
            var reader = new PacketReader(MakeStream(0xAA, 0xBB, 0x42), skipPadding: false);
            Assert.AreEqual(0xAA, reader.ReadByte());
        }

        // --- ReadByte ---

        [Test]
        public void ReadByte_ReturnsCorrectValue()
        {
            var reader = new PacketReader(MakeStream(0x00, 0x00, 0xFF));
            Assert.AreEqual(0xFF, reader.ReadByte());
        }

        [Test]
        public void ReadByte_ThrowsAtEnd()
        {
            var reader = new PacketReader(MakeStream(0x00, 0x00));
            Assert.Throws<EndOfStreamException>(() => reader.ReadByte());
        }

        // --- ReadBool ---

        [Test]
        public void ReadBool_ZeroIsFalse()
        {
            var reader = new PacketReader(MakeStream(0x00, 0x00, 0x00));
            Assert.IsFalse(reader.ReadBool());
        }

        [Test]
        public void ReadBool_NonZeroIsTrue()
        {
            var reader = new PacketReader(MakeStream(0x00, 0x00, 0x01));
            Assert.IsTrue(reader.ReadBool());

            var reader2 = new PacketReader(MakeStream(0x00, 0x00, 0xFF));
            Assert.IsTrue(reader2.ReadBool());
        }

        // --- ReadInt16BE ---

        [Test]
        public void ReadInt16BE_ParsesBigEndian()
        {
            // 0x0134 big-endian = 308 decimal
            var reader = new PacketReader(MakeStream(0x00, 0x00, 0x01, 0x34));
            Assert.AreEqual(308, reader.ReadInt16BE());
        }

        [Test]
        public void ReadInt16BE_NegativeValue()
        {
            // 0xFFFF big-endian = -1
            var reader = new PacketReader(MakeStream(0x00, 0x00, 0xFF, 0xFF));
            Assert.AreEqual(-1, reader.ReadInt16BE());
        }

        [Test]
        public void ReadInt16BE_MatchesManualFlip()
        {
            // Verify we get the same result as the old manual pattern
            byte[] raw = new byte[] { 0x00, 0x00, 0x12, 0x34 };

            // Old way
            var stream1 = new MemoryStream(raw);
            stream1.Seek(2, SeekOrigin.Begin);
            byte[] buf = new byte[2];
            stream1.Read(buf, 0, 2);
            short oldResult = NetHelper.FlipBytes(BitConverter.ToInt16(buf, 0));

            // New way
            var reader = new PacketReader(new MemoryStream(raw));
            short newResult = reader.ReadInt16BE();

            Assert.AreEqual(oldResult, newResult);
        }

        // --- ReadUInt16BE ---

        [Test]
        public void ReadUInt16BE_ParsesBigEndian()
        {
            // 0xABCD = 43981
            var reader = new PacketReader(MakeStream(0x00, 0x00, 0xAB, 0xCD));
            Assert.AreEqual(43981, reader.ReadUInt16BE());
        }

        // --- ReadInt32BE ---

        [Test]
        public void ReadInt32BE_ParsesBigEndian()
        {
            // 0x00000064 = 100
            var reader = new PacketReader(MakeStream(0x00, 0x00, 0x00, 0x00, 0x00, 0x64));
            Assert.AreEqual(100, reader.ReadInt32BE());
        }

        // --- ReadFixedString ---

        [Test]
        public void ReadFixedString_NullTerminated()
        {
            byte[] data = new byte[] { 0x00, 0x00, (byte)'B', (byte)'o', (byte)'b', 0x00, 0x00, 0x00 };
            var reader = new PacketReader(MakeStream(data));
            Assert.AreEqual("Bob", reader.ReadFixedString(6));
        }

        [Test]
        public void ReadFixedString_FullLength()
        {
            byte[] data = new byte[] { 0x00, 0x00, (byte)'A', (byte)'B', (byte)'C' };
            var reader = new PacketReader(MakeStream(data));
            Assert.AreEqual("ABC", reader.ReadFixedString(3));
        }

        [Test]
        public void ReadFixedString_Empty()
        {
            byte[] data = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00 };
            var reader = new PacketReader(MakeStream(data));
            Assert.AreEqual("", reader.ReadFixedString(3));
        }

        // --- ReadBytes ---

        [Test]
        public void ReadBytes_ReturnsCorrectSlice()
        {
            var reader = new PacketReader(MakeStream(0x00, 0x00, 0x11, 0x22, 0x33, 0x44));
            byte[] result = reader.ReadBytes(4);
            Assert.AreEqual(new byte[] { 0x11, 0x22, 0x33, 0x44 }, result);
        }

        // --- Skip ---

        [Test]
        public void Skip_AdvancesPosition()
        {
            var reader = new PacketReader(MakeStream(0x00, 0x00, 0xAA, 0xBB, 0xCC));
            reader.Skip(2);
            Assert.AreEqual(0xCC, reader.ReadByte());
        }

        // --- Sequential reads ---

        [Test]
        public void SequentialReads_MatchRealHandler()
        {
            // Simulate CastTargeted incoming packet:
            // [2 padding] [2 spellId] [5 skip] [1 targetId] [8 skip] [1 isResisted]
            var ms = new MemoryStream();
            ms.Write(new byte[] { 0x00, 0x00 }, 0, 2);          // padding
            ms.Write(new byte[] { 0x00, 0x2A }, 0, 2);          // spellId = 42 BE
            ms.Write(new byte[] { 0, 0, 0, 0, 0 }, 0, 5);      // skip 5
            ms.WriteByte(0x07);                                   // targetId = 7
            ms.Write(new byte[8], 0, 8);                         // skip 8
            ms.WriteByte(0x01);                                   // isResisted = true
            ms.Position = 0;

            var reader = new PacketReader(ms);
            short spellId = reader.ReadInt16BE();
            reader.Skip(5);
            byte targetId = reader.ReadByte();
            reader.Skip(8);
            bool isResisted = reader.ReadBool();

            Assert.AreEqual(42, spellId);
            Assert.AreEqual(7, targetId);
            Assert.IsTrue(isResisted);
        }
    }

    [TestFixture]
    public class PacketWriterTests
    {
        // --- Basic construction ---

        [Test]
        public void Constructor_WritesLeadingZeroAndFuncId()
        {
            var writer = new PacketWriter(PacketOutFunction.PlayerJoin);
            byte[] data = writer.ToStream().ToArray();
            Assert.AreEqual(0x00, data[0]);
            Assert.AreEqual((byte)PacketOutFunction.PlayerJoin, data[1]);
        }

        // --- WriteByte ---

        [Test]
        public void WriteByte_AppendsCorrectly()
        {
            var writer = new PacketWriter(PacketOutFunction.PlayerMoveState);
            writer.WriteByte(0xAB);
            byte[] data = writer.ToStream().ToArray();
            Assert.AreEqual(0xAB, data[2]);
        }

        // --- WriteInt16BE ---

        [Test]
        public void WriteInt16BE_BigEndian()
        {
            var writer = new PacketWriter(PacketOutFunction.CastEffect);
            writer.WriteInt16BE(0x1234);
            byte[] data = writer.ToStream().ToArray();
            // Bytes 0,1 = header. Bytes 2,3 = value in BE.
            Assert.AreEqual(0x12, data[2]);
            Assert.AreEqual(0x34, data[3]);
        }

        [Test]
        public void WriteInt16BE_MatchesManualFlip()
        {
            short value = 42;

            // Old way
            var old = new MemoryStream();
            old.WriteByte(0x00);
            old.WriteByte((byte)PacketOutFunction.CastEffect);
            old.Write(BitConverter.GetBytes(NetHelper.FlipBytes(value)), 0, 2);

            // New way
            var writer = new PacketWriter(PacketOutFunction.CastEffect);
            writer.WriteInt16BE(value);

            Assert.AreEqual(old.ToArray(), writer.ToStream().ToArray());
        }

        // --- WriteUInt16BE ---

        [Test]
        public void WriteUInt16BE_BigEndian()
        {
            var writer = new PacketWriter(PacketOutFunction.PlayerMoveState);
            writer.WriteUInt16BE(0xABCD);
            byte[] data = writer.ToStream().ToArray();
            Assert.AreEqual(0xAB, data[2]);
            Assert.AreEqual(0xCD, data[3]);
        }

        // --- WriteInt32BE ---

        [Test]
        public void WriteInt32BE_BigEndian()
        {
            var writer = new PacketWriter(PacketOutFunction.PlayerMoveState);
            writer.WriteInt32BE(0x12345678);
            byte[] data = writer.ToStream().ToArray();
            Assert.AreEqual(0x12, data[2]);
            Assert.AreEqual(0x34, data[3]);
            Assert.AreEqual(0x56, data[4]);
            Assert.AreEqual(0x78, data[5]);
        }

        // --- WriteFixedString ---

        [Test]
        public void WriteFixedString_NullPads()
        {
            var writer = new PacketWriter(PacketOutFunction.PlayerJoin);
            writer.WriteFixedString("Bob", 12);
            byte[] data = writer.ToStream().ToArray();
            Assert.AreEqual((byte)'B', data[2]);
            Assert.AreEqual((byte)'o', data[3]);
            Assert.AreEqual((byte)'b', data[4]);
            // Remaining 9 bytes should be 0x00
            for (int i = 5; i < 14; i++)
                Assert.AreEqual(0x00, data[i], $"Byte {i} should be null padding");
        }

        [Test]
        public void WriteFixedString_Truncates()
        {
            var writer = new PacketWriter(PacketOutFunction.PlayerJoin);
            writer.WriteFixedString("VeryLongPlayerName", 6);
            byte[] data = writer.ToStream().ToArray();
            // Should write only first 5 chars + null terminator at position 5
            Assert.AreEqual((byte)'V', data[2]);
            Assert.AreEqual(8, data.Length); // 2 header + 6 string
        }

        [Test]
        public void WriteFixedString_NullInput()
        {
            var writer = new PacketWriter(PacketOutFunction.PlayerJoin);
            writer.WriteFixedString(null, 4);
            byte[] data = writer.ToStream().ToArray();
            for (int i = 2; i < 6; i++)
                Assert.AreEqual(0x00, data[i]);
        }

        // --- WritePadding ---

        [Test]
        public void WritePadding_WritesZeros()
        {
            var writer = new PacketWriter(PacketOutFunction.CastEffect);
            writer.WritePadding(16);
            byte[] data = writer.ToStream().ToArray();
            Assert.AreEqual(18, data.Length); // 2 header + 16 padding
            for (int i = 2; i < 18; i++)
                Assert.AreEqual(0x00, data[i]);
        }

        // --- WriteBytes ---

        [Test]
        public void WriteBytes_RelayPreserved()
        {
            byte[] relay = new byte[] { 0x68, 0x6F, 0xF1, 0x40, 0x07, 0x7C };
            var writer = new PacketWriter(PacketOutFunction.PlayerMoveState);
            writer.WriteBytes(relay);
            byte[] data = writer.ToStream().ToArray();
            for (int i = 0; i < relay.Length; i++)
                Assert.AreEqual(relay[i], data[i + 2]);
        }

        // --- End-to-end: replicate a real outgoing packet ---

        [Test]
        public void CastEffect_MatchesOldConstruction()
        {
            short spellId = 42;

            // Old way (from GamePacket.cs)
            var oldStream = new MemoryStream();
            oldStream.WriteByte(0x00);
            oldStream.WriteByte((byte)PacketOutFunction.CastEffect);
            oldStream.Write(BitConverter.GetBytes(NetHelper.FlipBytes(spellId)), 0, 2);
            oldStream.WriteByte(0x00);
            oldStream.WriteByte(0x00);

            // New way
            var writer = new PacketWriter(PacketOutFunction.CastEffect);
            writer.WriteInt16BE(spellId);
            writer.WritePadding(2);

            Assert.AreEqual(oldStream.ToArray(), writer.ToStream().ToArray());
        }

        [Test]
        public void PlayerMoveState_MatchesOldConstruction()
        {
            byte[] relayData = new byte[] { 0x68, 0x6F, 0xF1, 0x40, 0x07, 0x7C, 0x0E, 0x72, 0x24, 0xC8, 0x00, 0x01 };

            // Old way
            var oldStream = new MemoryStream();
            oldStream.WriteByte(0x00);
            oldStream.WriteByte((byte)PacketOutFunction.PlayerMoveState);
            oldStream.Write(relayData, 0, 12);

            // New way
            var writer = new PacketWriter(PacketOutFunction.PlayerMoveState);
            writer.WriteBytes(relayData);

            Assert.AreEqual(oldStream.ToArray(), writer.ToStream().ToArray());
        }
    }
}
