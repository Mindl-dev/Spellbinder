using System;
using System.IO;
using System.Text;
using Helper.Network;

namespace SpellServer
{
    /// <summary>Big-endian packet writer for outgoing server packets.
    /// Automatically writes the leading 0x00 padding byte and func_id.
    /// All multi-byte writes are big-endian (network byte order).</summary>
    public class PacketWriter
    {
        private readonly MemoryStream _stream;

        public PacketWriter(PacketOutFunction funcId)
        {
            _stream = new MemoryStream();
            _stream.WriteByte(0x00);
            _stream.WriteByte((byte)funcId);
        }

        public void WriteByte(byte value)
        {
            _stream.WriteByte(value);
        }

        public void WriteBool(bool value)
        {
            _stream.WriteByte(value ? (byte)1 : (byte)0);
        }

        public void WriteInt16BE(short value)
        {
            _stream.Write(BitConverter.GetBytes(NetHelper.FlipBytes(value)), 0, 2);
        }

        public void WriteUInt16BE(ushort value)
        {
            _stream.Write(BitConverter.GetBytes(NetHelper.FlipBytes(value)), 0, 2);
        }

        public void WriteInt32BE(int value)
        {
            _stream.Write(BitConverter.GetBytes(NetHelper.FlipBytes(value)), 0, 4);
        }

        /// <summary>Write a fixed-length ASCII string, null-padded.</summary>
        public void WriteFixedString(string value, int length)
        {
            byte[] buf = new byte[length];
            if (!string.IsNullOrEmpty(value))
            {
                byte[] raw = Encoding.ASCII.GetBytes(value);
                Array.Copy(raw, 0, buf, 0, Math.Min(raw.Length, length - 1));
            }
            _stream.Write(buf, 0, length);
        }

        public void WriteBytes(byte[] data, int offset, int count)
        {
            _stream.Write(data, offset, count);
        }

        public void WriteBytes(byte[] data)
        {
            _stream.Write(data, 0, data.Length);
        }

        /// <summary>Write N zero bytes.</summary>
        public void WritePadding(int count)
        {
            _stream.Write(new byte[count], 0, count);
        }

        public MemoryStream ToStream()
        {
            return _stream;
        }
    }
}
