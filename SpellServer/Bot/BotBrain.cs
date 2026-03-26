using System;
using System.Collections.Generic;
using Helper;
using Helper.Timing;
using SharpDX;

namespace SpellServer.Bot
{
    public enum BotState
    {
        Idle,
        NavigateToTarget,
        Combat,
        Dead,
        Retreating,
    }

    /// <summary>
    /// Per-bot AI. Ticked at 200ms intervals from BotManager.
    /// </summary>
    public class BotBrain
    {
        private const float MoveSpeed = 300f;     // world units per second
        private const float CastRange = 400f;     // max spell range
        private const int ThinkIntervalMs = 200;
        private const int RepathIntervalMs = 1000;
        private const int CastCooldownMs = 15000;  // Slowed 10x for testing

        public readonly ArenaPlayer Bot;
        public readonly Arena Arena;
        public BotState State;

        private ArenaPlayer _target;
        private List<Vector2> _path;
        private int _pathIndex;
        private Interval _thinkTick;
        private Interval _repathTick;
        private Interval _castTick;

        public BotBrain(ArenaPlayer bot, Arena arena)
        {
            Bot = bot;
            Arena = arena;
            State = BotState.Idle;
            _thinkTick = new Interval(ThinkIntervalMs, true);
            _repathTick = new Interval(RepathIntervalMs, true);
            _castTick = new Interval(CastCooldownMs, true);
        }

        public void Think()
        {
            if (!_thinkTick.HasElapsed) return;

            if (!Bot.IsAlive)
            {
                State = BotState.Dead;
                return;
            }

            // Find target
            if (_target == null || !_target.IsAlive || _target.OwnerArena != Arena)
            {
                _target = FindNearestEnemy();
            }

            if (_target == null)
            {
                State = BotState.Idle;
                return;
            }

            float dist = Vector3.Distance(Bot.Location, _target.Location);

            if (Arena.DebugFlags.HasFlag(ArenaSpecialFlag.ProjectileTracking) && _repathTick.HasElapsed)
            {
                Program.Log($"[BotAI] {Bot.ActiveCharacter?.Name} state={State} target={_target.ActiveCharacter?.Name} dist={dist:F0} pos=({Bot.Location.X:F0},{Bot.Location.Y:F0},{Bot.Location.Z:F0})", System.Drawing.Color.Cyan);
            }

            // In range? Try combat, but keep moving toward target
            if (dist < CastRange)
            {
                State = BotState.Combat;
                FaceTarget(_target);
                TryCast();
            }

            // Always navigate toward target — chase, don't stand still
            if (State != BotState.Combat)
                State = BotState.NavigateToTarget;

            if (_repathTick.HasElapsed || _path == null)
                ComputePathTo(_target.Location);

            FollowPath();
            BotPlayer.BroadcastPosition(Bot);
        }

        private ArenaPlayer FindNearestEnemy()
        {
            ArenaPlayer best = null;
            float bestDist = float.MaxValue;

            for (int i = 0; i < Arena.ArenaPlayers.Count; i++)
            {
                var ap = Arena.ArenaPlayers[i];
                if (ap == null || ap == Bot || !ap.IsAlive) continue;
                if (ap.ActiveTeam == Bot.ActiveTeam) continue;

                float d = Vector3.Distance(Bot.Location, ap.Location);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = ap;
                }
            }
            return best;
        }

        private void ComputePathTo(Vector3 targetPos)
        {
            int startGX, startGY, goalGX, goalGY;
            NavGrid.WorldToGrid(Bot.Location.X, Bot.Location.Y, out startGX, out startGY);
            NavGrid.WorldToGrid(targetPos.X, targetPos.Y, out goalGX, out goalGY);

            startGX = Math.Max(0, Math.Min(127, startGX));
            startGY = Math.Max(0, Math.Min(127, startGY));
            goalGX = Math.Max(0, Math.Min(127, goalGX));
            goalGY = Math.Max(0, Math.Min(127, goalGY));

            if (Arena.NavGrid == null)
            {
                _path = null;
                return;
            }

            _path = Pathfinder.FindPath(Arena.NavGrid, startGX, startGY, goalGX, goalGY);
            _pathIndex = 0;

            if (Arena.DebugFlags.HasFlag(ArenaSpecialFlag.ProjectileTracking))
            {
                bool startWalkable = Arena.NavGrid.Walkability[startGX, startGY] == 0;
                bool goalWalkable = Arena.NavGrid.Walkability[goalGX, goalGY] == 0;
                Program.Log($"[BotPath] {Bot.ActiveCharacter?.Name} from=({startGX},{startGY}) walk={startWalkable} to=({goalGX},{goalGY}) walk={goalWalkable} path={(_path != null ? _path.Count + " nodes" : "NULL")}",
                    System.Drawing.Color.Yellow);
            }
        }

        private void FollowPath()
        {
            // If path is exhausted or null, move directly toward target
            if ((_path == null || _pathIndex >= _path.Count) && _target != null)
            {
                MoveDirectlyToward(_target.Location);
                return;
            }
            if (_path == null || _pathIndex >= _path.Count) return;

            var waypoint = _path[_pathIndex];
            int wpGX = (int)waypoint.X, wpGY = (int)waypoint.Y;
            short floorZ = Arena.NavGrid.FloorHeight[wpGX, wpGY];
            Vector3 wpWorld = NavGrid.GridToWorld(wpGX, wpGY, floorZ);

            float dist = Vector2.Distance(
                new Vector2(Bot.Location.X, Bot.Location.Y),
                new Vector2(wpWorld.X, wpWorld.Y));

            if (dist < 32) // close enough to waypoint
            {
                _pathIndex++;
                if (_pathIndex >= _path.Count) return;
                waypoint = _path[_pathIndex];
                wpGX = (int)waypoint.X; wpGY = (int)waypoint.Y;
                floorZ = Arena.NavGrid.FloorHeight[wpGX, wpGY];
                wpWorld = NavGrid.GridToWorld(wpGX, wpGY, floorZ);
            }

            // Move toward waypoint
            Vector3 dir = wpWorld - Bot.Location;
            if (dir.LengthSquared() > 0)
            {
                dir.Normalize();
                float step = MoveSpeed * (ThinkIntervalMs / 1000f);
                Vector3 newPos = Bot.Location + dir * step;
                newPos.Z = floorZ; // snap to floor

                float direction = (float)Math.Atan2(dir.Y, dir.X);
                Arena.PlayerMove(Bot, ArenaPlayer.StatusFlag.None, 200, newPos, direction);
            }
        }

        private void MoveDirectlyToward(Vector3 targetPos)
        {
            Vector3 dir = targetPos - Bot.Location;
            if (dir.LengthSquared() < 1) return;
            dir.Normalize();
            float step = MoveSpeed * (ThinkIntervalMs / 1000f);
            Vector3 newPos = Bot.Location + dir * step;
            float direction = (float)Math.Atan2(dir.Y, dir.X);
            Arena.PlayerMove(Bot, ArenaPlayer.StatusFlag.None, 200, newPos, direction);
        }

        private void FaceTarget(ArenaPlayer target)
        {
            Vector3 dir = target.Location - Bot.Location;
            if (dir.LengthSquared() > 0)
            {
                float direction = (float)Math.Atan2(dir.Y, dir.X);
                Arena.PlayerMove(Bot, ArenaPlayer.StatusFlag.None, 0, Bot.Location, direction);
            }
        }

        private void TryCast()
        {
            if (!_castTick.HasElapsed) return;
            if (_target == null || !_target.IsAlive) return;

            // Pick Flame Streak I (spell 1) as default attack
            var spell = SpellManager.Spells[1];
            if (spell == null) return;

            // Direct damage via CastTargeted
            Arena.CastTargeted(Bot, _target, spell);
        }
    }
}
