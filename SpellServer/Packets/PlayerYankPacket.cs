using Helper.Network;
using System;
using System.IO;

namespace SpellServer.Packets
{
    class PlayerYankPacket : OutPacket
    {
        public override byte Opcode => 0xA7;

        public byte PlayerId { get; }
        public SharpDX.Vector3 Location { get; }

        public PlayerYankPacket(byte playerId, SharpDX.Vector3 location)
        {
            PlayerId = playerId;
            Location = location;
        }

        public override MemoryStream ToBytes()
        {
            MemoryStream outStream = new MemoryStream();
            outStream.WriteByte(0x00);
            outStream.WriteByte((byte)PacketOutFunction.PlayerYank);
            outStream.Write(BitConverter.GetBytes(NetHelper.FlipBytes(PlayerId)), 0, 2);
            outStream.Write(BitConverter.GetBytes(NetHelper.FlipBytes(Convert.ToInt16(Location.X))), 0, 2);
            outStream.Write(BitConverter.GetBytes(NetHelper.FlipBytes(Convert.ToInt16(Location.Y))), 0, 2);
            outStream.Write(BitConverter.GetBytes(NetHelper.FlipBytes(Convert.ToInt16(Location.Z))), 0, 2);
            return outStream;
        }
    }
}
