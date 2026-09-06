using System;
using SharpDX;

namespace SpellServer.Commands
{
    public class TpCommand : IChatCommand
    {
        public string Name { get { return "tp"; } }
        public string[] Aliases { get { return new[] { "teleport", "goto" }; } }
        public int MinAdminLevel { get { return 3; } }
        public bool RequiresArena { get { return true; } }

        public void Execute(Player player, ChatCommand cmd)
        {
            var arena = player.ActiveArena;
            var ap = player.ActiveArenaPlayer;

            if (cmd.Arguments.Count == 0)
            {
                // Print nexus positions
                for (int i = 0; i < 3; i++)
                {
                    var shrine = arena.ArenaTeams[i]?.Shrine;
                    if (shrine == null) continue;
                    World.SendSystemMessage(player,
                        String.Format("[TP] {0} nexus: {1} {2} {3}",
                            shrine.Team, shrine.X, shrine.Y, shrine.Z));
                }

                return;
            }

            // Shorthand: !tp d/p/g for nexus, !tp c for center
            if (cmd.Arguments.Count == 1)
            {
                Vector3 dest;
                switch (cmd.Arguments[0].ToLower())
                {
                    case "d": case "dragon":
                        var ds = arena.ArenaTeams.Dragon?.Shrine;
                        if (ds == null) { World.SendSystemMessage(player, "[TP] No Dragon shrine"); return; }
                        dest = new Vector3(ds.X, ds.Y, ds.Z);
                        break;
                    case "p": case "phoenix":
                        var ps = arena.ArenaTeams.Pheonix?.Shrine;
                        if (ps == null) { World.SendSystemMessage(player, "[TP] No Phoenix shrine"); return; }
                        dest = new Vector3(ps.X, ps.Y, ps.Z);
                        break;
                    case "g": case "gryphon":
                        var gs = arena.ArenaTeams.Gryphon?.Shrine;
                        if (gs == null) { World.SendSystemMessage(player, "[TP] No Gryphon shrine"); return; }
                        dest = new Vector3(gs.X, gs.Y, gs.Z);
                        break;
                    case "c": case "center":
                        dest = new Vector3(3950, 4087, 64);
                        break;
                    default:
                        World.SendSystemMessage(player, "[TP] Usage: !tp d/p/g/c or !tp <x> <y> <z>");
                        return;
                }
                arena.PlayerYank(player, ap, ap.ArenaPlayerId, dest);
                World.SendSystemMessage(player, String.Format("[TP] Teleported to {0:F0} {1:F0} {2:F0}", dest.X, dest.Y, dest.Z));
                return;
            }

            if (cmd.Arguments.Count < 3)
            {
                World.SendSystemMessage(player, "[TP] Usage: !tp d/p/g/c or !tp <x> <y> <z>");
                return;
            }

            int x, y, z;
            if (!Int32.TryParse(cmd.Arguments[0], out x) ||
                !Int32.TryParse(cmd.Arguments[1], out y) ||
                !Int32.TryParse(cmd.Arguments[2], out z))
            {
                World.SendSystemMessage(player, "[TP] Invalid coordinates.");
                return;
            }

            var newPos = new Vector3(x, y, z);
            arena.PlayerYank(player, ap, ap.ArenaPlayerId, newPos);
            World.SendSystemMessage(player,
                String.Format("[TP] Teleported to {0} {1} {2}", x, y, z));
        }
    }
}
