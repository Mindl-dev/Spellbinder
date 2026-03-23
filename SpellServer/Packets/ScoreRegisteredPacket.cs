using System;
using System.IO;
using System.Text;

namespace SpellServer.Packets
{
    class ScoreRegisteredPacket : InPacket
    {
        public int Timestamp { get; set; }
        public int CharLevel { get; set; }
        public int Experience { get; set; }
        public string AccountName { get; set; }
        public int Slot { get; set; }
        public string CharName { get; set; }
        public override byte Opcode => 0xA0;

        public ScoreRegisteredPacket(Player source, MemoryStream inStream)
        {
            Source = source;

            PacketReader reader = new PacketReader(inStream);
            reader.Skip(2);

            Timestamp = reader.ReadInt32BE();
            CharLevel = reader.ReadInt32BE();
            Experience = reader.ReadInt32BE();
            byte[] rawPayload = reader.ReadBytes(110);
            string fullText = Encoding.ASCII.GetString(rawPayload);
            string[] clumps = fullText.Split(new[] { '\0', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            AccountName = clumps.Length > 0 ? clumps[0] : "";
            Slot = clumps.Length > 1 && int.TryParse(clumps[1], out int s) ? s : 0;
            CharName = clumps.Length > 2 ? clumps[2] : "";
        }
    }
}
