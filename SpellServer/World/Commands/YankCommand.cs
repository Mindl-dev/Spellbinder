using System;

namespace SpellServer.Commands
{
    public class YankCommand : IChatCommand
    {
        public string Name { get { return "yank"; } }
        public string[] Aliases { get { return null; } }
        public int MinAdminLevel { get { return 3; } } // Staff+
        public bool RequiresArena { get { return true; } }

        public void Execute(Player player, ChatCommand cmd)
        {
            if (cmd.Arguments.Count < 1)
            {
                World.SendSystemMessage(player, "[System] Usage: !yank <targetname> — teleports target to you.");
                return;
            }

            ArenaPlayer targetArenaPlayer = player.ActiveArena.ArenaPlayers.FindByCharacterName(cmd.Arguments[0]);
            if (targetArenaPlayer != null)
            {
                var fromPos = targetArenaPlayer.Location;
                var toPos = player.ActiveArenaPlayer.Location;
                player.ActiveArena.PlayerYank(player, targetArenaPlayer,
                    targetArenaPlayer.ArenaPlayerId, toPos);
                Program.Log(String.Format("[Yank] {0} yanked {1} from ({2:F0},{3:F0},{4:F0}) to ({5:F0},{6:F0},{7:F0})",
                    player.ActiveCharacter.Name, targetArenaPlayer.ActiveCharacter.Name,
                    fromPos.X, fromPos.Y, fromPos.Z, toPos.X, toPos.Y, toPos.Z),
                    System.Drawing.Color.Magenta);
                World.SendSystemMessage(player,
                    String.Format("[System] Yanked {0} to your location.", targetArenaPlayer.ActiveCharacter.Name));
            }
            else
            {
                World.SendSystemMessage(player, Resources.Strings_Commands.General_NoTargetsFound);
            }
        }
    }
}
