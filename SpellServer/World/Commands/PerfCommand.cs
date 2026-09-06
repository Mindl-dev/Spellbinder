using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Helper;

namespace SpellServer.Commands
{
    public class PerfCommand : IChatCommand
    {
        public string Name { get { return "perf"; } }
        public string[] Aliases { get { return new[] { "profile", "timing" }; } }
        public int MinAdminLevel { get { return 3; } }
        public bool RequiresArena { get { return true; } }

        public void Execute(Player player, ChatCommand cmd)
        {
            var arena = player.ActiveArena;
            if (arena == null) return;

            if (cmd.Arguments.Count > 0 && cmd.Arguments[0].Equals("on", StringComparison.OrdinalIgnoreCase))
            {
                arena.ProfilingEnabled = true;
                World.SendSystemMessage(player, "[Perf] Profiling ON — collecting tick timings. Use !perf to view, !perf off to stop.");
                return;
            }

            if (cmd.Arguments.Count > 0 && cmd.Arguments[0].Equals("off", StringComparison.OrdinalIgnoreCase))
            {
                arena.ProfilingEnabled = false;
                arena.ResetProfiling();
                World.SendSystemMessage(player, "[Perf] Profiling OFF.");
                return;
            }

            if (!arena.ProfilingEnabled)
            {
                World.SendSystemMessage(player, "[Perf] Not active. Use !perf on to start.");
                return;
            }

            // Report results
            var profile = arena.TickProfile;
            long totalTicks = profile.TotalTick.ElapsedTicks;
            if (totalTicks == 0) totalTicks = 1;

            World.SendSystemMessage(player, String.Format(
                "[Perf] Ticks sampled: {0} | Avg tick: {1:F2}ms | Max tick: {2:F2}ms",
                profile.SampleCount,
                profile.TotalTick.AverageMs,
                profile.TotalTick.MaxMs));

            World.SendSystemMessage(player, String.Format(
                "[Perf] Input: {0:F2}ms ({1:F0}%) | Players: {2:F2}ms ({3:F0}%)",
                profile.ProcessInput.AverageMs, 100.0 * profile.ProcessInput.TotalMs / profile.TotalTick.TotalMs,
                profile.ProcessPlayers.AverageMs, 100.0 * profile.ProcessPlayers.TotalMs / profile.TotalTick.TotalMs));

            World.SendSystemMessage(player, String.Format(
                "[Perf] Projectiles: {0:F2}ms ({1:F0}%) | Runes: {2:F2}ms ({3:F0}%)",
                profile.ProcessProjectiles.AverageMs, 100.0 * profile.ProcessProjectiles.TotalMs / profile.TotalTick.TotalMs,
                profile.ProcessRunes.AverageMs, 100.0 * profile.ProcessRunes.TotalMs / profile.TotalTick.TotalMs));

            World.SendSystemMessage(player, String.Format(
                "[Perf] Bolts: {0:F2}ms ({1:F0}%) | Walls: {2:F2}ms ({3:F0}%)",
                profile.ProcessBolts.AverageMs, 100.0 * profile.ProcessBolts.TotalMs / profile.TotalTick.TotalMs,
                profile.ProcessWalls.AverageMs, 100.0 * profile.ProcessWalls.TotalMs / profile.TotalTick.TotalMs));

            World.SendSystemMessage(player, String.Format(
                "[Perf] Triggers: {0:F2}ms ({1:F0}%) | Misc: {2:F2}ms ({3:F0}%)",
                profile.ProcessTriggers.AverageMs, 100.0 * profile.ProcessTriggers.TotalMs / profile.TotalTick.TotalMs,
                profile.ProcessMisc.AverageMs, 100.0 * profile.ProcessMisc.TotalMs / profile.TotalTick.TotalMs));

            World.SendSystemMessage(player, String.Format(
                "[Perf] Lock wait (move): avg {0:F2}ms max {1:F2}ms ({2} contended)",
                profile.MoveWait.AverageMs, profile.MoveWait.MaxMs, profile.MoveWait.ContentionCount));

            profile.Reset();
        }
    }

    /// <summary>Accumulates timing stats for a named phase.</summary>
    public class PhaseTimer
    {
        private long _totalTicks;
        private long _maxTicks;
        private int _count;
        private int _contentionCount;
        private readonly Stopwatch _sw = new Stopwatch();

        public void Start() { _sw.Restart(); }
        public void Stop()
        {
            _sw.Stop();
            long t = _sw.ElapsedTicks;
            Interlocked.Add(ref _totalTicks, t);
            Interlocked.Increment(ref _count);

            long current;
            do { current = Interlocked.Read(ref _maxTicks); }
            while (t > current && Interlocked.CompareExchange(ref _maxTicks, t, current) != current);
        }

        public void RecordContention() { Interlocked.Increment(ref _contentionCount); }

        private static double TicksToMs(long ticks) => ticks * 1000.0 / Stopwatch.Frequency;

        public double TotalMs => TicksToMs(Interlocked.Read(ref _totalTicks));
        public double MaxMs => TicksToMs(Interlocked.Read(ref _maxTicks));
        public double AverageMs { get { int c = _count; return c > 0 ? TotalMs / c : 0; } }
        public long ElapsedTicks => Interlocked.Read(ref _totalTicks);
        public int ContentionCount => _contentionCount;

        public void Reset()
        {
            Interlocked.Exchange(ref _totalTicks, 0);
            Interlocked.Exchange(ref _maxTicks, 0);
            Interlocked.Exchange(ref _count, 0);
            Interlocked.Exchange(ref _contentionCount, 0);
        }
    }

    public class TickProfile
    {
        public PhaseTimer TotalTick = new PhaseTimer();
        public PhaseTimer ProcessInput = new PhaseTimer();
        public PhaseTimer ProcessPlayers = new PhaseTimer();
        public PhaseTimer ProcessProjectiles = new PhaseTimer();
        public PhaseTimer ProcessRunes = new PhaseTimer();
        public PhaseTimer ProcessBolts = new PhaseTimer();
        public PhaseTimer ProcessWalls = new PhaseTimer();
        public PhaseTimer ProcessTriggers = new PhaseTimer();
        public PhaseTimer ProcessMisc = new PhaseTimer();
        public PhaseTimer MoveWait = new PhaseTimer();
        public int SampleCount;

        public void Reset()
        {
            TotalTick.Reset();
            ProcessInput.Reset();
            ProcessPlayers.Reset();
            ProcessProjectiles.Reset();
            ProcessRunes.Reset();
            ProcessBolts.Reset();
            ProcessWalls.Reset();
            ProcessTriggers.Reset();
            ProcessMisc.Reset();
            MoveWait.Reset();
            SampleCount = 0;
        }
    }
}
