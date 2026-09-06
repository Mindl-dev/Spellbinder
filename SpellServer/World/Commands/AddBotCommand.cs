using System;
using Helper;

namespace SpellServer.Commands
{
    public class AddBotCommand : IChatCommand
    {
        public string Name { get { return "addbot"; } }
        public string[] Aliases { get { return new[] { "bot" }; } }
        public int MinAdminLevel { get { return 3; } }
        public bool RequiresArena { get { return true; } }

        private static readonly string[] BotNames = { "Shadowmend", "Ironflare", "Voidweaver", "Frostbane", "Flamecrest",
            "Stormcaller", "Ashwalker", "Darkpulse", "Icefang", "Emberheart", "Gravechill", "Dawnfire", "Nightshard",
            "Crystalvein", "Thornmage", "Duskblade", "Spiritforge", "Blazewing", "Mistwalker", "Runekeeper" };

        private static int _nameIndex = 0;

        public void Execute(Player player, ChatCommand cmd)
        {
            var arena = player.ActiveArena;
            if (arena == null) return;

            // Parse team — default to a random non-player team
            Team team = Team.Neutral;
            if (cmd.Arguments.Count > 0)
            {
                switch (cmd.Arguments[0].ToLower())
                {
                    case "dragon": case "d": team = Team.Dragon; break;
                    case "phoenix": case "p": team = Team.Pheonix; break;
                    case "gryphon": case "g": team = Team.Gryphon; break;
                    default:
                        // Try to parse as count
                        int count;
                        if (Int32.TryParse(cmd.Arguments[0], out count))
                        {
                            for (int i = 0; i < count; i++)
                            {
                                SpawnOne(arena, GetNextTeam(arena));
                            }
                            World.SendSystemMessage(player, $"[Bot] Spawned {count} bots");
                            return;
                        }
                        break;
                }
            }

            if (team == Team.Neutral)
                team = GetNextTeam(arena);

            SpawnOne(arena, team);
            World.SendSystemMessage(player, $"[Bot] Spawned bot on {team}");
        }

        private void SpawnOne(Arena arena, Team team)
        {
            string name = BotNames[_nameIndex % BotNames.Length];
            _nameIndex++;

            // Rotate class
            var classes = new[] { Character.PlayerClass.Magician, Character.PlayerClass.Healer,
                Character.PlayerClass.Mystic, Character.PlayerClass.Runemage };
            var botClass = classes[_nameIndex % classes.Length];

            byte level = (byte)Math.Max(1, arena.AveragePlayerLevel);

            Bot.BotPlayer.Create(arena, team, name, botClass, level);
        }

        private Team GetNextTeam(Arena arena)
        {
            // Pick team with fewest players
            int d = arena.ArenaPlayers.GetTeamPlayerCount(Team.Dragon);
            int p = arena.ArenaPlayers.GetTeamPlayerCount(Team.Pheonix);
            int g = arena.ArenaPlayers.GetTeamPlayerCount(Team.Gryphon);

            if (d <= p && d <= g) return Team.Dragon;
            if (p <= g) return Team.Pheonix;
            return Team.Gryphon;
        }
    }
}
