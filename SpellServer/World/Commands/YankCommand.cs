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
                player.ActiveArena.PlayerYank(player, targetArenaPlayer,
                    targetArenaPlayer.ArenaPlayerId, player.ActiveArenaPlayer.Location);
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
