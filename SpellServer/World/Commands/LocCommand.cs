using System;

namespace SpellServer.Commands
{
    public class LocCommand : IChatCommand
    {
        public string Name { get { return "loc"; } }
        public string[] Aliases { get { return new[] { "location", "pos" }; } }
        public int MinAdminLevel { get { return 0; } }
        public bool RequiresArena { get { return true; } }

        public void Execute(Player player, ChatCommand cmd)
        {
            var pos = player.ActiveArenaPlayer.Location;
            World.SendSystemMessage(player,
                String.Format("[Loc] X={0:F0} Y={1:F0} Z={2:F0}", pos.X, pos.Y, pos.Z));
        }
    }
}
