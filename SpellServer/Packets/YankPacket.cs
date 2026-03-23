using System.IO;

namespace SpellServer.Packets
{
    [PacketOpcode(0xA4)]
    public class YankPacket : InPacket
    {
        public override byte Opcode => 0xA4;
        public byte TargetId { get; }

        public YankPacket(Player source, MemoryStream inStream, bool isUdp = false)
        {
            Source = source;
            IsUdp = isUdp;

            var reader = new PacketReader(inStream);
            reader.Skip(3);
            TargetId = reader.ReadByte();
        }

        public override void Apply(Arena arena)
        {
            if (Source?.ActiveArena == null || Source.ActiveArenaPlayer == null || !Source.IsAdmin)
            {
                return;
            }

            var targetArenaPlayer = Source.ActiveArena.ArenaPlayers.FindById(TargetId);
            if (targetArenaPlayer == null)
            {
                return;
            }

            Source.ActiveArena.PlayerYank(
                Source,
                targetArenaPlayer,
                Source.ActiveArenaPlayer.ArenaPlayerId,
                Source.ActiveArenaPlayer.Location);
        }
    }
}
