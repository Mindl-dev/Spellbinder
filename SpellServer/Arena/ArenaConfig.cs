using Helper;
using System;

namespace SpellServer
{
    /// <summary>
    /// Immutable configuration for an arena match.
    /// Set once at creation, never modified during the match.
    /// </summary>
    public class ArenaConfig
    {
        public Byte ArenaId { get; }
        public Byte TableId { get; }
        public Grid Grid { get; }
        public ArenaRuleset Ruleset { get; }
        public String GameName { get; }
        public String ShortGameName { get; }
        public Byte MaxPlayers { get; }
        public Byte LevelRange { get; }
        public Int16 TimeLimit { get; }
        public Int32 FounderCharId { get; }
        public String Founder { get; }
        public Int32 EventExp { get; }
        public Tables Tables { get; }

        public ArenaConfig(
            byte arenaId,
            byte tableId,
            Grid grid,
            ArenaRuleset ruleset,
            byte levelRange,
            int founderCharId,
            string founder,
            int eventExp)
        {
            ArenaId = arenaId;
            TableId = tableId;
            Grid = new Grid(grid);
            Ruleset = ruleset;
            Tables = grid.Tables.GetById(grid.GridId);
            MaxPlayers = grid.MaxPlayers;
            ShortGameName = grid.ShortGameName;
            LevelRange = levelRange;
            FounderCharId = founderCharId;
            Founder = founder;
            EventExp = eventExp;

            GameName = String.Format("[{0}] {1}", ruleset.ModeString, Grid.GameName);
            if (GameName.Length > 19)
            {
                GameName = GameName.Substring(0, 19);
            }

            if (ruleset.Rules.HasFlag(ArenaRuleset.ArenaRule.NoTeams))
            {
                TimeLimit = (Int16)(Grid.TimeLimit / 2);
            }
            else
            {
                TimeLimit = Grid.TimeLimit;
            }
        }
    }
}
