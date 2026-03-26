using Helper;
using Helper.Timing;
using SharpDX;
using System;
using System.Linq;
using System.Threading;
using OrientedBoundingBox = Helper.Math.OrientedBoundingBox;

namespace SpellServer
{ 
    public class ArenaPlayer
    {
        private readonly Object _statusFlagsSync = new Object();

        [Flags]
        public enum StatusFlag
        {
            None = 0x00,
            Backwards = 0x08,
            Crouching = 0x10,
            Flying = 0x20,
            Hurt = 0x40,
            Torch = 0x80,
            Dead = Crouching | Flying | Hurt,
        }

        [Flags]
        public enum SpecialFlag
        {
            None,
            God,
        }

        public enum ExperienceType
        {
            Combat,
            Objective,
            Bonus
        }

        public static readonly Vector3 PlayerStandingSize = new Vector3(48, 48, 80);
        public static readonly Vector3 PlayerCrouchingSize = new Vector3(48, 48, 40);
        public static readonly Vector3 PlayerOrigin = new Vector3(24, 24, 0);

        public Arena OwnerArena;

        public Byte ArenaPlayerId;

        public Character ActiveCharacter;
        public Team ActiveTeam; 

        public OrientedBoundingBox BoundingBox;

        public DateTime JoinTime;
        public ArenaPlayer LastAttacker;

        public Interval NonFriendlyWallTime;
        public Interval FriendlyWallTime;
        public Interval InCombatTime;
        public Interval ActiveTime;

        public Vector3 Location;
        public Single Direction;

        // Lag compensation — ring buffer of recent positions
        private const int PositionHistorySize = 30;  // ~500ms at 60Hz
        private Vector3[] _positionHistory = new Vector3[PositionHistorySize];
        private Int64[] _positionTimestamps = new Int64[PositionHistorySize];
        private int _positionHistoryIndex = 0;

        public void RecordPosition(Int64 timestamp)
        {
            _positionHistory[_positionHistoryIndex] = Location;
            _positionTimestamps[_positionHistoryIndex] = timestamp;
            _positionHistoryIndex = (_positionHistoryIndex + 1) % PositionHistorySize;
        }

        /// <summary>Get the player's position at a past time for lag compensation.</summary>
        public Vector3 GetPositionAtTime(Int64 targetTimestamp)
        {
            // Find the two samples bracketing the target time
            int best = -1;
            Int64 bestDelta = Int64.MaxValue;
            for (int i = 0; i < PositionHistorySize; i++)
            {
                if (_positionTimestamps[i] == 0) continue;
                Int64 delta = Math.Abs(_positionTimestamps[i] - targetTimestamp);
                if (delta < bestDelta)
                {
                    bestDelta = delta;
                    best = i;
                }
            }
            return best >= 0 ? _positionHistory[best] : Location;
        }

        public GridBlock CurrentGridBlock;
        public GridBlockFlagData CurrentGridBlockFlagData;

        public SpecialFlag SpecialFlags;
        public Byte MoveSpeed;
        public Effect[] Effects;
        public Int64 LastStateReceived;
        public Int64 LastStateRelayed;
        public Int16 StateReceivedCount;
        public Player WorldPlayer;
        public Boolean HasFliedSinceHackDetect;

        public Interval ValhallaProtection;
        public Interval BiasCooldown;

        private Vector3 _previousLocation;
        private Int16 _previousLocationTick;

        private Int16 _maxHp;
        private Int16 _currentHp;

        private Int16 _deathCount;
        private Int16 _killCount;
        private Int16 _raiseCount;
        
        private Int32 _combatExp;
        private Int32 _objectiveExp;
        private Int32 _bonusExp;

        public Int32 SessionKillExp { get; set; } = 0;

        public Int32 SessionScore { get; set; } = 0;

        private StatusFlag _statusFlags;

        public bool JustLoaded = true;
        public StatusFlag StatusFlags
        {
            get { return _statusFlags; }
            set
            {
                lock (_statusFlagsSync)
                {
                    if (value.HasFlag(StatusFlag.Flying) && HasFliedSinceHackDetect == false)
                    {
                        HasFliedSinceHackDetect = true;
                    }

                    _statusFlags = value;
                }
            }
        }

        public void OnKill(ArenaPlayer killer, ArenaPlayer victim)
        {
            int baseExp = 100; // Example base
            int levelDiff = victim.ActiveCharacter.Level - killer.ActiveCharacter.Level;

            // Scale EXP: Reward more for higher levels, less for "noob-killing"
            int earned = baseExp + (levelDiff * 25);
            if (earned < 10) earned = 10; // Minimum floor

            killer.SessionKillExp += earned;
        }

        public void OnPlayerDeath(ArenaPlayer victim)
        {
            if (victim.ActiveCharacter.Level <= 2) return;

            // Standard 10% loss on death
            int penalty = (int)(victim.SessionKillExp * 0.10f);
            victim.SessionKillExp -= penalty;
        }

        public void OnNodeResurrect(ArenaPlayer player)
        {
            // If they bypass a player-raise, lose an additional 20%
            int nodePenalty = (int)(player.SessionKillExp * 0.20f);
            player.SessionKillExp -= nodePenalty;
        }
        public void AwardFinalMatchExp(Arena arena)
        {
            int timeRemainingSeconds = arena.TimeLimit - (short)arena.elapsedTime.TotalSeconds;
            float timeBonusMultiplier = 1.0f + (timeRemainingSeconds / 600f); // Example: 10% bonus per 1 min left

            foreach (var ap in arena.ArenaPlayers)
            {
                int sessionTotal = ap.CombatExp + ap.ObjectiveExp + ap.BonusExp;
                int finalExp = (int)(sessionTotal * timeBonusMultiplier);

                ap.WorldPlayer.ActiveCharacter.AwardExp = finalExp;

                // Final Database Commit
                MySQL.Character.Save(ap.WorldPlayer.ActiveCharacter, false, ap.WorldPlayer.ActiveCharacter.PlayerFlags);

                // Inform the client
                //Network.Send(ap.WorldPlayer, Outgoing.Arena.GameEndStats(finalExp));
            }
        }
        public Vector3 PreviousLocation
        {
            set
            {
                if (_previousLocationTick >= 3)
                {
                    _previousLocationTick = 0;
                    _previousLocation = value;
                }
                else
                {
                    _previousLocationTick++;
                }
            }

            get { return _previousLocation; }
        }

        public Int16 KillCount
        {
            get { return _killCount; }
            set
            {
                if (value < 0) value = 0;
                if (value > 255) value = 255;

                _killCount = value;
            }
        }
        public Int16 DeathCount
        {
            get { return _deathCount; }
            set
            {
                if (value < 0) value = 0;
                if (value > 255) value = 255;

                _deathCount = value;
            }
        }
        public Int16 RaiseCount
        {
            get { return _raiseCount; }
            set
            {
                if (value < 0) value = 0;
                if (value > 255) value = 255;

                _raiseCount = value;
            }
        }

        public Int32 Points
        {
            get
            {
                return (KillCount - DeathCount) + (RaiseCount/2);
            }
        }

        public Boolean IsAlive
        {
            get { return CurrentHp > 0; }
        }

        public Boolean IsDamageable
        {
            get
            {
                return IsAlive && !WorldPlayer.Flags.HasFlag(PlayerFlag.Hidden) && !SpecialFlags.HasFlag(SpecialFlag.God);
            }
        }


        public Boolean IsInValhalla
        {
            get
            {
                return CurrentGridBlockFlagData.BlockFlag == GridBlockFlag.Valhalla || !ValhallaProtection.HasElapsed;
            }
        }

        public Boolean IsMoving
        {
            get
            {
                return MoveSpeed > 0 || Location != PreviousLocation;
            }
        }

        public Boolean IsInCombat
        {
            get
            {
                return !InCombatTime.HasElapsed;
            }
            set
            {
                if (value)
                {
                    InCombatTime.Reset();
                }
                else
                {
                    InCombatTime.End();
                }

                IsAwayFromKeyboard = false;
            }
        }

        public Boolean IsAwayFromKeyboard
        {
            get
            {
                return ActiveTime.HasElapsed;
            }
            set
            {
                if (value)
                {
                    ActiveTime.End();
                }
                else
                {
                    ActiveTime.Reset();
                }
            }
        }

        public Int16 MaxHp
        {
            get { return _maxHp; }
            set
            {
                if (value < 0) value = 0;
                if (value > 32767) value = 32767;

                _maxHp = value;
            }
        }
        public Int16 CurrentHp
        {
            get { return _currentHp; }
            set
            {
                if (value < 0) value = 0;
                if (value > MaxHp) value = MaxHp;
                if (value > 32767) value = 32767;

                _currentHp = value;

                if (StatusFlags.HasFlag(StatusFlag.Hurt))
                {
                    if ((Single)_currentHp / _maxHp > 0.65f)
                    {
                        StatusFlags &= ~StatusFlag.Hurt;
                    }
                }
                else
                {
                    if ((Single)_currentHp / _maxHp <= 0.65f)
                    {
                        StatusFlags |= StatusFlag.Hurt;
                    }
                }
            }
        }

        public Int32 CombatExp
        {
            get { return _combatExp; }
            set
            {
                if (WorldPlayer.Flags.HasFlag(PlayerFlag.ExpLocked)) return;

                if (value < 0) value = 0;
                if (value > 999999) value = 999999;

                if (WorldPlayer.ActiveArena != null)
                {
                    _combatExp = value;

                    Network.Send(WorldPlayer, GamePacket.Outgoing.Arena.UpdateExperience(this));
                }
            }
        }
        public Int32 ObjectiveExp
        {
            get { return _objectiveExp; }
            set
            {
                if (WorldPlayer.Flags.HasFlag(PlayerFlag.ExpLocked)) return;

                if (value < 0) value = 0;
                if (value > 999999) value = 999999;

                if (WorldPlayer.ActiveArena != null)
                {
                    _objectiveExp = value;

                    Network.Send(WorldPlayer, GamePacket.Outgoing.Arena.UpdateExperience(this));
                }
            }
        }
        public Int32 BonusExp
        {
            get { return _bonusExp; }
            set
            {
                if (WorldPlayer.Flags.HasFlag(PlayerFlag.ExpLocked)) return;

                if (value < 0) value = 0;
                if (value > 999999) value = 999999;

                if (WorldPlayer.ActiveArena != null)
                {
                    _bonusExp = value;

                    Network.Send(WorldPlayer, GamePacket.Outgoing.Arena.UpdateExperience(this));
                }
            }
        }
        public Int32 TotalExp
        {
            get { return CombatExp + ObjectiveExp + BonusExp; }
        }

        public Int32 ExpPenalty
        {
            set
            {
                Single normalPenalty = (Single)Math.Ceiling(value * 0.1f);
                Single objectivePenalty = normalPenalty;

                if (normalPenalty > CombatExp)
                {
                    objectivePenalty += normalPenalty - CombatExp;
                    normalPenalty = CombatExp;
                }

                if (objectivePenalty > ObjectiveExp)
                {
                    normalPenalty += objectivePenalty - ObjectiveExp;
                    objectivePenalty = ObjectiveExp;
                }

                CombatExp -= (Int16)normalPenalty;
                ObjectiveExp -= (Int16)objectivePenalty;
            }
        }

        public Int32 SecondsPlayed
        {
            get
            {
                return (Int16)DateTime.Now.Subtract(JoinTime).TotalSeconds;
            }
        }

        public Shrine ActiveShrine
        {
            get { return OwnerArena.Grid.GetShrineByTeam(ActiveTeam); }
        }

        public ArenaPlayer(Player player, Arena arena)
        {
            lock (arena.SyncRoot)
            {
                WorldPlayer = player;
                OwnerArena = arena;

                lock (OwnerArena.ArenaPlayers.SyncRoot)
                {
                    // 1. Clean up ANY stale instance of this player first
                    //var existing = OwnerArena.ArenaPlayers.FirstOrDefault(ap => ap.WorldPlayer.PlayerId == player.PlayerId);
                    //if (existing != null) OwnerArena.PlayerLeft(existing);

                    // 2. Assign a ROLLING ID, not the 'first available'
                    ArenaPlayerId = OwnerArena.ArenaPlayers.GetNextRollingId();
                }

                Program.Log($"ArenaPlayerId: {ArenaPlayerId}, ArenaPlayerName: {player.ActiveCharacter.Name}, Team: {WorldPlayer.ActiveTeam}", System.Drawing.Color.Red);

                if (ArenaPlayerId == 0) return;

                WorldPlayer.PingInitialized = false;
                WorldPlayer.TableId = 0;
                WorldPlayer.ActiveArena = arena;
                WorldPlayer.LastArenaId = arena.ArenaId;

                ActiveTeam = OwnerArena.Ruleset.Rules.HasFlag(ArenaRuleset.ArenaRule.NoTeams) ? Team.Neutral : WorldPlayer.ActiveTeam;
                ActiveCharacter = player.ActiveCharacter;

                // Spawn at team's shrine position
                Shrine teamShrine = null;
                switch (ActiveTeam)
                {
                    case Team.Dragon: teamShrine = arena.ArenaTeams.Dragon?.Shrine; break;
                    case Team.Pheonix: teamShrine = arena.ArenaTeams.Pheonix?.Shrine; break;
                    case Team.Gryphon: teamShrine = arena.ArenaTeams.Gryphon?.Shrine; break;
                }
                Vector3 spawnPos = (teamShrine != null && (teamShrine.X != 0 || teamShrine.Y != 0))
                    ? new Vector3(teamShrine.X, teamShrine.Y, teamShrine.Z)
                    : new Vector3(0, 0, 0);

                _previousLocation = spawnPos;
                _previousLocationTick = 0;

                Location = spawnPos;
                Direction = 0;

                CurrentGridBlock = null;
                CurrentGridBlockFlagData = new GridBlockFlagData();

                InCombatTime = new Interval(7000, false);
                NonFriendlyWallTime = new Interval(1000, false);
                FriendlyWallTime = new Interval(1000, false);
                ValhallaProtection = new Interval(2000, false);
                BiasCooldown = new Interval(2000, true); // 2 seconds between bias attempts
                ActiveTime = new Interval(0, false);
                BoundingBox = new OrientedBoundingBox(Location, PlayerStandingSize, 0.0f);

                StatusFlags = StatusFlag.None;
                SpecialFlags = SpecialFlag.None;

                Effects = new Effect[21];

                MoveSpeed = 0;
                StateReceivedCount = 0;
                LastStateReceived = NativeMethods.PerformanceCount;
                LastStateRelayed = 0;

                LastAttacker = null;

                JoinTime = DateTime.Now;

                HasFliedSinceHackDetect = false;

                MaxHp = player.ActiveCharacter.MaxHealth;

                if (ActiveShrine == null)
                {
                    if (ActiveTeam == Team.Neutral)
                    {
                        CurrentHp = MaxHp;
                    }
                    else return;
                }
                else
                {
                    if (ActiveShrine.IsDisabled)
                    {

                        Network.Send(WorldPlayer, GamePacket.Outgoing.Player.SendPlayerId(this));
                        Network.Send(WorldPlayer, GamePacket.Outgoing.Arena.SuccessfulArenaEntry());
                        OwnerArena.ArenaKickPlayer(this);

                        return;
                    }

                    CurrentHp = ActiveShrine.IsDead ? (Int16)0 : MaxHp;
                }

                Network.Send(WorldPlayer, GamePacket.Outgoing.Player.SendPlayerId(this));

                //if (!WorldPlayer.Flags.HasFlag(PlayerFlag.Hidden))
                //{
                    Network.SendTo(WorldPlayer, GamePacket.Outgoing.World.PlayerLeave(WorldPlayer), Network.SendToType.Tavern, false);
                    Network.SendTo(WorldPlayer, GamePacket.Outgoing.World.PlayerJoin(WorldPlayer), Network.SendToType.Tavern, false);

                    Network.SendToArena(this, GamePacket.Outgoing.Arena.PlayerJoin(this), false);
                //}

                if (OwnerArena.ArenaPlayerHistory.FindByCharacterId(WorldPlayer.ActiveCharacter.CharacterId) == null)
                {
                    OwnerArena.ArenaPlayerHistory.Add(this);
                }

                WorldPlayer.ActiveArenaPlayer = this;
                OwnerArena.ArenaPlayers.Add(this);

                OwnerArena.AveragePlayerLevel = OwnerArena.ArenaPlayers.GetAveragePlayerLevel();
            }

            var sw = System.Diagnostics.Stopwatch.StartNew();

            lock (OwnerArena.SyncRoot)
            {
                Network.Send(WorldPlayer, GamePacket.Outgoing.Arena.UpdateShrinePoolState(arena, this, true));
            }

            Program.Log($"[ArenaJoin] ShrinePoolState: {sw.ElapsedMilliseconds}ms", System.Drawing.Color.Blue);


            Network.Send(WorldPlayer, GamePacket.Outgoing.Arena.SuccessfulArenaEntry());

            Program.Log($"[ArenaJoin] SuccessfulEntry: {sw.ElapsedMilliseconds}ms", System.Drawing.Color.Blue);


            lock (OwnerArena.SyncRoot)
            {
                for (Int32 i = 0; i < OwnerArena.Runes.Count; i++)
                {
                    Network.Send(WorldPlayer, GamePacket.Outgoing.Arena.CastRune(this, OwnerArena.Runes[i].RawData));
                }
            }

            Program.Log($"[ArenaJoin] Runes ({OwnerArena.Runes.Count}): {sw.ElapsedMilliseconds}ms", System.Drawing.Color.Blue);


            lock (OwnerArena.SyncRoot)
            {
                for (Int32 i = 0; i < OwnerArena.Walls.Count; i++)
                {
                    Network.Send(WorldPlayer, GamePacket.Outgoing.Arena.CastWall(arena.Walls[i].RawData));
                }
            }

            Program.Log($"[ArenaJoin] Walls ({OwnerArena.Walls.Count}): {sw.ElapsedMilliseconds}ms", System.Drawing.Color.Blue);


            lock (OwnerArena.SyncRoot)
            {
                for (Int32 i = 0; i < arena.Grid.Triggers.Count; i++)
                {
                    Network.Send(WorldPlayer, GamePacket.Outgoing.Arena.ActivatedTrigger(OwnerArena.Grid.Triggers[i]));
                }
            }

            Program.Log($"[ArenaJoin] Triggers ({arena.Grid.Triggers.Count}): {sw.ElapsedMilliseconds}ms", System.Drawing.Color.Blue);

            if (OwnerArena.Ruleset.Mode == ArenaRuleset.ArenaMode.Custom)
            {
                Network.Send(WorldPlayer, GamePacket.Outgoing.System.DirectTextMessage(WorldPlayer, String.Format("This arena has the following rules: {0}.", arena.Ruleset.Rules)));
            }

            if (OwnerArena.Ruleset.Rules.HasFlag(ArenaRuleset.ArenaRule.ExpEvent))
            {
                Network.Send(WorldPlayer, GamePacket.Outgoing.System.DirectTextMessage(WorldPlayer, String.Format("If your team wins this match, you will earn {0:0,0} experience.", (WorldPlayer.Flags.HasFlag(PlayerFlag.MagestormPlus) ? OwnerArena.EventExp * 2f : OwnerArena.EventExp))));
            }

            World.UpdateAllArenaPlayers(this.WorldPlayer);

            Program.Log($"[ArenaJoin] UpdateAllPlayers: {sw.ElapsedMilliseconds}ms", System.Drawing.Color.Blue);

            Network.Send(this.WorldPlayer, GamePacket.Outgoing.Study.CabalIDUpdate(this.WorldPlayer));

            Program.Log($"[ArenaJoin] Total: {sw.ElapsedMilliseconds}ms", System.Drawing.Color.Blue);
            sw.Stop();

            // Yank player to their team's shrine on first entry
            if (Location.X != 0 || Location.Y != 0)
            {
                var spawnYank = new Packets.PlayerYankPacket(ArenaPlayerId, Location).ToBytes();
                Network.Send(WorldPlayer, spawnYank);
            }

            //Network.Send(WorldPlayer, GamePacket.Outgoing.System.DirectTextMessage(WorldPlayer, String.Format("This arena currently has an EXP bonus of {0}%.", ((arena.Grid.ExpBonus + (Properties.Settings.Default.ExpMultiplier - 1.0f) + (WorldPlayer.Flags.HasFlag(PlayerFlag.MagestormPlus) ? 0.2f : 0.0f)) * 100))));
        }
    }
}
