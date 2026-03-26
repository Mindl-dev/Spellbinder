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

            if (cmd.Arguments.Count < 3)
            {
                World.SendSystemMessage(player, "[TP] Usage: !tp <x> <y> <z> — or !tp with no args for locations");
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
