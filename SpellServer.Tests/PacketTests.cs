using System;
using System.IO;
using NUnit.Framework;
using Helper;
using Helper.Network;

namespace SpellServer.Tests
{
    [TestFixture]
    public class PacketTests
    {
        [Test]
        public void Packet_Constructor_SetsCorrectLength()
        {
            // Build a simple outgoing packet (like PlayerMoveState)
            var inStream = new MemoryStream();
            inStream.WriteByte(0x00);                                    // padding
            inStream.WriteByte((byte)PacketOutFunction.PlayerMoveState); // 0x01
            inStream.Write(new byte[12], 0, 12);                        // 12 bytes relay data

            var packet = new Packet(inStream);

            // PacketData layout: [2:length] [1:0x00] [stream data] [2:trailing]
            // length should be the inStream length (14)
            int payloadLen = NetHelper.FlipBytes(BitConverter.ToInt16(packet.PacketData, 0));
            Assert.AreEqual(14, payloadLen, "Payload length should match inStream size");
        }

        [Test]
        public void Packet_Constructor_SetsCorrectFuncId()
        {
            var inStream = new MemoryStream();
            inStream.WriteByte(0x00);
            inStream.WriteByte((byte)PacketOutFunction.PlayerJoin); // 0x03
            inStream.Write(new byte[20], 0, 20);

            var packet = new Packet(inStream);

            Assert.AreEqual(PacketOutFunction.PlayerJoin, packet.Function);
        }

        [Test]
        public void Packet_Constructor_SourceIdIsZero()
        {
            var inStream = new MemoryStream();
            inStream.WriteByte(0x00);
            inStream.WriteByte((byte)PacketOutFunction.PlayerMoveState);
            inStream.Write(new byte[12], 0, 12);

            var packet = new Packet(inStream);

            // Source ID is at bytes [2-3] — both should be 0x00
            Assert.AreEqual(0x00, packet.PacketData[2], "Source ID high byte should be 0");
            Assert.AreEqual(0x00, packet.PacketData[3], "Source ID low byte should be 0");
        }

        [Test]
        public void Packet_PlayerMoveState_RelayDataPreserved()
        {
            // Simulate a real position relay
            byte[] relayData = new byte[] { 0x68, 0x6F, 0xF1, 0x40, 0x07, 0x7C, 0x0E, 0x72, 0x24, 0xC8, 0x00, 0x01 };

            var inStream = new MemoryStream();
            inStream.WriteByte(0x00);
            inStream.WriteByte((byte)PacketOutFunction.PlayerMoveState);
            inStream.Write(relayData, 0, 12);

            var packet = new Packet(inStream);

            // Relay data should start at offset 5 in PacketData
            // [0-1] length, [2] 0x00, [3] 0x00(from stream), [4] func_id, [5+] data
            for (int i = 0; i < 12; i++)
            {
                Assert.AreEqual(relayData[i], packet.PacketData[5 + i],
                    $"Relay byte {i} mismatch: expected 0x{relayData[i]:X2}, got 0x{packet.PacketData[5 + i]:X2}");
            }
        }

        [Test]
        public void NetHelper_FlipBytes_Int16_RoundTrip()
        {
            Int16 original = 0x1234;
            Int16 flipped = NetHelper.FlipBytes(original);
            Int16 restored = NetHelper.FlipBytes(flipped);
            Assert.AreEqual(original, restored);
        }

        [Test]
        public void NetHelper_FlipBytes_UInt16_RoundTrip()
        {
            UInt16 original = 0xABCD;
            UInt16 flipped = NetHelper.FlipBytes(original);
            UInt16 restored = NetHelper.FlipBytes(flipped);
            Assert.AreEqual(original, restored);
        }

        [Test]
        public void PositionEncoding_DirectionParsesCorrectly()
        {
            // Real client sends direction as 12-bit angle in lower bits of first 2 bytes
            // Direction 1038 (≈1.59 radians) from capture
            ushort rawWord = 1038;
            int rawAngle = rawWord & 0x0FFF;
            float direction = rawAngle * (2f * (float)Math.PI / 4096f);

            Assert.AreEqual(1038, rawAngle);
            Assert.That(direction, Is.InRange(1.5f, 1.7f), "Direction should be ~1.59 radians");
        }

        [Test]
        public void PositionEncoding_ZParsesCorrectly()
        {
            // Z=288 encoded: speed_scalar(4 bits) | Z(11 bits with sign)
            // 288 positive = 0x120, no sign bit
            // With speed scalar 0xF: 0xF120
            ushort rawZ = 0xF120;
            int zPos = rawZ & 0x7FF;
            if ((rawZ & 0x800) != 0) zPos = -zPos;
            int speedScalar = (rawZ >> 12) & 0x0F;

            Assert.AreEqual(288, zPos, "Z should be 288");
            Assert.AreEqual(15, speedScalar, "Speed scalar should be 15");
        }

        [Test]
        public void PositionEncoding_NegativeZ()
        {
            // Z=-100: sign bit set, value=100
            // 100 = 0x64, with sign bit: 0x864
            ushort rawZ = 0xF864;
            int zPos = rawZ & 0x7FF;
            if ((rawZ & 0x800) != 0) zPos = -zPos;

            Assert.AreEqual(-100, zPos, "Z should be -100");
        }
    }
}
