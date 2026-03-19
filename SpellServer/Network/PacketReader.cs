using System;
using System.IO;
using System.Text;
using Helper.Network;

namespace SpellServer
{
    /// <summary>Big-endian packet reader for incoming client packets.
    /// Wraps a MemoryStream and handles byte-flipping automatically.
    /// Skips the 2-byte (1B1B) leading padding that every handler currently does manually.</summary>
    public class PacketReader
    {
        private readonly MemoryStream _stream;

        public PacketReader(MemoryStream stream, bool skipPadding = true)
        {
            _stream = stream;
            if (skipPadding)
                _stream.Seek(2, SeekOrigin.Begin);
        }

        public long Position
        {
            get { return _stream.Position; }
            set { _stream.Position = value; }
        }

        public long Length { get { return _stream.Length; } }

        public byte ReadByte()
        {
            int b = _stream.ReadByte();
            if (b < 0) throw new EndOfStreamException();
            return (byte)b;
        }

        public bool ReadBool()
        {
            return ReadByte() != 0;
        }

        public short ReadInt16BE()
        {
            byte[] buf = new byte[2];
            _stream.Read(buf, 0, 2);
            return NetHelper.FlipBytes(BitConverter.ToInt16(buf, 0));
        }

        public ushort ReadUInt16BE()
        {
            byte[] buf = new byte[2];
            _stream.Read(buf, 0, 2);
            return NetHelper.FlipBytes(BitConverter.ToUInt16(buf, 0));
        }

        public int ReadInt32BE()
        {
            byte[] buf = new byte[4];
            _stream.Read(buf, 0, 4);
            return NetHelper.FlipBytes(BitConverter.ToInt32(buf, 0));
        }

        public string ReadFixedString(int length)
        {
            byte[] buf = new byte[length];
            _stream.Read(buf, 0, length);
            int end = Array.IndexOf(buf, (byte)0);
            if (end < 0) end = length;
            return Encoding.ASCII.GetString(buf, 0, end);
        }

        public byte[] ReadBytes(int count)
        {
            byte[] buf = new byte[count];
            _stream.Read(buf, 0, count);
            return buf;
        }

        public void Skip(int count)
        {
            _stream.Seek(count, SeekOrigin.Current);
        }

        /// <summary>Read remaining bytes from current position.</summary>
        public byte[] ReadRemaining()
        {
            int remaining = (int)(_stream.Length - _stream.Position);
            return ReadBytes(remaining);
        }
    }
}
