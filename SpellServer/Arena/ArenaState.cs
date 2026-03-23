using Helper;
using Helper.Timing;
using System;

namespace SpellServer
{
    /// <summary>
    /// All mutable state for an arena match.
    /// Owned by the arena tick thread — no other thread should mutate this directly.
    /// </summary>
    public class ArenaState
    {
        // Immutable config
        public ArenaConfig Config { get; }

        // Match progress
        public Arena.State CurrentState;
        public Arena.State EndState;
        public Interval Duration;
        public Interval IdleDuration;
        public DateTime StartTime;
        public TimeSpan ElapsedTime;
        public Int32 ElapsedSeconds;
        public bool IsDurationLocked;
        public Interval CountdownTick;
        public bool StatsProcessed;

        // Teams
        public ArenaTeamCollection ArenaTeams;

        // Entities
        public ArenaPlayerCollection ArenaPlayers;
        public ArenaPlayerCollection ArenaPlayerHistory;
        public ProjectileGroupCollection ProjectileGroups;
        public BoltCollection Bolts;
        public RuneCollection Runes;
        public WallCollection Walls;

        // Runtime
        public Int32 AveragePlayerLevel;
        public ArenaSpecialFlag DebugFlags;

        public ArenaState(ArenaConfig config, ArenaTeamCollection teams)
        {
            Config = config;

            CurrentState = Arena.State.Normal;
            EndState = Arena.State.Normal;
            StartTime = DateTime.UtcNow;
            IsDurationLocked = false;
            StatsProcessed = false;
            AveragePlayerLevel = 1;

            if (config.Ruleset.Rules.HasFlag(ArenaRuleset.ArenaRule.NoTeams))
            {
                Duration = new Interval((config.Grid.TimeLimit / 2) * 1000, false);
            }
            else
            {
                Duration = new Interval(config.Grid.TimeLimit * 1000, false);
            }

            IdleDuration = new Interval(300000, false);

            ArenaTeams = teams;
            ArenaPlayers = new ArenaPlayerCollection();
            ArenaPlayerHistory = new ArenaPlayerCollection();
            ProjectileGroups = new ProjectileGroupCollection();
            Bolts = new BoltCollection();
            Runes = new RuneCollection();
            Walls = new WallCollection();
        }
    }
}
