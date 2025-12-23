using Helper;
using Helper.Math;
using Helper.Timing;
using SpellServer.Properties;
using SharpDX;
using System;
using System.Linq;
using System.Threading;
using System.Timers;
using static SpellServer.ArenaRuleset;
using Color = System.Drawing.Color;
using OrientedBoundingBox = Helper.Math.OrientedBoundingBox;

namespace SpellServer
{
    [Flags]
    public enum ArenaSpecialFlag
    {
        None = 0x00,
        ProjectileTracking = 0x01,
        PlayerTracking = 0x02,
        RuneTracking = 0x04,
        ThinTracking = 0x08,
        OneDamageToPlayers = 0x10,
    }

    public class Arena
    {
        public enum State
        {
            Normal = 0,
            Ended = 1,
            OneMinute = 2,
            DragonVictory = 3,
            PheonixVictory = 4,
            GryphonVictory = 5,
            CleanUp = 255,
        }

        public readonly Object SyncRoot = new Object();
	    private readonly Thread WorkerThread;

	    private const Int32 TickRate = 5;

        public ArenaTeamCollection ArenaTeams;
        public ArenaPlayerCollection ArenaPlayers;
        public ArenaPlayerCollection ArenaPlayerHistory;
        public BoltCollection Bolts;
        public ProjectileGroupCollection ProjectileGroups;
        public WallCollection Walls;
        public RuneCollection Runes;

        public ArenaRuleset Ruleset;

        public Byte ArenaId;
	    public Int64 MatchId;
        public Byte TableId;
        public Grid Grid;
        public Interval Duration;
	    public Interval IdleDuration;
        public State CurrentState;
        public State EndState;

		public Int32 FounderCharId;
        public String Founder;
        public String GameName;
        public Byte LevelRange;
        public Byte MaxPlayers;
        public Int16 TimeLimit;
        public String ShortGameName;
        public DateTime StartTime;
        public TimeSpan elaspedTime;
        public Int32 elaspedSeconds;
        public Int32 baseTime = 100000000;

        public Int32 AveragePlayerLevel;
        public Int32 EventExp;

        public Interval TriggerPulseTick;
        public Interval ProcessingTick;
        public Interval CountdownTick;
        public Interval HealthRegenTick;
        public Interval GuildRulesBroadcast;

        public Interval PlayerTrackingTick;
        public Interval ProjectileTrackingTick;
        public Interval ThinTrackingTick;
        public Interval RuneTrackingTick;

        public Boolean IsDurationLocked;

        public ArenaSpecialFlag DebugFlags;

        public Single CurrentTickDelta;

        public Arena(Player player, Grid grid, Byte levelRange, ArenaRuleset ruleset)
        {
            lock (ArenaManager.Arenas.SyncRoot)
            {
                ArenaId = ArenaManager.Arenas.GetAvailableArenaId();
                if (ArenaId == 0) return;

                if (ruleset.Rules.HasFlag(ArenaRuleset.ArenaRule.ExpEvent) && player.PreferredEventExp <= 0)
                {
                    Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, "[System] Your arena has not been created. You have not set an event exp amount."));
                    return;
                }

                Table table = TableManager.Tables.FindById(player.TableId);
                if (table == null)
                {
                    TableId = 0;
                }
                else
                {
                    switch (table.Type)
                    {
						case TableType.Public:
	                    {
							TableId = 0;
		                    break;
	                    }
                        case TableType.Private:
	                    {
		                    TableId = player.TableId;
		                    break;
	                    }
	                    default:
	                    {
		                    TableId = 0;
		                    break;
	                    }
                    }
                }

                Ruleset = ruleset;
                Grid = new Grid(grid);
                ArenaTeams = new ArenaTeamCollection(Grid);
                ArenaPlayers = new ArenaPlayerCollection();
                ArenaPlayerHistory = new ArenaPlayerCollection();
                Runes = new RuneCollection();
                Walls = new WallCollection();
                Bolts = new BoltCollection();
                ProjectileGroups = new ProjectileGroupCollection();

                if (ArenaTeams.Dragon.Shrine.Power == 0) ArenaTeams.Dragon.Shrine.IsDisabled = true;
                if (ArenaTeams.Pheonix.Shrine.Power == 0) ArenaTeams.Pheonix.Shrine.IsDisabled = true;
                if (ArenaTeams.Gryphon.Shrine.Power == 0) ArenaTeams.Gryphon.Shrine.IsDisabled = true;

                if (ArenaTeams.DisabledShrineCount == 0)
                {
                    if (Ruleset.Rules.HasFlag(ArenaRuleset.ArenaRule.TwoTeams))
                    {
                        ArenaTeams.Dragon.Shrine.IsDisabled = false;
                        ArenaTeams.Pheonix.Shrine.IsDisabled = false;
                        ArenaTeams.Gryphon.Shrine.IsDisabled = true;
                    }
                }

                if (Ruleset.Rules.HasFlag(ArenaRuleset.ArenaRule.NoTeams))
                {
                    ArenaTeams.Dragon.Shrine.IsDisabled = true;
                    ArenaTeams.Pheonix.Shrine.IsDisabled = true;
                    ArenaTeams.Gryphon.Shrine.IsDisabled = true;
                }

                if (!Ruleset.Rules.HasFlag(ArenaRuleset.ArenaRule.NoTeams) && ArenaTeams.DisabledShrineCount >= 2)
                {
                    Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, "[System] Error creating arena."));
                    return;
                }

                TriggerPulseTick = new Interval(5000, true);
                ProcessingTick = new Interval(TickRate, false);
                HealthRegenTick = new Interval(750, true);
                GuildRulesBroadcast = new Interval(600000, true);
                RuneTrackingTick = new Interval(1000, true);
                ProjectileTrackingTick = new Interval(100, true);
                PlayerTrackingTick = new Interval(10, true);
                ThinTrackingTick = new Interval(3000, true);

                GameName = String.Format("[{0}] {1}", ruleset.ModeString, Grid.GameName);

                if (GameName.Length > 19)
                {
                    GameName = GameName.Substring(0, 19);
                }

                if (Ruleset.Rules.HasFlag(ArenaRuleset.ArenaRule.NoTeams))
                {
                    Duration = new Interval((Grid.TimeLimit / 2) * 1000, false);
                    TimeLimit = (Int16)(Grid.TimeLimit/2);

                }
                else
                {
                    Duration = new Interval(Grid.TimeLimit * 1000, false);
                    TimeLimit = Grid.TimeLimit;
                }

				IdleDuration = new Interval(300000, false);
                ShortGameName = Grid.ShortGameName;
	            FounderCharId = player.ActiveCharacter.CharacterId;
                Founder = player.ActiveCharacter.Name;
                LevelRange = levelRange;
                CurrentState = State.Normal;
                MaxPlayers = Grid.MaxPlayers;
                EndState = State.Normal;
                IsDurationLocked = false;
                DebugFlags = ArenaSpecialFlag.None;

                StartTime = DateTime.UtcNow;

                if (ruleset.Rules.HasFlag(ArenaRuleset.ArenaRule.ExpEvent))
                {
                    Program.ServerForm.AdminLog.WriteMessage(String.Format("[Admin] ({0}){1} -> Created an Event Exp Game ({2} EXP)", player.AccountId, player.ActiveCharacter.Name, player.PreferredEventExp), Color.Blue);
                    
                    EventExp = player.PreferredEventExp;
                }
                else
                {
                    EventExp = 0;
                }

                AveragePlayerLevel = 1;

	            MatchId = MySQL.Matches.Created(ArenaId, TableId, Duration.CreationTime.GetUnixTime(), ArenaPlayers.Count, ArenaPlayers.HighestPlayerCount, MaxPlayers, CurrentState, EndState, ShortGameName, GameName, FounderCharId, (Int32)((Duration.Duration/1000)/60), LevelRange, Ruleset.Mode, Ruleset.Rules);
               
				WorkerThread = new Thread(ProcessArena);
                WorkerThread.Start();

                ArenaManager.Arenas.Add(this);
            }
        }

        public Team WinningTeam
        {
            get
            {
                lock (SyncRoot)
                {
                    State teamState = CurrentState == State.Ended || CurrentState == State.CleanUp ? EndState : CurrentState;

                    if ((!ArenaTeams.Gryphon.Shrine.IsDamaged || Ruleset.Rules.HasFlag(ArenaRuleset.ArenaRule.CaptureTheFlag)) && !ArenaTeams.Gryphon.Shrine.IsIndestructible)
                    {
                        if (teamState == State.GryphonVictory)
                        {
                            if ((ArenaTeams.Dragon.Shrine.IsDamaged || ArenaTeams.Dragon.Shrine.IsIndestructible) && (ArenaTeams.Pheonix.Shrine.IsDamaged || ArenaTeams.Pheonix.Shrine.IsIndestructible))
                            {
                                return Team.Gryphon;
                            }
                        }
                        else
                        {
                            if ((ArenaTeams.Dragon.Shrine.IsDead || ArenaTeams.Dragon.Shrine.IsIndestructible) && (ArenaTeams.Pheonix.Shrine.IsDead || ArenaTeams.Pheonix.Shrine.IsIndestructible))
                            {
                                return Team.Gryphon;
                            }
                        }
                    }

                    if ((!ArenaTeams.Pheonix.Shrine.IsDamaged || Ruleset.Rules.HasFlag(ArenaRuleset.ArenaRule.CaptureTheFlag)) && !ArenaTeams.Pheonix.Shrine.IsIndestructible)
                    {
                        if (teamState == State.PheonixVictory)
                        {
                            if ((ArenaTeams.Dragon.Shrine.IsDamaged || ArenaTeams.Dragon.Shrine.IsIndestructible) && (ArenaTeams.Gryphon.Shrine.IsDamaged || ArenaTeams.Gryphon.Shrine.IsIndestructible))
                            {
                                return Team.Pheonix;
                            }
                        }
                        else
                        {
                            if ((ArenaTeams.Dragon.Shrine.IsDead || ArenaTeams.Dragon.Shrine.IsIndestructible) && (ArenaTeams.Gryphon.Shrine.IsDead || ArenaTeams.Gryphon.Shrine.IsIndestructible))
                            {
                                return Team.Pheonix;
                            }
                        }
                    }

                    if ((!ArenaTeams.Dragon.Shrine.IsDamaged || Ruleset.Rules.HasFlag(ArenaRuleset.ArenaRule.CaptureTheFlag)) && !ArenaTeams.Dragon.Shrine.IsIndestructible)
                    {
                        if (teamState == State.DragonVictory)
                        {
                            if ((ArenaTeams.Gryphon.Shrine.IsDamaged || ArenaTeams.Gryphon.Shrine.IsIndestructible) && (ArenaTeams.Pheonix.Shrine.IsDamaged || ArenaTeams.Pheonix.Shrine.IsIndestructible))
                            {
                                return Team.Dragon;
                            }
                        }
                        else
                        {
                            if ((ArenaTeams.Gryphon.Shrine.IsDead || ArenaTeams.Gryphon.Shrine.IsIndestructible) && (ArenaTeams.Pheonix.Shrine.IsDead || ArenaTeams.Pheonix.Shrine.IsIndestructible))
                            {
                                return Team.Dragon;
                            }
                        }
                    }

                    if (Ruleset.Rules.HasFlag(ArenaRuleset.ArenaRule.GuildRules) && (CurrentState == State.Ended || CurrentState == State.CleanUp))
                    {
                        if (ArenaTeams.Gryphon.Shrine.GuildPoints > ArenaTeams.Dragon.Shrine.GuildPoints && ArenaTeams.Gryphon.Shrine.GuildPoints > ArenaTeams.Pheonix.Shrine.GuildPoints)
                        {
                            EndState = State.GryphonVictory;
                            return Team.Gryphon;
                        }
                        if (ArenaTeams.Pheonix.Shrine.GuildPoints > ArenaTeams.Dragon.Shrine.GuildPoints && ArenaTeams.Pheonix.Shrine.GuildPoints > ArenaTeams.Gryphon.Shrine.GuildPoints)
                        {
                            EndState = State.PheonixVictory;
                            return Team.Pheonix;
                        }
                        if (ArenaTeams.Dragon.Shrine.GuildPoints > ArenaTeams.Gryphon.Shrine.GuildPoints && ArenaTeams.Dragon.Shrine.GuildPoints > ArenaTeams.Pheonix.Shrine.GuildPoints)
                        {
                            EndState = State.DragonVictory;
                            return Team.Dragon;
                        }
                    }
                }

                return Team.Neutral;
            }
        }

        private void ProcessArena()
        {
            while (CurrentState != State.Ended)
            {
                if (!ProcessingTick.HasElapsed)
                {
                    Thread.Sleep(1);
                    continue;
                }

                CurrentTickDelta = ProcessingTick.Delta;
                ProcessingTick.Reset();

                lock (SyncRoot)
                {
                    try
                    {
                        ProcessArenaPlayers();
                        ProcessProjectiles();
                        ProcessRunes();
                        ProcessBolts();
                        ProcessWalls();
                        ProcessTriggers();
                        ProcessMisc();

                        elaspedTime = DateTime.UtcNow - StartTime;
                        elaspedSeconds = (int)elaspedTime.TotalSeconds;
                    }
                    catch (Exception ex)
                    {
                        Program.ServerForm.MainLog.WriteMessage(String.Format("[Arena Exception] {0}", ex.GetStackTrace()), Color.Red);
                        
                        EndState = State.Ended;
                        EndMatch(false);

                        return;
                    }

                }
            }

            EndMatch(true);
        }

        public void EndMatch(Boolean isCleanEnding)
        {
            lock (SyncRoot)
            {
                if (isCleanEnding)
                {
                    Team winningTeam = WinningTeam;

                    ListCollection<ArenaPlayer> top10Points = ArenaPlayers.FindTop10Points();

                    for (Int32 i = 0; i < ArenaPlayers.Count; i++)
                    {
                        ArenaPlayer arenaPlayer = ArenaPlayers[i];
                        if (arenaPlayer == null) continue;

                        if (arenaPlayer.ActiveTeam == winningTeam)
                        {
                            if (Ruleset.Rules.HasFlag(ArenaRuleset.ArenaRule.ExpEvent) && arenaPlayer.SecondsPlayed >= 120)
                            {
                                Int32 awardedExp;

                                if (arenaPlayer.WorldPlayer.Flags.HasFlag(PlayerFlag.MagestormPlus))
                                {
                                    awardedExp = EventExp*2;
                                }
                                else
                                {
                                    awardedExp = EventExp;
                                }

                                arenaPlayer.ActiveCharacter.AwardExp += awardedExp;

                                Program.ServerForm.AdminLog.WriteMessage(String.Format("[Event] ({0}){1} -> Has been awarded {2} EXP by {3}.", arenaPlayer.WorldPlayer.AccountId, arenaPlayer.WorldPlayer.ActiveCharacter.Name, awardedExp, arenaPlayer.WorldPlayer.ActiveArena.Founder), Color.Blue);
                            }

                            Int32 pointsPlace = top10Points.FindIndex(indexPlayer => indexPlayer == arenaPlayer);

                            switch (pointsPlace)
                            {
                                case 0:
                                {
                                    pointsPlace = 10;
                                    break;
                                }
                                case 1:
                                {
                                    pointsPlace = 8;
                                    break;
                                }
                                case 2:
                                {
                                    pointsPlace = 7;
                                    break;
                                }
                                default:
                                {
                                    pointsPlace = 5;
                                    break;
                                }
                            }

                            Single pointsBonus = pointsPlace * ((arenaPlayer.ActiveCharacter.Level * (25 + (arenaPlayer.KillCount * 2.2f))) + (arenaPlayer.ActiveCharacter.Level * (10 + (arenaPlayer.RaiseCount * 1.3f))));
                            
                            Single bonusTime = (((Single)DateTime.Now.Subtract(arenaPlayer.JoinTime).TotalSeconds / (TimeLimit / 100f)) * 0.006f) + 1f;
                            Int32 bonusExp = (Int32)(((arenaPlayer.CombatExp * bonusTime) - arenaPlayer.CombatExp) + ((arenaPlayer.ObjectiveExp * bonusTime) - arenaPlayer.ObjectiveExp) + pointsBonus);

                            GivePlayerExperience(arenaPlayer, bonusExp, ArenaPlayer.ExperienceType.Bonus);

                            Network.Send(arenaPlayer.WorldPlayer, GamePacket.Outgoing.Arena.UpdateExperience(arenaPlayer));

                            if (arenaPlayer.SecondsPlayed >= 300 && ArenaPlayers.Count >= 3)
                            {
                                arenaPlayer.ActiveCharacter.Statistics.Wins++;
                            }
                        }
                        else
                        {
                            if (ArenaPlayers.Count >= 3)
                            {
                                arenaPlayer.ActiveCharacter.Statistics.Losses++;
                            }
                        }

                        for (Int32 j = 0; j < ArenaPlayers.Count; j++)
                        {
                            Network.Send(arenaPlayer.WorldPlayer, GamePacket.Outgoing.Arena.PlayerState(ArenaPlayers[j]));
                        }
                    }

                    Thread.Sleep(100);
                }

                for (Int32 i = 0; i < ArenaPlayers.Count; i++)
                {
                    ArenaPlayer arenaPlayer = ArenaPlayers[i];
                    if (arenaPlayer == null) continue;

                    Network.Send(arenaPlayer.WorldPlayer, GamePacket.Outgoing.World.ArenaState(this, arenaPlayer.WorldPlayer));
                    Network.SendTo(arenaPlayer.WorldPlayer, GamePacket.Outgoing.World.PlayerLeave(arenaPlayer.WorldPlayer), Network.SendToType.Tavern, false);
                }

                Network.SendTo(GamePacket.Outgoing.World.ArenaDeleted(this), Network.SendToType.Tavern);

                CurrentState = State.CleanUp;
            }
        }

        public void ProcessMisc()
        {
            if (DebugFlags.HasFlag(ArenaSpecialFlag.ThinTracking) && ThinTrackingTick.HasElapsed)
            {
                for (Int32 i = 0; i < ArenaPlayers.Count; i++)
                {
                    ArenaPlayer sendArenaPlayer = ArenaPlayers[i];
                    if (sendArenaPlayer == null) continue;

                    for (Int32 x = 0; x < Grid.Thins.Count; x++)
                    {
                        Thin thin = Grid.Thins[x];
                        if (thin == null || thin.BoundingBox == null) continue;

                        GamePacket.Outgoing.System.DrawBoundingBox(sendArenaPlayer, thin.BoundingBox);
                    }
                }
            }
        }

        public void ProcessArenaPlayers(bool UDP = false)
        {
            Boolean doHealthRegen = HealthRegenTick.HasElapsed;

            for (Int32 i = 0; i < ArenaPlayers.Count; i++)
            {
                ArenaPlayer arenaPlayer = ArenaPlayers[i];
                if (arenaPlayer == null) continue;

                switch (arenaPlayer.CurrentGridBlockFlagData.BlockFlag)
                {
                    case GridBlockFlag.Valhalla:
                    {
                        arenaPlayer.ValhallaProtection.Reset();
                        break;
                    }
                    case GridBlockFlag.Shrine:
                    {
                        if (Ruleset.Rules.HasFlag(ArenaRuleset.ArenaRule.CaptureTheFlag))
                        {
                            DoCaptureTheFlag(arenaPlayer);
                        }
                        break;
                    }
                }

                if (doHealthRegen && !Ruleset.Rules.HasFlag(ArenaRuleset.ArenaRule.NoRegen) && arenaPlayer.IsAlive && arenaPlayer.CurrentHp < arenaPlayer.MaxHp && !arenaPlayer.IsInCombat)
                {
                    Single regenAmount = Ruleset.Rules.HasFlag(ArenaRuleset.ArenaRule.FastRegen) ? 0.03f : 0.01f;
                    arenaPlayer.CurrentHp += Convert.ToInt16(Math.Ceiling(arenaPlayer.MaxHp*regenAmount));
                    Network.Send(arenaPlayer.WorldPlayer, GamePacket.Outgoing.Arena.UpdateHealth(arenaPlayer, UDP));
                }

                for (Int32 j = 0; j < arenaPlayer.Effects.Length; j++)
                {
                    Effect arenaEffect = arenaPlayer.Effects[j];
                    if (arenaEffect == null) continue;

                    Boolean hasElapsed = arenaEffect.Duration.HasElapsed;

                    if (!arenaPlayer.IsAlive || (hasElapsed && !arenaEffect.Duration.CanReset))
                    {
                        arenaPlayer.Effects[j] = null;
                        continue;
                    }

                    switch (arenaEffect.EffectSpell.Effect)
                    {
                        case SpellEffectType.Bleed:
                        {
                            if (hasElapsed) DoPlayerDamage(arenaPlayer, arenaEffect.Owner, arenaEffect.EffectSpell, null, false);

                            break;
                        }
                    }
                }

                if (DebugFlags.HasFlag(ArenaSpecialFlag.PlayerTracking) && PlayerTrackingTick.HasElapsed)
                {
                    for (Int32 c = 0; c < ArenaPlayers.Count; c++)
                    {
                        ArenaPlayer sendArenaPlayer = ArenaPlayers[c];
                        if (sendArenaPlayer == null) continue;

                        for (Int32 x = 0; x < ArenaPlayers.Count; x++)
                        {
                            ArenaPlayer debugArenaPlayer = ArenaPlayers[x];
                            if (debugArenaPlayer == null || sendArenaPlayer == debugArenaPlayer) continue;

                            GamePacket.Outgoing.System.DrawBoundingBox(sendArenaPlayer, debugArenaPlayer.BoundingBox);
                        }
                    }
                }

                if (TriggerPulseTick.HasElapsed)
                {
                    for (Int32 x = 0; x < Grid.Triggers.Count; x++)
                    {
                        Network.Send(arenaPlayer.WorldPlayer, GamePacket.Outgoing.Arena.ActivatedTrigger(Grid.Triggers[x], UDP));
                    }
                }
            }
        }

        public void ProcessTriggers(bool UDP = false)
        {
            for (Int32 i = 0; i < Grid.Triggers.Count; i++)
            {
                Trigger trigger = Grid.Triggers[i];
                if (trigger == null) continue;

                if (trigger.Duration != null)
                {
                    if (trigger.CurrentState == TriggerState.Active && trigger.ResetTimer > 0 && trigger.Duration.HasElapsed)
                    {
                        trigger.Duration = null;
                        trigger.CurrentState = TriggerState.Inactive;
                        Network.SendTo(this, GamePacket.Outgoing.Arena.ActivatedTrigger(trigger, UDP), Network.SendToType.Arena);
                    }
                }

                if (trigger.TriggerType == TriggerType.Elevator)
                {
                    Single zSpeed = trigger.Speed * CurrentTickDelta;

                    if (trigger.CurrentState == TriggerState.Active)
                    {
                        trigger.Position.Z += zSpeed;
                        if (trigger.Position.Z > trigger.OnHeight)
                        {
                            trigger.Position.Z = trigger.OnHeight;
                        }
                    }
                    else
                    {
                        trigger.Position.Z -= zSpeed;
                        if (trigger.Position.Z < trigger.OffHeight)
                        {
                            trigger.Position.Z = trigger.OffHeight;
                        }
                    }
                }
            }
        }

        public void ProcessRunes(bool UDP = false)
        {
            Boolean doRuneTracking = RuneTrackingTick.HasElapsed;

            for (Int32 i = Runes.Count - 1; i >= 0; i--)
            {
                Rune rune = Runes[i];
                if (rune == null) continue;

                if (rune.Duration.HasElapsed)
                {
                    if (rune.IsCTFOrb && Ruleset.Rules.HasFlag(ArenaRuleset.ArenaRule.CaptureTheFlag))
                    {
                        ArenaTeams.FindByTeam(rune.Team).ShrineOrb.ResetOrb();
                        Network.SendTo(this, GamePacket.Outgoing.System.DirectTextMessage(null, String.Format("The {0} orb has been returned to its shrine.", rune.Team)), Network.SendToType.Arena);
                    }

                    Network.SendTo(this, GamePacket.Outgoing.Arena.ObjectDeath(rune.ObjectId, UDP), Network.SendToType.Arena);
                    Runes.RemoveAt(i);
                    continue;
                }

                if (doRuneTracking && DebugFlags.HasFlag(ArenaSpecialFlag.RuneTracking))
                {
                    if (rune.Owner != null)
                    {
                        GamePacket.Outgoing.System.DrawBoundingBox(rune.Owner, rune.BoundingBox);
                    }
                }

                if (rune.IsCTFOrb && Ruleset.Rules.HasFlag(ArenaRuleset.ArenaRule.CaptureTheFlag))
                {
                    for (Int32 p = 0; p < ArenaPlayers.Count; p++)
                    {
                        ArenaPlayer arenaPlayer = ArenaPlayers[p];
                        if (arenaPlayer == null) continue;

                        if (!arenaPlayer.IsDamageable || arenaPlayer.ActiveTeam == Team.Neutral || arenaPlayer.IsInValhalla) continue;

                        if (arenaPlayer.BoundingBox.Collides(rune.BoundingBox))
                        {
                            ArenaTeam arenaTeam = ArenaTeams.FindByTeam(rune.Team);

                            switch (arenaTeam.ShrineOrb.ChangeState(arenaPlayer))
                            {
                                case CTFOrbState.InHomeShrine:
                                {
                                    Network.SendToArena(arenaPlayer, GamePacket.Outgoing.System.DirectTextMessage(arenaPlayer.WorldPlayer, String.Format("The {0} orb has been returned to its shrine.", arenaTeam.Shrine.Team)), true);

                                    Network.SendTo(this, GamePacket.Outgoing.Arena.ObjectDeath(rune.ObjectId, UDP), Network.SendToType.Arena);
                                    Runes.Remove(rune);
                                    break;
                                }
                                case CTFOrbState.OnEnemyPlayer:
                                {
                                    Network.SendToArena(arenaPlayer, GamePacket.Outgoing.System.DirectTextMessage(arenaPlayer.WorldPlayer, String.Format("{0} has picked up the {1} orb!", arenaPlayer.ActiveCharacter.Name, arenaTeam.Shrine.Team)), false);
                                    Network.Send(arenaPlayer.WorldPlayer, GamePacket.Outgoing.System.DirectTextMessage(arenaPlayer.WorldPlayer, String.Format("You have picked up the {0} orb!", arenaTeam.Shrine.Team)));

                                    Network.SendTo(this, GamePacket.Outgoing.Arena.ObjectDeath(rune.ObjectId, UDP), Network.SendToType.Arena);
                                    Runes.Remove(rune);
                                    break;
                                }
                            }
                        }
                    }

                    continue;
                }

                if (rune.IsAura)
                {
                    if (rune.AuraHealth <= 0)
                    {
                        Network.SendTo(this, GamePacket.Outgoing.Arena.ObjectDeath(rune.ObjectId, UDP), Network.SendToType.Arena);
                        Runes.RemoveAt(i);
                        continue;
                    }

                    if (rune.AuraPulse.HasElapsed)
                    {
                        for (Int32 p = 0; p < ArenaPlayers.Count; p++)
                        {
                            ArenaPlayer arenaPlayer = ArenaPlayers[p];
                            if (arenaPlayer == null) continue;

                            BoundingSphere boxSphere = arenaPlayer.BoundingBox.ExtentSphere;

                            if (rune.AuraEffectSphere.Contains(ref boxSphere) == ContainmentType.Disjoint) continue;

                            switch (rune.Spell.Friendly)
                            {
                                case SpellFriendlyType.NonFriendly:
                                {
                                    if (rune.Team != arenaPlayer.ActiveTeam || (rune.Team == Team.Neutral && rune.Owner != arenaPlayer))
                                    {
                                        if (Grid.LineToBoxIsBlocked(rune.AuraBoundingSphere.Center, arenaPlayer.BoundingBox)) continue;

                                        DoPlayerEffect(arenaPlayer, rune.Owner, rune.Spell, EffectType.AuraTarget);
                                    }

                                    break;
                                }

                                case SpellFriendlyType.Friendly:
                                case SpellFriendlyType.FriendlyDead:
                                {
                                    if ((rune.Team == arenaPlayer.ActiveTeam || arenaPlayer.ActiveTeam == Team.Neutral || rune.Team == Team.Neutral) || (rune.Spell.NoTeam && rune.Owner == arenaPlayer))
                                    {
                                        if (Grid.LineToBoxIsBlocked(rune.AuraBoundingSphere.Center, arenaPlayer.BoundingBox)) continue;

                                        DoPlayerEffect(arenaPlayer, rune.Owner, rune.Spell, rune.Owner == arenaPlayer ? EffectType.AuraCaster : EffectType.AuraTarget);
                                    }
                                    else
                                    {
                                        if (!arenaPlayer.IsAlive || Grid.LineToBoxIsBlocked(rune.AuraBoundingSphere.Center, arenaPlayer.BoundingBox)) continue;

                                        Single dist = Vector3.Distance(arenaPlayer.BoundingBox.Origin, rune.BoundingBox.Origin);
                                        Single maxDist = rune.AuraBoundingSphere.Radius + (arenaPlayer.BoundingBox.ExtentSphere.Radius/2);
                                        Single fReduction = 1.0f - ((dist/maxDist)*1.0f);

                                        if (fReduction > 0)
                                        {
                                            rune.AuraHealth -= (Int16) Math.Ceiling(((rune.AuraPulse.Duration/1000)*6)*fReduction);
                                        }
                                    }
                                    break;
                                }
                            }

                            if (rune.AuraHealth <= 0)
                            {
                                Network.SendTo(this, GamePacket.Outgoing.Arena.ObjectDeath(rune.ObjectId, UDP), Network.SendToType.Arena);
                                Runes.RemoveAt(i);
                                break;
                            }
                        }
                    }
                }
                else
                {
                    for (Int32 p = 0; p < ArenaPlayers.Count; p++)
                    {
                        ArenaPlayer arenaPlayer = ArenaPlayers[p];
                        if (arenaPlayer == null) continue;

                        if (!arenaPlayer.IsDamageable || (rune.Team == arenaPlayer.ActiveTeam && arenaPlayer.ActiveTeam != Team.Neutral) || (!rune.Spell.NoTeam && rune.Owner == arenaPlayer) || !arenaPlayer.IsMoving || (arenaPlayer.IsInValhalla && rune.Owner != arenaPlayer)) continue;

                        if (arenaPlayer.BoundingBox.Collides(rune.BoundingBox))
                        {
                            if (rune.Team == Team.None && rune.Owner == arenaPlayer)
                            {
                                if (Vector3.Distance(rune.BoundingBox.Origin, rune.Owner.BoundingBox.Origin) >= rune.OwnerDistance) continue;
                            }

                            if (DoPlayerEffect(arenaPlayer, rune.Owner, rune.Spell, EffectType.Death))
                            {
                                DoPlayerDamage(arenaPlayer, rune.Owner, rune.Spell, null, true);

                                Program.ServerForm.MainLog.WriteMessage($"Rune {rune.ObjectId} triggered by player {arenaPlayer.ActiveCharacter.Name}", Color.Red);

                                Network.SendTo(this, GamePacket.Outgoing.Arena.ObjectDeath(arenaPlayer, rune.ObjectId), Network.SendToType.Arena);
                                Network.SendTo(this, GamePacket.Outgoing.Arena.ObjectDeath(arenaPlayer, rune.ObjectId), Network.SendToType.Arena);

                                Runes.Remove(rune);
                            }
                        }
                    }
                }
            }
        }

        public void ProcessBolts()
        {
            for (Int32 i = Bolts.Count - 1; i >= 0; i--)
            {
                Bolts[i].Distance -= Bolts[i].Velocity * CurrentTickDelta;
                if (Bolts[i].Distance > 0) continue;

                if (Bolts[i].Target != null && Bolts[i].Target.IsAlive)
                {
                    DoPlayerDamage(Bolts[i].Target, Bolts[i].Owner, Bolts[i].Spell, null, true);
                }

                Bolts.RemoveAt(i);
            }
        }

        public void ProcessProjectiles()
        {
            Boolean doProjectileTracking = ProjectileTrackingTick.HasElapsed;

            Single adjustedTickDelta = CurrentTickDelta + (CurrentTickDelta * 0.085f);

            for (Int32 i = ProjectileGroups.Count - 1; i >= 0; i--)
            {
                for (Int32 j = ProjectileGroups[i].Projectiles.Count - 1; j >= 0; j--)
                {
                    Boolean hasCollided = false;

                    Projectile projectile = ProjectileGroups[i].Projectiles[j];

                    Single cosZRadians = (Single) Math.Cos(MathHelper.DegreesToRadians(projectile.Angle));
                    Single velocityDelta = projectile.Spell.Velocity * adjustedTickDelta;

                    switch (projectile.Spell.Gravity)
                    {
                        case true:
                        {
                            Single gravityVelocityDelta = (projectile.Spell.Velocity - Math.Abs(projectile.OriginalAngle)) * adjustedTickDelta;

                            if (projectile.GravityStepDelta > 0)
                            {
                                projectile.Location.X += -(gravityVelocityDelta) * (Single)Math.Sin(projectile.Direction) * cosZRadians;
                                projectile.Location.Y += (gravityVelocityDelta) * (Single)Math.Cos(projectile.Direction) * cosZRadians;
                                projectile.Location.Z += (-256 * projectile.GravityStepDelta + projectile.Angle * (gravityVelocityDelta * 2.85f)) * adjustedTickDelta;
                            }
                            else
                            {
                                projectile.Location.X += -(gravityVelocityDelta) * (Single)Math.Sin(projectile.Direction) * cosZRadians;
                                projectile.Location.Y += gravityVelocityDelta * (Single)Math.Cos(projectile.Direction) * cosZRadians;

                                if (projectile.Angle > 0 || projectile.Angle < 0)
                                {
                                    projectile.Location.Z += (gravityVelocityDelta * (MathHelper.DegreesToRadians(projectile.Angle) * 0.95f));
                                }
                            }

                            projectile.GravityStepDelta += adjustedTickDelta;
                            projectile.GravityStepCount++;

                            break;
                        }
                        case false:
                        {
                            projectile.Location.X += -(velocityDelta) * (Single)Math.Sin(projectile.Direction) * cosZRadians;
                            projectile.Location.Y += velocityDelta * (Single)Math.Cos(projectile.Direction) * cosZRadians;

                            if (projectile.Angle > 0 || projectile.Angle < 0)
                            {
                                projectile.Location.Z += (projectile.Spell.Velocity * adjustedTickDelta * (MathHelper.DegreesToRadians(projectile.Angle) * 0.95f));
                            }

                            break;
                        }
                    }

                    if (projectile.Location.X < 0 || projectile.Location.X > 32767 || projectile.Location.Y < 0 || projectile.Location.Y > 32767 || projectile.Location.Z < -32767 || projectile.Location.Z > 32767)
                    {
                        ProjectileGroups[i].Projectiles.RemoveAt(j);
                        continue;
                    }

                    projectile.BoundingBox.Move(projectile.Location);

                    if (projectile.Spell.Gravity)
                    {
                        GridBlock block  = Grid.GridBlocks.GetHighestGravityBlock(projectile.BoundingBox);

                        if (block != null)
                        {
                            if (((projectile.Location.Z + projectile.Spell.MaxStep) >= (block.LowBoxTopZ - block.LowBoxTopMod) && projectile.Location.Z < block.MidBoxBottomZ))
                            {
                                if (projectile.Location.Z <= (block.LowBoxTopZ + 1))
                                {
                                    projectile.Location.Z = block.LowBoxTopZ + 1 ;
                                    projectile.GravityStepDelta = 0f;
                                    projectile.GravityStepCount = 0;
                                    projectile.Angle = 0f;
                                    projectile.BoundingBox.Move(projectile.Location);
                                }
                            }
                        }

                        GridBlockCollection tileGridBlockCollection = Grid.GridBlocks.GetBlocksNearBoundingBox(projectile.BoundingBox);

                        foreach (GridBlock gridBlock in tileGridBlockCollection)
                        {
                            if (gridBlock.LowBoxTile == null) continue;

                            foreach (TileBlock tileBlock in gridBlock.LowBoxTile.TileBlocks)
                            {
                                if (tileBlock.BottomHeight <= 0) continue;
                                if (!tileBlock.BottomBoundingBox.Collides(projectile.BoundingBox)) continue;
                                
                                if ((projectile.Location.Z + projectile.Spell.MaxStep) >= (tileBlock.BottomHeight + gridBlock.LowBoxTopZ))
                                {
                                    projectile.Location.Z = tileBlock.BottomHeight + gridBlock.LowBoxTopZ + 1f;
                                    projectile.GravityStepDelta = 0f;
                                    projectile.GravityStepCount = 0;
                                    projectile.Angle = 0f;
                                    projectile.BoundingBox.Move(projectile.Location);
                                    break;
                                }
                            }
                        }
                    }


                    if (!projectile.Spell.Ethereal)
                    {
                        for (Int32 w = Walls.Count - 1; w >= 0; w--)
                        {
                            if (!Walls[w].BoundingBox.Collides(projectile.BoundingBox)) continue;

                            if (projectile.Spell.EffectRadius > 0)
                            {
                                DoAreaDamage(null, projectile, Walls[w].BoundingBox);
                            }

                            if (ProjectileGroups[i].Projectiles[j].Spell.DamageByDistanceTraveled)
                            {
                                SpellDamage spellDamage = new SpellDamage(ProjectileGroups[i].Projectiles[j].Spell);

                                Single bonus = (((ProjectileGroups[i].Projectiles[j].DistanceTicks.Ticks * 2f) * 0.01f) + 1f);

                                if (DebugFlags.HasFlag(ArenaSpecialFlag.ProjectileTracking))
                                {
                                    Network.SendTo(this, GamePacket.Outgoing.System.DirectTextMessage(null, String.Format("[DBDT Wall Damage] Base: {0}, After: {1}, Bonus: {2}%, {3} Ticks", spellDamage.Damage, spellDamage.Damage * bonus, (bonus - 1) * 100, ProjectileGroups[i].Projectiles[j].DistanceTicks.Ticks)), Network.SendToType.Arena);
                                }

                                spellDamage.Damage = (Int16)(spellDamage.Damage * bonus);

                                DoWallDamage(projectile.Owner, Walls[w], projectile.Spell, spellDamage);
                            }
                            else
                            {
                                DoWallDamage(projectile.Owner, Walls[w], projectile.Spell, null);
                            }

                            if (projectile.Spell.Bounce > 0 && projectile.BouncesRemaining > 0)
                            {
                                // Compute reflection
                                // Get wall normal (perpendicular to direction)
                                float wallRad = Walls[w].Direction * (2f * (float)Math.PI / 65536f);

                                Vector3 wallNormal = new Vector3(
                                    (float)Math.Cos(wallRad + Math.PI / 2f),  // +90° for correct side
                                    (float)Math.Sin(wallRad + Math.PI / 2f),
                                    0f);

                                // Current velocity direction
                                Vector3 vel = new Vector3(
                                    (float)Math.Cos(projectile.Direction * (2f * Math.PI / 65536f)),
                                    (float)Math.Sin(projectile.Direction * (2f * Math.PI / 65536f)),
                                    0f);

                                // Reflect: v' = v - 2 * (v · n) * n
                                float dot = Vector3.Dot(vel, wallNormal);
                                Vector3 reflected = vel - 2f * dot * wallNormal;

                                // Convert back to direction (0-65535)
                                float newRad = (float)Math.Atan2(reflected.Y, reflected.X);

                                projectile.Direction = (ushort)((newRad * 65536f / (2f * Math.PI) + 65536f) % 65536f);

                                projectile.BouncesRemaining--;

                                // Small push away from wall to prevent stuck/re-collision
                                projectile.Location += reflected * 10f;

                                hasCollided = false;
                                break;
                            }

                            hasCollided = true;
                            break;
                        }
                    }

                    if (hasCollided)
                    {
                        if (DebugFlags.HasFlag(ArenaSpecialFlag.ProjectileTracking))
                        {
                            if (projectile.Owner != null)
                            {
                                GamePacket.Outgoing.System.DrawBoundingBox(projectile.Owner, projectile.BoundingBox);
                            }
                        }

                        ProjectileGroups[i].Projectiles.RemoveAt(j);
                        continue;
                    }


                    if (Grid.TileCollides(projectile.BoundingBox))
                    {
                        if (projectile.Spell.EffectRadius > 0)
                        {
                            GridBlock block = Grid.GridBlocks.GetBlockByLocation(projectile.BoundingBox.Origin.X, projectile.BoundingBox.Origin.Y);

                            if (block != null)
                            {
                                DoAreaDamage(null, projectile, block.ContainerBox);
                            }
                        }

                        ProjectileGroups[i].Projectiles.RemoveAt(j);
                        continue;
                    }

                    for (Int32 k = ArenaPlayers.Count - 1; k >= 0; k--)
                    {
                        ArenaPlayer arenaPlayer = ArenaPlayers[k];
                        if (arenaPlayer == null) continue;

                        if (arenaPlayer.WorldPlayer.Flags.HasFlag(PlayerFlag.Hidden) ||
                            (!arenaPlayer.IsAlive && projectile.Spell.Friendly != SpellFriendlyType.FriendlyDead) ||
                            ProjectileGroups[i].Owner == arenaPlayer ||
                            !arenaPlayer.BoundingBox.Collides(projectile.BoundingBox))
                            continue;

                        if ((arenaPlayer.ActiveTeam != ProjectileGroups[i].Team || Ruleset.Rules.HasFlag(ArenaRuleset.ArenaRule.FriendlyFire)) || arenaPlayer.ActiveTeam == Team.Neutral)
                        {
                            if (ProjectileGroups[i].Projectiles[j].Spell.DamageByDistanceTraveled)
                            {
                                SpellDamage spellDamage = new SpellDamage(ProjectileGroups[i].Projectiles[j].Spell);

                                Single bonus = (((ProjectileGroups[i].Projectiles[j].DistanceTicks.Ticks*2f)*0.01f) + 1f);

                                if (DebugFlags.HasFlag(ArenaSpecialFlag.ProjectileTracking))
                                {
                                    Network.SendTo(this, GamePacket.Outgoing.System.DirectTextMessage(null, String.Format("[DBDT Player Damage] Base: {0}, After: {1}, Bonus: {2}%, {3} Ticks", spellDamage.Damage, spellDamage.Damage * bonus, (bonus - 1) * 100, ProjectileGroups[i].Projectiles[j].DistanceTicks.Ticks)), Network.SendToType.Arena);
                                }

                                spellDamage.Damage = (Int16)(spellDamage.Damage * bonus);

								DoPlayerDamage(arenaPlayer, ProjectileGroups[i].Owner, ProjectileGroups[i].Projectiles[j].Spell, spellDamage, true);
                            }
                            else
                            {
                                if (Ruleset.Rules.HasFlag(ArenaRuleset.ArenaRule.FriendlyFire) && (ProjectileGroups[i].Owner.ActiveTeam == arenaPlayer.ActiveTeam && arenaPlayer.ActiveTeam != Team.Neutral))
                                {
                                    SpellDamage spellDamage = new SpellDamage(ProjectileGroups[i].Projectiles[j].Spell);

                                    spellDamage.Damage = (Int16)(spellDamage.Damage * 0.50f);
                                    spellDamage.Power = (Int16)(spellDamage.Power * 0.50f);

                                    DoPlayerDamage(arenaPlayer, ProjectileGroups[i].Owner, ProjectileGroups[i].Projectiles[j].Spell, spellDamage, true);
                                    DoPlayerDamage(ProjectileGroups[i].Owner, ProjectileGroups[i].Owner, ProjectileGroups[i].Projectiles[j].Spell, spellDamage, false);
                                }
                                else
                                {
									DoPlayerDamage(arenaPlayer, ProjectileGroups[i].Owner, ProjectileGroups[i].Projectiles[j].Spell, null, true);
                                }
                            }

                            DoPlayerEffect(arenaPlayer, ProjectileGroups[i].Owner, ProjectileGroups[i].Projectiles[j].Spell, EffectType.Death);
                        }

                        if (projectile.Spell.EffectRadius > 0)
                        {
                            DoAreaDamage(arenaPlayer, projectile, arenaPlayer.BoundingBox);
                        }

                        hasCollided = true;
                        break;
                    }

                    if (hasCollided)
                    {
                        if (DebugFlags.HasFlag(ArenaSpecialFlag.ProjectileTracking))
                        {
                            if (projectile.Owner != null)
                            {
                                GamePacket.Outgoing.System.DrawBoundingBox(projectile.Owner, projectile.BoundingBox);
                            }
                        }

                        ProjectileGroups[i].Projectiles.RemoveAt(j);
                        continue;
                    }

                    if (DebugFlags.HasFlag(ArenaSpecialFlag.ProjectileTracking) && doProjectileTracking)
                    {
                        if (projectile.Owner != null)
                        {
                            GamePacket.Outgoing.System.DrawBoundingBox(projectile.Owner, projectile.BoundingBox);
                        }
                    }

                    foreach (Thin thin in Grid.Thins)
                    {
                        if (thin.BoundingBox == null) continue;

                        if (thin.BoundingBox.Collides(projectile.BoundingBox))
                        {
                            if (thin.TriggerId > 0)
                            {
                                Trigger trigger = Grid.Triggers[thin.TriggerId];

                                if (trigger != null)
                                {
                                    if (!trigger.Enabled) continue;

                                    if (trigger.TriggerType == TriggerType.Door && trigger.CurrentState == TriggerState.Active) continue;
                                }
                            }
                            else
                            {
                                if (!thin.BlockProjectiles) continue;
                            }

                            if (DebugFlags.HasFlag(ArenaSpecialFlag.ProjectileTracking))
                            {
                                if (projectile.Owner != null)
                                {
                                    GamePacket.Outgoing.System.DrawBoundingBox(projectile.Owner, thin.BoundingBox);
                                }
                            }

                            if (projectile.Spell.EffectRadius > 0)
                            {
                                DoAreaDamage(null, projectile, thin.BoundingBox);
                            }

                            hasCollided = true;
                            break;
                        }
                    }

                    if (hasCollided)
                    {
                        if (DebugFlags.HasFlag(ArenaSpecialFlag.ProjectileTracking))
                        {
                            if (projectile.Owner != null)
                            {
                                GamePacket.Outgoing.System.DrawBoundingBox(projectile.Owner, projectile.BoundingBox);
                            }
                        }

                        ProjectileGroups[i].Projectiles.RemoveAt(j);
                        continue;
                    }

                    GridBlock impactBlock = Grid.GridBlocks.GetBlockByLocation(projectile.BoundingBox.Origin.X, projectile.BoundingBox.Origin.Y);

                    if (Grid.Collides(projectile.BoundingBox))
                    {
                        if (projectile.BouncesRemaining > 0)
                        {
                            Program.ServerForm.MainLog.WriteMessage($"[Grid.Collides] BouncesReamaing: {projectile.BouncesRemaining.ToString()}", Color.Red);

                            // Approximate normal from movement direction (simple bounce)
                            // Reverse direction (180° turn)
                            projectile.Direction = (ushort)((projectile.Direction + 32768 + CryptoRandom.GetInt32(-2000, 2000)) % 65536);

                            projectile.BouncesRemaining--;

                            // Small offset
                            Vector3 moveDir = new Vector3(
                                (float)Math.Cos(projectile.Direction * (2f * Math.PI / 65536f)),
                                (float)Math.Sin(projectile.Direction * (2f * Math.PI / 65536f)),
                                0f);
                            projectile.Location += moveDir * 10f;
                        }
                        else
                        {
                            hasCollided = true;
                        }
                    }

                    if (projectile.Duration.HasElapsed || hasCollided)
                    {
                        if (projectile.Spell.EffectRadius > 0)
                        {
                            if (impactBlock != null)
                            {
                                DoAreaDamage(null, projectile, impactBlock.ContainerBox);
                            }
                        }

                        if (DebugFlags.HasFlag(ArenaSpecialFlag.ProjectileTracking))
                        {
                            if (projectile.Owner != null)
                            {
                                GamePacket.Outgoing.System.DrawBoundingBox(projectile.Owner, projectile.BoundingBox);
                            }
                        }

                        ProjectileGroups[i].Projectiles.RemoveAt(j);
                    }
                }

                if (ProjectileGroups[i].Projectiles.Count == 0) ProjectileGroups.RemoveAt(i);
            }
        }

        public void ProcessWalls(bool UDP = false)
        {
            for (Int32 i = Walls.Count - 1; i >= 0; i--)
            {
                Wall wall = Walls[i];
                if (wall == null) continue;

                Boolean removeWall = wall.Duration.HasElapsed;

                if (!removeWall)
                {
                    if (wall.WeakenedDuration != null)
                    {
                        if (wall.WeakenedDuration.HasElapsed)
                        {
                            if (IsPlayerInWall(wall))
                            {
                                removeWall = true;
                            }
                            else
                            {
                                wall.WeakenedDuration = null;
                            }
                        }
                    }
                    else
                    {
                        if (wall.Spell.CollisionVelocity == 0 && !wall.Spell.CanDamage)
                        {
                            if (IsPlayerInWall(wall))
                            {
                                Int64 newDuration = (Int64) ((wall.Duration.Duration - wall.Duration.ElapsedMilliseconds)*0.25f);

                                if (newDuration <= 0)
                                {
                                    removeWall = true;
                                }
                                else
                                {
                                    wall.WeakenedDuration = new Interval(newDuration, false);
                                }
                            }
                        }
                    }
                }

                if (removeWall)
                {
                    Network.SendTo(this, GamePacket.Outgoing.Arena.ThinDamage(Walls[i].ObjectId, 1000, UDP), Network.SendToType.Arena);
                    Walls.RemoveAt(i);
                    continue;
                }

                if (wall.Spell.CanDamage)
                {
                    for (Int32 p = 0; p < ArenaPlayers.Count; p++)
                    {
                        ArenaPlayer arenaPlayer = ArenaPlayers[p];
                        if (arenaPlayer == null || !arenaPlayer.IsAlive) continue;

                        switch (wall.Spell.Friendly)
                        {
                            case SpellFriendlyType.NonFriendly:
                            {
                                if (arenaPlayer.IsMoving && arenaPlayer.NonFriendlyWallTime.HasElapsed && arenaPlayer.BoundingBox.Collides(wall.BoundingBox))
                                {
                                    DoPlayerDamage(arenaPlayer, wall.Owner, wall.Spell, null, false);
                                    arenaPlayer.NonFriendlyWallTime.Reset();
                                }

                                break;
                            }

                            case SpellFriendlyType.Friendly:
                            {
                                if (arenaPlayer.FriendlyWallTime.HasElapsed && arenaPlayer.BoundingBox.Collides(wall.BoundingBox))
                                {
                                    DoPlayerHealing(arenaPlayer, wall.Owner, wall.Spell);
                                    arenaPlayer.FriendlyWallTime.Reset();
                                }
                                break;
                            }
                        }
                    }
                }
            }
        }

        public Boolean IsPlayerInWall(Wall wall)
        {
            if (wall == null) return true;

            return ArenaPlayers.Where(arenaPlayer => arenaPlayer != null && arenaPlayer.IsAlive).Any(arenaPlayer => wall.BoundingBox.PointInBox(arenaPlayer.BoundingBox.Origin));
        }

        public void AdminKillPlayer(ArenaPlayer targetPlayer)
        {
            if (!targetPlayer.IsAlive || targetPlayer.WorldPlayer.Flags.HasFlag(PlayerFlag.Hidden)) return;

            lock (SyncRoot)
            {
                targetPlayer.CurrentHp = 0;

                Network.Send(targetPlayer.WorldPlayer, GamePacket.Outgoing.Arena.PlayerDamage(targetPlayer, null, new SpellDamage(null, targetPlayer.MaxHp, 0, 0)));

                if (!targetPlayer.IsAlive) PlayerDeath(targetPlayer, null);
            }
        }

        public void AdminRaisePlayer(ArenaPlayer targetPlayer, bool UDP = false)
        {
            if (targetPlayer.IsAlive) return;

            lock (SyncRoot)
            {
                targetPlayer.CurrentHp = targetPlayer.MaxHp;

                Network.SendTo(this, GamePacket.Outgoing.Arena.PlayerResurrect(targetPlayer, targetPlayer, UDP), Network.SendToType.Arena);
                Network.Send(targetPlayer.WorldPlayer, GamePacket.Outgoing.Arena.PlayerDamage(targetPlayer, null, new SpellDamage(null, 0, 0, 0), UDP));
            }
        }

        public void ArenaKickPlayer(ArenaPlayer targetPlayer, bool UDP = false)
        {
            if (targetPlayer.WorldPlayer.Flags.HasFlag(PlayerFlag.Hidden)) return;

            lock (SyncRoot)
            {
                Table table = TableManager.Tables.FindById(TableId);

                if (table != null)
                {
                    table.InvitedCharacterIds.Remove(targetPlayer.ActiveCharacter.CharacterId);
                }

                PlayerLeft(targetPlayer);
                Network.Send(targetPlayer.WorldPlayer, GamePacket.Outgoing.World.ArenaForceEndState(this, targetPlayer.WorldPlayer, UDP));
            }
        }

        public void DoCaptureTheFlag(ArenaPlayer arenaPlayer, bool UDP = false)
        {
            ArenaTeam shrineTeam = arenaPlayer.CurrentGridBlockFlagData.ShrineTeam;

            if (!arenaPlayer.IsAlive || shrineTeam == null) return;

            // Stepped in an Enemy Shrine
            if (shrineTeam.Shrine.Team != arenaPlayer.ActiveTeam)
            {
                switch (shrineTeam.ShrineOrb.OrbState)
                {
                    case CTFOrbState.InHomeShrine:
                    {
                        if (shrineTeam.Shrine.IsDead || shrineTeam.Shrine.IsIndestructible || ArenaTeams.IsPlayerCarryingOrb(arenaPlayer)) break;

                        if (shrineTeam.ShrineOrb.ChangeState(arenaPlayer) == CTFOrbState.OnEnemyPlayer)
                        {
                            Network.SendToArena(arenaPlayer, GamePacket.Outgoing.System.DirectTextMessage(arenaPlayer.WorldPlayer, String.Format("{0} has picked up the {1} orb!", arenaPlayer.ActiveCharacter.Name, shrineTeam.Shrine.Team)), false);
                            Network.Send(arenaPlayer.WorldPlayer, GamePacket.Outgoing.System.DirectTextMessage(arenaPlayer.WorldPlayer, String.Format("You have picked up the {0} orb!", shrineTeam.Shrine.Team)));
                        }
                        break;
                    }
                }
            }
            // Stepped in Your Shrine
            else
            {
                switch (shrineTeam.ShrineOrb.OrbState)
                {
                    case CTFOrbState.InHomeShrine:
                    {
                        ArenaTeam captureTeam = ArenaTeams.GetCarriedOrbTeam(arenaPlayer);

                        if (captureTeam == null || shrineTeam.Shrine.IsDead || shrineTeam.Shrine.IsIndestructible) break;

                        if (captureTeam.ShrineOrb.ChangeState(arenaPlayer) == CTFOrbState.InHomeShrine)
                        {
                            Int32 biasAmount = 20;

                            captureTeam.Shrine.CurrentBias -= 20;

                            if (captureTeam.Shrine.IsDead)
                            {
                                biasAmount = -captureTeam.Shrine.MaxBias & 0xFF;
                            }
                            else
                            {
                                biasAmount = -biasAmount & 0xFF;
                            }

                            Single experience = (arenaPlayer.ActiveCharacter.Level * 0.013f) * (ArenaPlayers.GetTeamPlayerCount(captureTeam.Shrine.Team) * 50);
                            GivePlayerExperience(arenaPlayer, (arenaPlayer.ActiveCharacter.Class == Character.PlayerClass.Runemage ? experience * 2 : experience), ArenaPlayer.ExperienceType.Objective);

                            Network.SendToArena(arenaPlayer, GamePacket.Outgoing.System.DirectTextMessage(arenaPlayer.WorldPlayer, String.Format("{0} has captured the {1} orb!", arenaPlayer.ActiveCharacter.Name, captureTeam.Shrine.Team)), false);
                            Network.Send(arenaPlayer.WorldPlayer, GamePacket.Outgoing.System.DirectTextMessage(arenaPlayer.WorldPlayer, String.Format("You have captured the {0} orb!", captureTeam.Shrine.Team)));

                            Network.SendToArena(arenaPlayer, GamePacket.Outgoing.Arena.BiasedShrine(arenaPlayer, captureTeam.Shrine, (Byte)biasAmount, UDP), true);
                        }
                        break;
                    }
                }
            }
        }

        public Boolean DoPlayerEffect(ArenaPlayer targetPlayer, ArenaPlayer sourcePlayer, Spell spell, EffectType effectType, bool UDP = false)
        {
            Effect arenaEffect = new Effect(spell, sourcePlayer, effectType);

            if (arenaEffect.EffectSpell != null)
            {
                SpellEffectType arenaEffectType = arenaEffect.EffectSpell.Effect;

                switch (arenaEffectType)
                {
                    case SpellEffectType.None:
                    {
                        return true;
                    }
                    case SpellEffectType.Resurrect:
                    {
                        if (targetPlayer.IsAlive || targetPlayer.IsInValhalla) return false;

                        if (Ruleset.Rules.HasFlag(ArenaRuleset.ArenaRule.NoRaiseCall)) return false;

                        DoPlayerResurrect(targetPlayer, sourcePlayer, arenaEffect.EffectSpell.Level);
                        break;
                    }
                    case SpellEffectType.Bless:
                    case SpellEffectType.Resist:
                    case SpellEffectType.Prayer:
                    case SpellEffectType.Speed:
                    case SpellEffectType.TargetResist:
                    case SpellEffectType.Leaping:
                    case SpellEffectType.Levitate:
                    case SpellEffectType.Fly:
                    case SpellEffectType.Expulse:
                    {
                        if (!targetPlayer.IsAlive) return false;

                        if (sourcePlayer == targetPlayer && effectType != EffectType.Area && effectType != EffectType.AuraCaster)
                        {
                            targetPlayer.Effects[(Int32) arenaEffectType] = arenaEffect;
                        }
                        else
                        {
                            if (sourcePlayer != targetPlayer)
                            {
                                if (Ruleset.Rules.HasFlag(ArenaRuleset.ArenaRule.NoFriendlyOther)) return false;
                            }

                            Effect currentEffect = targetPlayer.Effects[(Int32) arenaEffectType];

                            if (currentEffect != null)
                            {
                                if (currentEffect.EffectSpell.Level > arenaEffect.EffectSpell.Level) return false;
                            }

                            targetPlayer.Effects[(Int32) arenaEffectType] = arenaEffect;
                        }
                        break;
                    }
                    case SpellEffectType.Presence:
                    case SpellEffectType.Light:
                    case SpellEffectType.Bleed:
                    case SpellEffectType.HealingReduction:
                    {
                        if (!targetPlayer.IsAlive || targetPlayer.IsInValhalla) return false;

                        targetPlayer.IsInCombat = true;
                        targetPlayer.Effects[(Int32) arenaEffectType] = arenaEffect;
                        break;
                    }
                    case SpellEffectType.Hinder:
                    {
                        if (!targetPlayer.IsAlive || targetPlayer.IsInValhalla) return false;
                        
                        if (Ruleset.Rules.HasFlag(ArenaRuleset.ArenaRule.NoHinder)) return false;

                        targetPlayer.IsInCombat = true;
                        targetPlayer.Effects[(Int32) arenaEffectType] = arenaEffect;
                        break;
                    }
                    case SpellEffectType.Healing:
                    {
                        if (!targetPlayer.IsAlive || targetPlayer.IsInValhalla) return false;

                        if (sourcePlayer != targetPlayer && Ruleset.Rules.HasFlag(ArenaRuleset.ArenaRule.NoFriendlyOther)) return false;

                        if (!DoPlayerHealing(targetPlayer, sourcePlayer, arenaEffect.EffectSpell))
                        {
                            switch (spell.Type)
                            {
                                case SpellType.Effect:
                                //case SpellType.Target:
                                {
                                    break;
                                }
                                default:
                                {
                                    return false;
                                }
                            }
                        }

                        break;
                    }
                }

                if (effectType == EffectType.Area || effectType == EffectType.AuraCaster || effectType == EffectType.AuraTarget)
                {
                    Network.SendToArena(targetPlayer, GamePacket.Outgoing.Arena.CastEffect(targetPlayer, arenaEffect.EffectSpell.Id, UDP), true);
                    Network.SendToArena(targetPlayer, GamePacket.Outgoing.Arena.CastTargetedEx(targetPlayer, sourcePlayer, arenaEffect.OwnerSpell, UDP), true);
                    return true;
                }

                if (spell.Type == SpellType.Rune)
                {
                    Network.SendToArena(targetPlayer, GamePacket.Outgoing.Arena.CastEffect(targetPlayer, arenaEffect.EffectSpell.Id, UDP), true);
                    return true;
                }
            }

            return true;
        }

        public void DoAreaDamage(ArenaPlayer ignorePlayer, Projectile projectile, OrientedBoundingBox impactBox)
        {
            lock (SyncRoot)
            {
                Vector3 impactVector = impactBox != null ? impactBox.LineImpactVector(projectile.OriginalOrigin, projectile.BoundingBox.Origin) : projectile.BoundingBox.Origin;

                BoundingSphere areaEffectSphere = new BoundingSphere(impactVector, projectile.Spell.EffectRadius);

                for (Int32 p = 0; p < ArenaPlayers.Count; p++)
                {
                    ArenaPlayer arenaPlayer = ArenaPlayers[p];
                    if (arenaPlayer == null) continue;

                    if ((ignorePlayer == arenaPlayer && projectile.Spell.AreaEffectSpell == 0) || arenaPlayer.WorldPlayer.Flags.HasFlag(PlayerFlag.Hidden) || arenaPlayer.SpecialFlags.HasFlag(ArenaPlayer.SpecialFlag.God)) continue;
                    
                    BoundingSphere boxSphere = arenaPlayer.BoundingBox.ExtentSphere;

                    if (areaEffectSphere.Contains(ref boxSphere) == ContainmentType.Disjoint) continue;

                    Boolean hasCollided = true;

                    for (Int32 i = 0; i < Walls.Count; i++)
                    {
                        Wall wall = Walls[i];
                        if (wall == null) continue;

                        boxSphere = wall.BoundingBox.ExtentSphere;

                        if (areaEffectSphere.Contains(ref boxSphere) == ContainmentType.Disjoint) continue;
                        if (!arenaPlayer.BoundingBox.IsBoxVisibleToPoint(areaEffectSphere.Center, wall.BoundingBox)) continue;

                        switch (projectile.Spell.Friendly)
                        {
                            case SpellFriendlyType.Friendly:
                            case SpellFriendlyType.FriendlyDead:
                            {
                                if (wall.Spell.CollisionVelocity > 0) continue;
                                break;
                            }
                        }

                        hasCollided = false;
                        break;
                    }

                    foreach (Thin thin in Grid.Thins)
                    {
                        if (thin.BoundingBox == null) continue;

                        if (thin.BoundingBox.Collides(projectile.BoundingBox))
                        {
                            if (thin.TriggerId > 0)
                            {
                                Trigger trigger = Grid.Triggers[thin.TriggerId];

                                if (trigger != null)
                                {
                                    if (!trigger.Enabled) continue;

                                    if (trigger.TriggerType == TriggerType.Door && trigger.CurrentState == TriggerState.Active) continue;
                                }
                            }
                            else
                            {
                                if (!thin.BlockProjectiles) continue;
                            }

                            hasCollided = false;
                            break;
                        }
                    }

                    // ToDo -> Add a check here for tiles so that area damage doesnt hit through them.
                    if (!hasCollided || Grid.LineToBoxIsBlocked(areaEffectSphere.Center, arenaPlayer.BoundingBox)) continue;
                    
                    if (projectile.Owner == null) continue;

                    switch (projectile.Spell.Friendly)
                    {
                        case SpellFriendlyType.NonFriendly:
                        {
                            if ((projectile.Owner.ActiveTeam != arenaPlayer.ActiveTeam || (Ruleset.Rules.HasFlag(ArenaRuleset.ArenaRule.FriendlyFire) && arenaPlayer != projectile.Owner)) || (arenaPlayer.ActiveTeam == Team.Neutral && arenaPlayer != projectile.Owner))
                            {
                                SpellDamage spellDamage = new SpellDamage(projectile.Spell);

                                Single dist = arenaPlayer.BoundingBox.DistanceFromPointToClosestCorner(areaEffectSphere.Center);
                                Single maxDist = areaEffectSphere.Radius + (arenaPlayer.BoundingBox.ExtentSphere.Radius / 2);

                                Single fReduction = 0.6f - ((dist / maxDist) * 0.6f);

                                if (fReduction > 0.0f)
                                {
                                    spellDamage.Damage = (Int16)(spellDamage.Damage * fReduction);
                                    spellDamage.Healing = (Int16)(spellDamage.Healing * fReduction);
                                    spellDamage.Power = (Int16)(spellDamage.Power * fReduction);

                                    DoPlayerEffect(arenaPlayer, projectile.Owner, projectile.Spell, EffectType.Area);
                                    
                                    if (Ruleset.Rules.HasFlag(ArenaRuleset.ArenaRule.FriendlyFire) && projectile.Owner.ActiveTeam == arenaPlayer.ActiveTeam)
                                    {
                                        DoPlayerDamage(arenaPlayer, projectile.Owner, projectile.Spell, spellDamage, true);

                                        spellDamage.Damage = (Int16)(spellDamage.Damage * 0.30f);
                                        spellDamage.Power = (Int16)(spellDamage.Power * 0.30f);

                                        DoPlayerDamage(projectile.Owner, projectile.Owner, projectile.Spell, spellDamage, false);
                                    }
                                    else
                                    {
                                        DoPlayerDamage(arenaPlayer, projectile.Owner, projectile.Spell, spellDamage, true);
                                    }
                                }
                            }

                            break;
                        }
                        case SpellFriendlyType.Friendly:
                        {
                            if (projectile.Owner.ActiveTeam == arenaPlayer.ActiveTeam || arenaPlayer.ActiveTeam == Team.Neutral || projectile.Owner.ActiveTeam == Team.Neutral)
                            {
                                DoPlayerEffect(arenaPlayer, projectile.Owner, projectile.Spell, EffectType.Area);
                            }

                            break;
                        }
                        case SpellFriendlyType.FriendlyDead:
                        {
                            if (!arenaPlayer.IsAlive && (projectile.Owner.ActiveTeam == arenaPlayer.ActiveTeam || arenaPlayer.ActiveTeam == Team.Neutral || projectile.Owner.ActiveTeam == Team.Neutral))
                            {
                                DoPlayerEffect(arenaPlayer, projectile.Owner, projectile.Spell, EffectType.Area);
                            }

                            break;

                        }
                    }
                }
            }
        }

        public Boolean DoPlayerHealing(ArenaPlayer targetPlayer, ArenaPlayer sourcePlayer, Spell spell, bool UDP = false)
        {
            if (!targetPlayer.IsAlive || spell == null) return false;

            SpellDamage spellDamage = new SpellDamage(spell);
            Int16 hDifference = Convert.ToInt16(targetPlayer.MaxHp - targetPlayer.CurrentHp);

            if (spellDamage.Healing <= 0 || hDifference <= 0) return false;

            Int16 healingDone = spellDamage.Healing > hDifference ? hDifference : spellDamage.Healing;

            Effect arenaEffect = targetPlayer.Effects[(Int32) SpellEffectType.HealingReduction];

            if (arenaEffect != null && spellDamage.Healing < 255)
            {
                healingDone = (Int16)(healingDone - (healingDone * (arenaEffect.EffectSpell.Level / 100f)));
            }

            targetPlayer.CurrentHp += healingDone;
            targetPlayer.ActiveCharacter.Statistics.HealingTaken += healingDone;

            targetPlayer.IsInCombat = true;

            Network.Send(targetPlayer.WorldPlayer, GamePacket.Outgoing.Arena.PlayerDamage(targetPlayer, sourcePlayer, spellDamage, UDP));

            if (sourcePlayer != null && targetPlayer != sourcePlayer && targetPlayer.ActiveTeam == sourcePlayer.ActiveTeam)
            {
                sourcePlayer.IsInCombat = true;

                GivePlayerExperience(sourcePlayer, (Single)(Math.Ceiling(healingDone * 2.4f)), ArenaPlayer.ExperienceType.Combat);
                sourcePlayer.ActiveCharacter.Statistics.HealingDone += healingDone;
            }

            if (!targetPlayer.IsAlive) PlayerDeath(targetPlayer, sourcePlayer);

            return true;
        }

        public void DoPlayerResurrect(ArenaPlayer targetPlayer, ArenaPlayer sourcePlayer, Int16 hpPercent, bool UDP = false)
        {
            if (hpPercent <= 0) return;

            Int16 healAmount = Convert.ToInt16(Math.Floor(targetPlayer.MaxHp*(hpPercent*0.01f)));
            targetPlayer.CurrentHp = healAmount;

            Single experience = 25 + healAmount + (targetPlayer.ActiveCharacter.Level*5.0f) + ((targetPlayer.ActiveCharacter.Level - targetPlayer.ActiveCharacter.Level)*4.0f);

            GivePlayerExperience(sourcePlayer, experience, ArenaPlayer.ExperienceType.Combat);

            sourcePlayer.RaiseCount++;
            sourcePlayer.ActiveCharacter.Statistics.Raises++;

            targetPlayer.IsInCombat = false;

            Network.SendTo(this, GamePacket.Outgoing.Arena.PlayerResurrect(sourcePlayer, targetPlayer, UDP), Network.SendToType.Arena);
            Network.Send(targetPlayer.WorldPlayer, GamePacket.Outgoing.Arena.PlayerDamage(targetPlayer, null, new SpellDamage(null, 0, 0, 0), UDP));
        }

        public void DoPlayerDamage(ArenaPlayer targetPlayer, ArenaPlayer sourcePlayer, Spell spell, SpellDamage spellDamage, Boolean showHitToSource, bool UDP = false)
        {
            if (!targetPlayer.IsDamageable || targetPlayer.IsInValhalla || spell == null) return;

            if (spellDamage == null) spellDamage = new SpellDamage(spell);

            if ((spellDamage.Healing <= 0 && spellDamage.Damage <= 0) && spellDamage.Power <= 0) return;

            Int16 resistedAmount = 0;

            for (Int32 j = 0; j < targetPlayer.Effects.Length; j++)
            {
                Effect arenaEffect = targetPlayer.Effects[j];
                if (arenaEffect == null) continue;

                switch (arenaEffect.EffectSpell.Effect)
                {
                    case SpellEffectType.Bless:
                    case SpellEffectType.Prayer:
                    case SpellEffectType.Resist:
                    {
                        Single dReduction = 0;

                        switch (spell.Element)
                        {
                            case SpellElementType.Void:
                            case SpellElementType.Arcane:
                            {
                                if (arenaEffect.EffectSpell.Element == SpellElementType.None)
                                {
                                    dReduction = (arenaEffect.EffectSpell.Level*0.01f)*spellDamage.Damage;
                                }
                                else
                                {
                                    dReduction = ((arenaEffect.EffectSpell.Level*0.5f)*0.01f)*spellDamage.Damage;
                                }
                                break;
                            }
                            case SpellElementType.Nature:
                            {
                                break;
                            }
                            default:
                            {
                                if ((arenaEffect.EffectSpell.Element == spell.Element || arenaEffect.EffectSpell.Element == SpellElementType.None) && spell.Element != SpellElementType.None)
                                {
                                    dReduction = (arenaEffect.EffectSpell.Level*0.01f)*spellDamage.Damage;
                                }
                                break;
                            }
                        }

                        resistedAmount += Convert.ToInt16(Math.Ceiling(dReduction));

                        if (resistedAmount >= spellDamage.Damage) resistedAmount = 0;

                        break;
                    }
                }
            }

            if (DebugFlags.HasFlag(ArenaSpecialFlag.ProjectileTracking))
            {
                Network.SendTo(this, GamePacket.Outgoing.System.DirectTextMessage(null, String.Format("[Resist Tracker] Target: {0}, Spell: {1}, Before: {2}, After: {3}, Resisted: {4}", targetPlayer.ActiveCharacter.Name, spell.Name, spellDamage.Damage, spellDamage.Damage - resistedAmount, resistedAmount)), Network.SendToType.Arena);
            }

            spellDamage.Damage -= resistedAmount;

            if (targetPlayer.ActiveCharacter.Level < AveragePlayerLevel)
            {
                Single reduction = ((AveragePlayerLevel - targetPlayer.ActiveCharacter.Level) + 1) / 38f;

                if (DebugFlags.HasFlag(ArenaSpecialFlag.ProjectileTracking))
                {
                    Network.SendTo(this, GamePacket.Outgoing.System.DirectTextMessage(null, String.Format("[Low Level Tracker] Target: {0}, Spell: {1}, Before: {2}, After: {3}, Resisted: {4}%", targetPlayer.ActiveCharacter.Name, spell.Name, spellDamage.Damage, (Int16)(spellDamage.Damage * (1 - reduction)), reduction * 100)), Network.SendToType.Arena);
                }

                spellDamage.Damage = (Int16)(spellDamage.Damage * (1 - reduction));
                spellDamage.Power = (Int16)(spellDamage.Power * (1 - reduction));
            }

            if (DebugFlags.HasFlag(ArenaSpecialFlag.OneDamageToPlayers))
            {
                spellDamage.Damage = 1;
            }

			targetPlayer.CurrentHp += spellDamage.Healing;
			targetPlayer.CurrentHp -= spellDamage.Damage;

            targetPlayer.ActiveCharacter.Statistics.DamageTaken += spellDamage.Damage;
            targetPlayer.ActiveCharacter.Statistics.HealingTaken += spellDamage.Healing;

            targetPlayer.IsInCombat = true;
            targetPlayer.LastAttacker = sourcePlayer;

            Network.Send(targetPlayer.WorldPlayer, GamePacket.Outgoing.Arena.PlayerDamage(targetPlayer, sourcePlayer, spellDamage, UDP));

            if (sourcePlayer != null)
            {
                sourcePlayer.IsInCombat = true;

                if ((targetPlayer.ActiveTeam != sourcePlayer.ActiveTeam || targetPlayer.ActiveTeam == Team.Neutral) && targetPlayer != sourcePlayer)
                {
                    sourcePlayer.ActiveCharacter.Statistics.DamageDone += spellDamage.Damage;
                    sourcePlayer.ActiveCharacter.Statistics.HealingDone += spellDamage.Healing;

                    Single experience = (Single) (spellDamage.Damage + Math.Ceiling(spellDamage.Power*0.75));

                    GivePlayerExperience(sourcePlayer, experience*1.80f, ArenaPlayer.ExperienceType.Combat);
                    GivePlayerExperience(targetPlayer, experience*0.70f, ArenaPlayer.ExperienceType.Combat);
                }

                if (showHitToSource)
                {
                    Network.Send(sourcePlayer.WorldPlayer, GamePacket.Outgoing.Arena.PlayerHit(targetPlayer, UDP));
                }
            }

            if (!targetPlayer.IsAlive) PlayerDeath(targetPlayer, sourcePlayer);
        }

        public void GivePlayerExperience(ArenaPlayer arenaPlayer, Single baseAmount, ArenaPlayer.ExperienceType experienceType)
        {
            Single plusBonus = arenaPlayer.WorldPlayer.Flags.HasFlag(PlayerFlag.MagestormPlus) ? Settings.Default.PlusExpBonus : 0.0f;

            switch (experienceType)
            {
                case ArenaPlayer.ExperienceType.Combat:
                {
					arenaPlayer.CombatExp += (Int32)(baseAmount * (Settings.Default.ExpMultiplier + Grid.ExpBonus + plusBonus));
                    break;
                }
                case ArenaPlayer.ExperienceType.Objective:
                {
					arenaPlayer.ObjectiveExp += (Int32)(baseAmount * (Settings.Default.ExpMultiplier + Grid.ExpBonus + plusBonus));
                    break;
                }
                case ArenaPlayer.ExperienceType.Bonus:
                {
                    arenaPlayer.BonusExp += (Int32)(baseAmount * (1.0f + plusBonus));
                    break;
                }
            }
        }

        public void DoWallDamage(ArenaPlayer arenaPlayer, Wall wall, Spell spell, SpellDamage spellDamage, bool UDP = false)
        {
            if (wall == null)
            {
                return;
            }
            if (spellDamage == null) spellDamage = new SpellDamage(spell);

            switch (spellDamage.Spell.Type)
            {
                case SpellType.Projectile:
                    {
                        if ((spell.Element == SpellElementType.Earth && wall.Spell.Element == SpellElementType.Earth) ||
                            (spell.Element == SpellElementType.Fire && wall.Spell.Element == SpellElementType.Cold) ||
                            (spell.Element == SpellElementType.Fire && wall.Spell.Element == SpellElementType.Nature) ||
                            (spell.Element == SpellElementType.Cold && wall.Spell.Element == SpellElementType.Air))
                        {
                            // Do Nothing
                        }
                        else if (spell.Element == SpellElementType.Void && wall.Spell.Element != SpellElementType.Void)
                        {
                            spellDamage.Power = Convert.ToInt16(Math.Ceiling(spellDamage.Power * 2f));
                        }
                        else
                        {
                            if (spell.Element == wall.Spell.Element)
                            {
                                spellDamage.Damage = 0;
                                spellDamage.Power = 0;
                            }
                            else
                            {
                                if (spell.Element != SpellElementType.Void)
                                {
                                    spellDamage.Damage = Convert.ToInt16(Math.Ceiling(spellDamage.Damage * 0.60f));
                                }
                            }
                        }

                        break;
                    }
                case SpellType.Dispel:
                    {
                        spellDamage.Damage = 1000;
                        break;
                    }

            }

            if (spellDamage.Damage <= 0 && spellDamage.Power <= 0)
            {
                return;
            }

            wall.CurrentHp -= (Int16)(spellDamage.Damage + spellDamage.Power);

            if (wall.CurrentHp <= 0)
            {
                Network.SendTo(this, GamePacket.Outgoing.Arena.ThinDamage(wall.ObjectId, 1000, UDP), Network.SendToType.Arena);
                Network.SendTo(this, GamePacket.Outgoing.Arena.ObjectDeath(arenaPlayer, wall.ObjectId), Network.SendToType.Arena);
                Walls.Remove(wall);
            }
        }

        public void DoWallDamage(ArenaPlayer arenaPlayer, Wall wall, Int16 damage, bool UDP = false)
        {
            if (wall == null || damage <= 0) return;

            wall.CurrentHp -= damage;

            if (wall.CurrentHp <= 0)
            {
                Network.SendTo(this, GamePacket.Outgoing.Arena.ThinDamage(wall.ObjectId, 1000, UDP), Network.SendToType.Arena);
                Network.SendTo(this, GamePacket.Outgoing.Arena.ObjectDeath(arenaPlayer, wall.ObjectId), Network.SendToType.Arena);
                Walls.Remove(wall);
            }
        }

        public void PlayerYank(Player player, ArenaPlayer arenaPlayer, Byte playerId, SharpDX.Vector3 location, bool UDP = false)
        {
            if (player.Flags.HasFlag(PlayerFlag.Hidden))
            {
                Network.Send(player, GamePacket.Outgoing.System.DirectTextMessage(player, "[System] You cannot yank players while hidden."));
                return;
            }

            lock (SyncRoot)
            {
                Network.Send(arenaPlayer.WorldPlayer, GamePacket.Outgoing.Arena.PlayerYank(arenaPlayer, playerId, location, UDP));
            }
        }

        public void PlayerLeft(ArenaPlayer arenaPlayer, bool UDP = false)
        {
            lock (SyncRoot)
            {
                Network.SendToArena(arenaPlayer, GamePacket.Outgoing.Arena.PlayerLeave(arenaPlayer, UDP), false);

                for (Int32 i = 0; i < Runes.Count; i++)
                {
                    if (Runes[i].Owner == arenaPlayer) Runes[i].Owner = null;
                }

                for (Int32 i = 0; i < Walls.Count; i++)
                {
                    if (Walls[i].Owner == arenaPlayer) Walls[i].Owner = null;
                }

                for (Int32 i = 0; i < Bolts.Count; i++)
                {
                    if (Bolts[i].Owner == arenaPlayer) Bolts[i].Owner = null;
                }

                for (Int32 i = 0; i < ProjectileGroups.Count; i++)
                {
                    if (ProjectileGroups[i].Owner == arenaPlayer) ProjectileGroups[i].Owner = null;

                    for (Int32 j = 0; j < ProjectileGroups[i].Projectiles.Count; j++)
                    {
                        if (ProjectileGroups[i].Projectiles[j].Owner == arenaPlayer) ProjectileGroups[i].Projectiles[j].Owner = null;
                    }
                }

                if (Ruleset.Rules.HasFlag(ArenaRuleset.ArenaRule.CaptureTheFlag))
                {
                    ArenaTeam orbTeam = ArenaTeams.GetCarriedOrbTeam(arenaPlayer);

                    if (orbTeam != null)
                    {
                        orbTeam.ShrineOrb.ResetOrb();

                        Network.SendToArena(arenaPlayer, GamePacket.Outgoing.System.DirectTextMessage(arenaPlayer.WorldPlayer, String.Format("The {0} orb has been returned to the its shrine.", orbTeam.Shrine.Team)), false);
                    }
                }

                if (CurrentState != State.Ended && CurrentState != State.CleanUp)
                {
                    Boolean hasGivenKillExp = false;

                    for (Int32 i = 0; i < ArenaPlayers.Count; i++)
                    {
                        if (arenaPlayer.LastAttacker == ArenaPlayers[i])
                        {
                            if (!hasGivenKillExp && arenaPlayer.IsAlive && arenaPlayer.IsInCombat)
                            {
                                arenaPlayer.CurrentHp = 0;
                                PlayerDeath(arenaPlayer, ArenaPlayers[i]);
                                hasGivenKillExp = true;
                            }

                            ArenaPlayers[i].LastAttacker = null;
                        }
                    }

                    if (!arenaPlayer.IsAlive)
                    {
                        arenaPlayer.ExpPenalty = arenaPlayer.ActiveCharacter.Level*(TeamHasHealer(arenaPlayer.ActiveTeam) ? 13 : 8);
                    }
                }

                Character.Save(arenaPlayer.WorldPlayer, null);

                arenaPlayer.WorldPlayer.ActiveArena = null;
                arenaPlayer.WorldPlayer.ActiveArenaPlayer = null;

                ArenaPlayers.Remove(arenaPlayer);

                AveragePlayerLevel = ArenaPlayers.GetAveragePlayerLevel();
            }
        }

        public void PlayerDeath(ArenaPlayer arenaPlayer, ArenaPlayer targetArenaPlayer, bool UDP = false)
        {
            lock (SyncRoot)
            {
                if (arenaPlayer == null || arenaPlayer.IsAlive) return;

                arenaPlayer.LastAttacker = null;
                arenaPlayer.DeathCount++;

                for (Int32 j = 0; j < arenaPlayer.Effects.Length; j++)
                {
                    Effect arenaEffect = arenaPlayer.Effects[j];
                    if (arenaEffect == null) continue;

                    arenaPlayer.Effects[j] = null;
                }


                foreach (Rune arenaRune in Runes.Where(arenaRune => arenaRune.IsAura && arenaRune.Owner == arenaPlayer))
                {
                    Network.SendToArena(arenaPlayer, GamePacket.Outgoing.Arena.ObjectDeath(arenaRune.ObjectId, UDP), true);
                    Runes.Remove(arenaRune);
                    break;
                }

                if (Ruleset.Rules.HasFlag(ArenaRuleset.ArenaRule.CaptureTheFlag))
                {
                    ArenaTeam orbTeam = ArenaTeams.GetCarriedOrbTeam(arenaPlayer);

                    if (orbTeam != null)
                    {
                        Rune rune = new Rune(orbTeam.ShrineOrb.ObjectId, arenaPlayer, SpellManager.CTFOrbSpell, arenaPlayer.BoundingBox.Origin, arenaPlayer.Direction, new Byte[20])
                                    {
                                        Team = orbTeam.Shrine.Team
                                    };

                        if (rune.IsInWall(Grid))
                        {
                            rune = new Rune(orbTeam.ShrineOrb.ObjectId, arenaPlayer, SpellManager.CTFOrbSpell, arenaPlayer.BoundingBox.Origin, (Single)(arenaPlayer.Direction + Math.PI), new Byte[20])
                                    {
                                        Team = orbTeam.Shrine.Team
                                    };
                        }

                        if (rune.BoundingBox.IsBelowDeathZ || rune.IsInWall(Grid))
                        {
                            orbTeam.ShrineOrb.ResetOrb();

                            Network.SendTo(this, GamePacket.Outgoing.System.DirectTextMessage(null, String.Format("The {0} orb has been returned to its shrine.", rune.Team)), Network.SendToType.Arena);
                        }
                        else
                        {
                            if (orbTeam.ShrineOrb.ChangeState(rune) == CTFOrbState.OnGround)
                            {
                                Runes.Add(rune);

                                Network.SendToArena(arenaPlayer, GamePacket.Outgoing.System.DirectTextMessage(arenaPlayer.WorldPlayer, String.Format("The {0} orb has been dropped by {1}.", orbTeam.Shrine.Team, arenaPlayer.ActiveCharacter.Name)), false);
                                Network.Send(arenaPlayer.WorldPlayer, GamePacket.Outgoing.System.DirectTextMessage(arenaPlayer.WorldPlayer, String.Format("You have dropped the {0} orb!", orbTeam.Shrine.Team)));

                                Network.SendToArena(arenaPlayer, GamePacket.Outgoing.Arena.CastRuneEx(arenaPlayer, rune, UDP), true);
                            }
                        }
                    }
                }

                Single targetPenalty = (arenaPlayer.ActiveCharacter.Level*5);
                Single killerPenalty = 0;

                if (targetArenaPlayer != null)
                {
                    if (arenaPlayer.ActiveTeam != targetArenaPlayer.ActiveTeam || arenaPlayer.ActiveTeam == Team.Neutral)
                    {
                        Single killerAdd;

                        if (targetArenaPlayer == arenaPlayer)
                        {
                            if (arenaPlayer.OwnerArena.Ruleset.Mode == ArenaRuleset.ArenaMode.FreeForAll)
                            {
                                killerAdd = arenaPlayer.ActiveCharacter.Level * 15;
                            }
                            else
                            {
                                killerAdd = arenaPlayer.ActiveCharacter.Level * 30;
                            }
                        }
                        else
                        {
                            if (arenaPlayer.OwnerArena.Ruleset.Mode == ArenaRuleset.ArenaMode.FreeForAll)
                            {
                                killerAdd = (arenaPlayer.ActiveCharacter.Level - targetArenaPlayer.ActiveCharacter.Level) * 5;
                            }
                            else
                            {
                                killerAdd = (arenaPlayer.ActiveCharacter.Level - targetArenaPlayer.ActiveCharacter.Level) * 17;
                            }

                        }

                        if (killerAdd > 0) targetPenalty += killerAdd;
                    }
                    else
                    {
                        killerPenalty += (arenaPlayer.ActiveCharacter.Level * 3);
                        killerPenalty += (arenaPlayer.ActiveCharacter.Level - targetArenaPlayer.ActiveCharacter.Level) * 12;
                    }

                }
                
                arenaPlayer.ExpPenalty = (Int32) targetPenalty;

                if (targetArenaPlayer != null)
                {
                    if (killerPenalty > 0)
                    {
                        targetArenaPlayer.ExpPenalty = (Int32)killerPenalty;
                    }

                    if (targetArenaPlayer != arenaPlayer)
                    {
                        if (arenaPlayer.WorldPlayer.Serial == targetArenaPlayer.WorldPlayer.Serial)
                        {
                            Program.ServerForm.CheatLog.WriteMessage(String.Format("[Serial Pump] Killer: {{{0}}}, {1} ({2}) Lv.{3}, Target: {{{4}}}, {5} ({6}) Lv.{7}, Serial: {8}", targetArenaPlayer.WorldPlayer.AccountId, targetArenaPlayer.WorldPlayer.Username, targetArenaPlayer.ActiveCharacter.Name, targetArenaPlayer.ActiveCharacter.Level, arenaPlayer.WorldPlayer.AccountId, arenaPlayer.WorldPlayer.Username, arenaPlayer.ActiveCharacter.Name, arenaPlayer.ActiveCharacter.Level, arenaPlayer.WorldPlayer.Serial), Color.Red);
                            MailManager.QueueMail("Serial Pumper Detected", String.Format("Account Name: {0}\nCharacter Name: {1}\nSerial: {2}", arenaPlayer.WorldPlayer.Username, arenaPlayer.ActiveCharacter.Name, arenaPlayer.WorldPlayer.Serial));
                        }
                        else if (arenaPlayer.WorldPlayer.IpAddress == targetArenaPlayer.WorldPlayer.IpAddress)
                        {
                            Program.ServerForm.CheatLog.WriteMessage(String.Format("[IP Pump] Killer: {{{0}}}, {1} ({2}) Lv.{3}, Target: {{{4}}}, {5} ({6}) Lv.{7}", targetArenaPlayer.WorldPlayer.AccountId, targetArenaPlayer.WorldPlayer.Username, targetArenaPlayer.ActiveCharacter.Name, targetArenaPlayer.ActiveCharacter.Level, arenaPlayer.WorldPlayer.AccountId, arenaPlayer.WorldPlayer.Username, arenaPlayer.ActiveCharacter.Name, arenaPlayer.ActiveCharacter.Level), Color.Red);
                        }
                    }

                    if (arenaPlayer != targetArenaPlayer && (arenaPlayer.ActiveTeam != targetArenaPlayer.ActiveTeam || arenaPlayer.ActiveTeam == Team.Neutral))
                    {
                        Single experience = 75 + (targetArenaPlayer.ActiveCharacter.Level*14) + Math.Max(0, (arenaPlayer.ActiveCharacter.Level - targetArenaPlayer.ActiveCharacter.Level)*18);
                        GivePlayerExperience(targetArenaPlayer, experience, ArenaPlayer.ExperienceType.Combat);

                        targetArenaPlayer.KillCount++;

                        if (targetArenaPlayer.ActiveCharacter.OpLevel == 0)
                        {
                            targetArenaPlayer.ActiveCharacter.Statistics.Kills++;
                            arenaPlayer.ActiveCharacter.Statistics.Deaths++;
                        }
                    }
                }

                Network.SendTo(this, GamePacket.Outgoing.Arena.PlayerDeath(arenaPlayer, targetArenaPlayer, UDP), Network.SendToType.Arena);
            }
        }

        public void PlayerMove(ArenaPlayer arenaPlayer, ArenaPlayer.StatusFlag statusFlags, Byte mSpeed, Vector3 location, Single direction)
        {
            if (arenaPlayer.StateReceivedCount++ >= 500)
            {
                Int64 deltaState = TimeHelper.DeltaMilliseconds(arenaPlayer.LastStateReceived, NativeMethods.PerformanceCount);
                Int32 minDelta = arenaPlayer.HasFliedSinceHackDetect ? 20000 : 30000;

                if (deltaState < minDelta && !arenaPlayer.WorldPlayer.IsAdmin)
                {
                    Program.ServerForm.CheatLog.WriteMessage(String.Format("[Speedhack] (AID: {0}, {1}) {2} - Time: {3}ms/{4}ms", arenaPlayer.WorldPlayer.AccountId, arenaPlayer.WorldPlayer.Username, arenaPlayer.ActiveCharacter.Name, deltaState, minDelta), Color.Red);

                    arenaPlayer.WorldPlayer.DisconnectReason = Resources.Strings_Disconnect.SpeedHack;
                    arenaPlayer.WorldPlayer.Disconnect = true;
                }

                arenaPlayer.StateReceivedCount = 0;
                arenaPlayer.HasFliedSinceHackDetect = false;
                arenaPlayer.LastStateReceived = NativeMethods.PerformanceCount;
            }

            lock (SyncRoot)
            {
                arenaPlayer.StatusFlags = statusFlags;
                arenaPlayer.MoveSpeed = mSpeed;
                arenaPlayer.Location = location;
                arenaPlayer.PreviousLocation = location;
                arenaPlayer.Direction = direction;

                arenaPlayer.BoundingBox.MoveAndResize(arenaPlayer.Location, statusFlags.HasFlag(ArenaPlayer.StatusFlag.Crouching) ? ArenaPlayer.PlayerCrouchingSize : ArenaPlayer.PlayerStandingSize);

                arenaPlayer.CurrentGridBlock = Grid.GridBlocks.GetBlockByLocation(arenaPlayer.BoundingBox.Origin.X, arenaPlayer.BoundingBox.Origin.Y);
                arenaPlayer.CurrentGridBlockFlagData.UpdateFlagData(this, arenaPlayer.CurrentGridBlock != null ? arenaPlayer.CurrentGridBlock.BlockFlags : 0);

                if (arenaPlayer.IsDamageable && arenaPlayer.BoundingBox.IsBelowDeathZ)
                {
                    arenaPlayer.CurrentHp = 0;
                    PlayerDeath(arenaPlayer, null);
                }
            }
        }

        public void BiasedShrine(ArenaPlayer arenaPlayer, Byte shrineId, bool UDP = false)
        {
            Shrine shrine = Grid.GetShrineById(shrineId);
            if (shrine == null || shrine.IsIndestructible || !arenaPlayer.IsAlive || arenaPlayer.WorldPlayer.Flags.HasFlag(PlayerFlag.Hidden)) return;

            if (Ruleset.Rules.HasFlag(ArenaRuleset.ArenaRule.NoShrineBiasing) || Ruleset.Rules.HasFlag(ArenaRuleset.ArenaRule.CaptureTheFlag)) return;

            lock (SyncRoot)
            {
                Int32 penaltyDivider = 3;
                Int32 biasMin = 0;
                Int32 biasMax = 0;
                Int32 biasRollBonus = arenaPlayer.ActiveCharacter.Level/2;

                switch (arenaPlayer.ActiveCharacter.Class)
                {
                    case Character.PlayerClass.Runemage:
                    {
                        penaltyDivider = 3;
                        biasMin = 15;
                        biasMax = 35;
                        biasRollBonus = 22 + biasRollBonus;
                        break;
                    }
                    case Character.PlayerClass.Healer:
                    {
                        penaltyDivider = 2;
                        biasMin = 35;
                        biasMax = 70;
                        biasRollBonus = 40 + biasRollBonus;
                        break;
                    }
                    case Character.PlayerClass.Magician:
                    {
                        penaltyDivider = 3;
                        biasMin = 15;
                        biasMax = 35;
                        biasRollBonus = 22 + biasRollBonus;
                        break;
                    }
                    case Character.PlayerClass.Mystic:
                    {
                        penaltyDivider = 3;
                        biasMin = 20;
                        biasMax = 45;
                        biasRollBonus = 30 + biasRollBonus;
                        break;
                    }
                }

                if (arenaPlayer.ActiveCharacter.Level < AveragePlayerLevel)
                {
                    Int32 penalty = ((AveragePlayerLevel - arenaPlayer.ActiveCharacter.Level) / penaltyDivider);

                    biasMin = biasMin - (penalty * 2);
                    biasMax = biasMax - (penalty * 2);
                    biasRollBonus = biasRollBonus - penalty;
                }

                Int32 biasAmount = CryptoRandom.GetInt32(biasMin, biasMax);

                if (biasAmount > 0 && CryptoRandom.GetInt32(biasRollBonus, 100) > 50)
                {
                    if (arenaPlayer.ActiveTeam == shrine.Team)
                    {
                        if (shrine.CurrentBias >= 100) return;

                        Single experience = (arenaPlayer.ActiveCharacter.Level*0.05f)*(ArenaPlayers.GetTeamPlayerCount(arenaPlayer.ActiveTeam)*biasAmount);
                        GivePlayerExperience(arenaPlayer, (arenaPlayer.ActiveCharacter.Class == Character.PlayerClass.Runemage ? experience * 2 : experience), ArenaPlayer.ExperienceType.Objective);

                        shrine.CurrentBias += (Byte) biasAmount;

                        if (shrine.CurrentBias == shrine.MaxBias) biasAmount = (Byte) shrine.MaxBias;
                        Network.SendTo(this, GamePacket.Outgoing.Arena.BiasedShrine(arenaPlayer, shrine, (Byte) biasAmount, UDP), Network.SendToType.Arena);
                    }
                    else
                    {
                        if (shrine.CurrentBias <= 0) return;

                        Single experience = (arenaPlayer.ActiveCharacter.Level*0.07f)*(ArenaPlayers.GetTeamPlayerCount(shrine.Team)*biasAmount);
                        GivePlayerExperience(arenaPlayer, (arenaPlayer.ActiveCharacter.Class == Character.PlayerClass.Runemage ? experience * 2 : experience), ArenaPlayer.ExperienceType.Objective);

                        shrine.CurrentBias -= (Byte) biasAmount;

                        if (shrine.CurrentBias == 0)
                        {
                            biasAmount = -shrine.MaxBias & 0xFF;
                        }
                        else
                        {
                            biasAmount = -biasAmount & 0xFF;
                        }

                        Network.SendTo(this, GamePacket.Outgoing.Arena.BiasedShrine(arenaPlayer, shrine, (Byte) biasAmount, UDP), Network.SendToType.Arena);
                    }

                    Network.Send(arenaPlayer.WorldPlayer, GamePacket.Outgoing.Arena.UpdateExperience(arenaPlayer, UDP));
                }
                else
                {
                    Network.Send(arenaPlayer.WorldPlayer, GamePacket.Outgoing.Arena.BiasedShrine(arenaPlayer, shrine, 0, UDP));
                }
            }
        }

        public void BiasedPool(ArenaPlayer arenaPlayer, Byte poolId, bool UDP = false)
        {
            Pool pool = Grid.Pools.FindById(poolId);
            if (pool == null || !arenaPlayer.IsAlive || Ruleset.Rules.HasFlag(ArenaRuleset.ArenaRule.NoPoolBiasing) || arenaPlayer.WorldPlayer.Flags.HasFlag(PlayerFlag.Hidden)) return;

            lock (SyncRoot)
            {
                Int32 penaltyDivider = 3;
                Int32 biasMin = 0;
                Int32 biasMax = 0;
                Int32 biasRollBonus = arenaPlayer.ActiveCharacter.Level;

                switch (arenaPlayer.ActiveCharacter.Class)
                {
                    case Character.PlayerClass.Runemage:
                    {
                        penaltyDivider = 3;
                        biasMin = 40;
                        biasMax = 80;
                        biasRollBonus = Math.Max(0, (50 + biasRollBonus) - (pool.Power / 2));
                        break;
                    }
                    case Character.PlayerClass.Healer:
                    {
                        penaltyDivider = 3;
                        biasMin = 15;
                        biasMax = 40;
                        biasRollBonus = Math.Max(0, (40 + biasRollBonus) - (pool.Power/2));
                        break;
                    }
                    case Character.PlayerClass.Magician:
                    {
                        penaltyDivider = 2;
                        biasMin = 55;
                        biasMax = 90;
                        biasRollBonus = Math.Max(0, (40 + biasRollBonus) - (pool.Power/4));
                        break;
                    }
                    case Character.PlayerClass.Mystic:
                    {
                        penaltyDivider = 3;
                        biasMin = 30;
                        biasMax = 65;
                        biasRollBonus = Math.Max(0, (35 + biasRollBonus) - (pool.Power/3));
                        break;
                    }
                }

                if (arenaPlayer.ActiveCharacter.Level < AveragePlayerLevel)
                {
                    Int32 penalty = (((AveragePlayerLevel - arenaPlayer.ActiveCharacter.Level) + 3) / penaltyDivider);

                    biasMin = biasMin - penalty;
                    biasMax = biasMax - (penalty * 3);
                    biasRollBonus = biasRollBonus - penalty;
                }

                Int32 biasAmount = CryptoRandom.GetInt32(biasMin, biasMax);

                if (biasAmount > 0 && CryptoRandom.GetInt32(biasRollBonus, 100) > 50)
                {
                    GivePlayerExperience(arenaPlayer, (((arenaPlayer.ActiveCharacter.Level / 9.2f) + 0.48f) * biasAmount), ArenaPlayer.ExperienceType.Objective);

                    if (pool.Team == arenaPlayer.ActiveTeam || pool.Team == Team.Neutral)
                    {
                        pool.Team = arenaPlayer.ActiveTeam;
                        pool.CurrentBias += (Byte) biasAmount;

                        if (pool.CurrentBias == pool.MaxBias) biasAmount = Convert.ToByte(pool.MaxBias);
                        Network.SendTo(this, GamePacket.Outgoing.Arena.BiasedPool(arenaPlayer, pool, (Byte) biasAmount, UDP), Network.SendToType.Arena);
                    }
                    else
                    {
                        Int16 biasRemaining = (Int16)(biasAmount - pool.CurrentBias);

                        pool.CurrentBias -= (Int16) biasAmount;

                        if (pool.CurrentBias == 0)
                        {
                            if (arenaPlayer.ActiveCharacter.Class == Character.PlayerClass.Magician)
                            {
                                pool.Team = arenaPlayer.ActiveTeam;
                                pool.CurrentBias += biasRemaining;

                                if (pool.CurrentBias == 0) pool.CurrentBias = 1;
                                if (pool.CurrentBias == pool.MaxBias) biasAmount = Convert.ToByte(pool.MaxBias);

                                Network.SendTo(this, GamePacket.Outgoing.Arena.BiasedPool(arenaPlayer, pool, (Byte)biasAmount, UDP), Network.SendToType.Arena);
                            }
                            else
                            {
                                pool.Team = Team.Neutral;
                                biasAmount = -pool.MaxBias & 0xFF;
                            }
                        }
                        else
                        {
                            biasAmount = -biasAmount & 0xFF;
                        }

                        Network.SendTo(this, GamePacket.Outgoing.Arena.BiasedPool(arenaPlayer, pool, (Byte) biasAmount, UDP), Network.SendToType.Arena);
                    }

                    Network.Send(arenaPlayer.WorldPlayer, GamePacket.Outgoing.Arena.UpdateExperience(arenaPlayer, UDP));
                }
                else
                {
                    Network.Send(arenaPlayer.WorldPlayer, GamePacket.Outgoing.Arena.BiasedPool(arenaPlayer, pool, 0, UDP));
                }
            }
        }

        public void TappedAtShrine(ArenaPlayer arenaPlayer, bool UDP = false)
        {
            lock (SyncRoot)
            {
                if (arenaPlayer.IsAlive || (Ruleset.Rules.HasFlag(ArenaRuleset.ArenaRule.NoTapping) && !arenaPlayer.WorldPlayer.IsAdmin)) return;
                
                if (arenaPlayer.ActiveTeam == Team.Neutral || !arenaPlayer.ActiveShrine.IsDead)
                {
                    if (arenaPlayer.OwnerArena.Ruleset.Mode != ArenaRuleset.ArenaMode.FreeForAll)
                    {
                        arenaPlayer.ExpPenalty = arenaPlayer.ActiveCharacter.Level*(TeamHasHealer(arenaPlayer.ActiveTeam) ? 13 : 4);
                        Network.Send(arenaPlayer.WorldPlayer, GamePacket.Outgoing.Arena.UpdateExperience(arenaPlayer, UDP));
                    }

                    Network.SendTo(arenaPlayer.OwnerArena, GamePacket.Outgoing.Arena.TappedAtShrine(arenaPlayer, true, UDP), Network.SendToType.Arena);

                    arenaPlayer.IsInCombat = false;

                    arenaPlayer.ValhallaProtection.Reset();
                    arenaPlayer.CurrentHp = arenaPlayer.MaxHp;
                }
                else
                {
                    Network.Send(arenaPlayer.WorldPlayer, GamePacket.Outgoing.Arena.TappedAtShrine(arenaPlayer, false, UDP));
                }
            }
        }

        public void CalledGhost(ArenaPlayer arenaPlayer, ArenaPlayer targetArenaPlayer, byte[] relayBuffer, bool UDP = false)
        {
            lock (SyncRoot)
            {
                if (!arenaPlayer.IsAlive || targetArenaPlayer.IsAlive || Ruleset.Rules.HasFlag(ArenaRuleset.ArenaRule.NoRaiseCall)) return;

                Network.SendToArena(arenaPlayer, GamePacket.Outgoing.Arena.CalledGhost(arenaPlayer, targetArenaPlayer, relayBuffer, UDP), false);
            }
        }

        public void ActivatedTrigger(ArenaPlayer arenaPlayer, Trigger trigger, bool UDP = false)
        {
            Trigger currentTrigger = trigger;

            lock (SyncRoot)
            {
                arenaPlayer.IsAwayFromKeyboard = false;

                if (trigger.TriggerType == TriggerType.Teleport)
                {
                    currentTrigger = Grid.Triggers[currentTrigger.NextTrigger];
                    if (currentTrigger.TriggerId == 0) return;
                }

                if (currentTrigger.Cooldown.HasElapsed)
                {
                    lock (Grid.Triggers.SyncRoot)
                    {
                        do
                        {
                            switch (currentTrigger.CurrentState)
                            {
                                case TriggerState.Active:
                                {
                                    currentTrigger.Duration = null;
                                    currentTrigger.CurrentState = TriggerState.Inactive;
                                    break;
                                }
                                case TriggerState.Inactive:
                                {
                                    currentTrigger.Duration = new Interval(currentTrigger.ResetTimer, true);
                                    currentTrigger.CurrentState = TriggerState.Active;
                                    break;
                                }
                            }

                            currentTrigger = Grid.Triggers[currentTrigger.NextTrigger];
                        } while (currentTrigger.TriggerId > 0);
                    }

                    currentTrigger.Cooldown.Reset();
                }

                if (trigger.TriggerType == TriggerType.Teleport)
                {
                    currentTrigger = Grid.Triggers[currentTrigger.NextTrigger];

                    if (currentTrigger.TriggerId > 0)
                    {
                        Network.SendTo(this, GamePacket.Outgoing.Arena.ActivatedTrigger(currentTrigger, UDP), Network.SendToType.Arena);
                    }
                }
                else
                {
                    Network.SendTo(this, GamePacket.Outgoing.Arena.ActivatedTrigger(trigger, UDP), Network.SendToType.Arena);
                }
            }
        }

        public Boolean CastEffect(ArenaPlayer arenaPlayer, Spell spell)
        {
            lock (SyncRoot)
            {
                if (!arenaPlayer.IsAlive) return false;

                SpellCheatInfo cheatInfo = SpellManager.DoesPlayerHaveSpell(arenaPlayer.WorldPlayer, spell);

                if (!cheatInfo.HasSpell)
                {
					Program.ServerForm.CheatLog.WriteMessage(String.Format("[Spell Hack] ({0}){1} -> Spell: {2}, List Level: {3}, Spell Level: {4}, List: {5}, Error: {6}", arenaPlayer.WorldPlayer.AccountId, arenaPlayer.ActiveCharacter.Name, cheatInfo.Spell.Name, cheatInfo.ListLevel, cheatInfo.SpellLevel, cheatInfo.ListName, cheatInfo.Error), Color.Red);

					arenaPlayer.WorldPlayer.DisconnectReason = Resources.Strings_Disconnect.SpellHack;
                    arenaPlayer.WorldPlayer.Disconnect = true;
                    return false;
                }

                arenaPlayer.IsAwayFromKeyboard = false;
     
                DoPlayerEffect(arenaPlayer, arenaPlayer, spell, EffectType.Default);

                return true;
            }
        }

        public Boolean CastTargeted(ArenaPlayer arenaPlayer, ArenaPlayer targetArenaPlayer, Spell spell)
        {
            lock (SyncRoot)
            {
                if (targetArenaPlayer == null || !arenaPlayer.IsAlive) return false;

                SpellCheatInfo cheatInfo = SpellManager.DoesPlayerHaveSpell(arenaPlayer.WorldPlayer, spell);

                if (!cheatInfo.HasSpell)
                {
					Program.ServerForm.CheatLog.WriteMessage(String.Format("[Spell Hack] ({0}){1} -> Spell: {2}, List Level: {3}, Spell Level: {4}, List: {5}, Error: {6}", arenaPlayer.WorldPlayer.AccountId, arenaPlayer.ActiveCharacter.Name, cheatInfo.Spell.Name, cheatInfo.ListLevel, cheatInfo.SpellLevel, cheatInfo.ListName, cheatInfo.Error), Color.Red);

                    arenaPlayer.WorldPlayer.DisconnectReason = Resources.Strings_Disconnect.SpellHack;
                    arenaPlayer.WorldPlayer.Disconnect = true;
                    return false;
                }


                switch (spell.Friendly)
                {
                    case SpellFriendlyType.NonFriendly:
                    {
                        if (targetArenaPlayer.IsAlive && (arenaPlayer.ActiveTeam != targetArenaPlayer.ActiveTeam || targetArenaPlayer.ActiveTeam == Team.Neutral))
                        {
                            DoPlayerDamage(targetArenaPlayer, arenaPlayer, spell, null, false);

                            DoPlayerEffect(arenaPlayer, arenaPlayer, spell, EffectType.Caster);
                            if (!DoPlayerEffect(targetArenaPlayer, arenaPlayer, spell, EffectType.Target)) return false;
                        }
                        else return false;

                        break;
                    }

                    case SpellFriendlyType.Friendly:
                    {
                        if (targetArenaPlayer.IsAlive && ((arenaPlayer.ActiveTeam == targetArenaPlayer.ActiveTeam || arenaPlayer.ActiveTeam == Team.Neutral) || targetArenaPlayer.ActiveTeam == Team.Neutral))
                        {
                            DoPlayerEffect(arenaPlayer, arenaPlayer, spell, EffectType.Caster);
                            if (!DoPlayerEffect(targetArenaPlayer, arenaPlayer, spell, EffectType.Target)) return false;
                        }
                        else return false;
                        break;
                    }

                    case SpellFriendlyType.FriendlyDead:
                    {
                        if (!targetArenaPlayer.IsAlive && ((arenaPlayer.ActiveTeam == targetArenaPlayer.ActiveTeam || arenaPlayer.ActiveTeam == Team.Neutral) || targetArenaPlayer.ActiveTeam == Team.Neutral))
                        {
                            DoPlayerEffect(arenaPlayer, arenaPlayer, spell, EffectType.Caster);
                            if (!DoPlayerEffect(targetArenaPlayer, arenaPlayer, spell, EffectType.Target)) return false;
                        }
                        else return false;
                        break;
                    }
                }

                return true;
            }
        }

        public Boolean CastRune(ArenaPlayer arenaPlayer, Rune rune, bool UDP = false)
        {
            lock (SyncRoot)
            {
                if (rune.Spell.Type != SpellType.Rune || !arenaPlayer.IsAlive) return false;

                SpellCheatInfo cheatInfo = SpellManager.DoesPlayerHaveSpell(arenaPlayer.WorldPlayer, rune.Spell);

                if (!cheatInfo.HasSpell)
                {
					Program.ServerForm.CheatLog.WriteMessage(String.Format("[Spell Hack] ({0}){1} -> Spell: {2}, List Level: {3}, Spell Level: {4}, List: {5}, Error: {6}", arenaPlayer.WorldPlayer.AccountId, arenaPlayer.ActiveCharacter.Name, cheatInfo.Spell.Name, cheatInfo.ListLevel, cheatInfo.SpellLevel, cheatInfo.ListName, cheatInfo.Error), Color.Red);

					arenaPlayer.WorldPlayer.DisconnectReason = Resources.Strings_Disconnect.SpellHack;
                    arenaPlayer.WorldPlayer.Disconnect = true;
                    return false;
                }

                arenaPlayer.IsAwayFromKeyboard = false;

                if (arenaPlayer.OwnerArena.Ruleset.Rules.HasFlag(ArenaRuleset.ArenaRule.NoHinder))
                {
                    Spell runeEffect = SpellManager.Spells[rune.Spell.DeathSpellEffect];
                    if (runeEffect != null && runeEffect.Effect == SpellEffectType.Hinder)
                    {
                        Network.Send(arenaPlayer.WorldPlayer, GamePacket.Outgoing.Arena.ObjectDeath(rune.ObjectId, UDP));
                        return false;
                    }
                }

                if (rune.IsInWall(Grid))
                {
                    Network.Send(arenaPlayer.WorldPlayer, GamePacket.Outgoing.Arena.ObjectDeath(rune.ObjectId, UDP));
                    return false;
                }

                if (rune.IsAura)
                {
                    if (!rune.Spell.AuraStackable)
                    {
                        foreach (Rune arenaRune in Runes.Where(arenaRune => arenaRune.IsAura && arenaRune.Spell.Id == rune.Spell.Id))
                        {
                            BoundingSphere runeSphere = arenaRune.BoundingBox.ExtentSphere;

                            if (rune.AuraBoundingSphere.Contains(ref runeSphere) == ContainmentType.Disjoint) continue;

                            switch (arenaRune.Spell.Friendly)
                            {
                                case SpellFriendlyType.NonFriendly:
                                {
                                    if (rune.Team == arenaRune.Team) continue;

                                    break;
                                }
                                case SpellFriendlyType.Friendly:
                                {
                                    if (rune.Team != arenaRune.Team) continue;

                                    break;
                                }
                            }
                            

                            if (rune.Owner == arenaRune.Owner)
                            {
                                Network.SendToArena(arenaPlayer, GamePacket.Outgoing.Arena.ObjectDeath(arenaRune.ObjectId, UDP), true);
                                Runes.Remove(arenaRune);
                                break;
                            }

                            Network.Send(arenaPlayer.WorldPlayer, GamePacket.Outgoing.Arena.ObjectDeath(rune.ObjectId, UDP));
                            return false;
                        }
                    }

                    foreach (Rune arenaRune in Runes.Where(arenaRune => arenaRune.IsAura && arenaRune.Owner == arenaPlayer))
                    {
                        Network.SendToArena(arenaPlayer, GamePacket.Outgoing.Arena.ObjectDeath(arenaRune.ObjectId, UDP), true);
                        Runes.Remove(arenaRune);
                        break;
                    }
                }


                Runes.Add(rune);
                return true;
            }
        }

        /*public Boolean CastBolt(ArenaPlayer arenaPlayer, Bolt bolt)
        {
            lock (SyncRoot)
            {
                if (bolt.Spell.Type != SpellType.Bolt || !arenaPlayer.IsAlive) return false;

                SpellCheatInfo cheatInfo = SpellManager.DoesPlayerHaveSpell(arenaPlayer.WorldPlayer, bolt.Spell);

                if (!cheatInfo.HasSpell)
                {
					Program.ServerForm.CheatLog.WriteMessage(String.Format("[Spell Hack] ({0}){1} -> Spell: {2}, List Level: {3}, Spell Level: {4}, List: {5}, Error: {6}", arenaPlayer.WorldPlayer.AccountId, arenaPlayer.ActiveCharacter.Name, cheatInfo.Spell.Name, cheatInfo.ListLevel, cheatInfo.SpellLevel, cheatInfo.ListName, cheatInfo.Error), Color.Red);

					arenaPlayer.WorldPlayer.DisconnectReason = Resources.Strings_Disconnect.SpellHack;
                    arenaPlayer.WorldPlayer.Disconnect = true;
                    return false;
                }

                arenaPlayer.IsAwayFromKeyboard = false;
                arenaPlayer.IsInCombat = true;

                if (bolt.Target != null)
                {
                    Bolts.Add(bolt);
                }

                return true;
            }
        }*/

        public Boolean CastProjectile(ArenaPlayer arenaPlayer, Spell spell, ProjectileGroup projectileGroup)
        {
            lock (SyncRoot)
            {
                if (!arenaPlayer.IsAlive) return false;

                SpellCheatInfo cheatInfo = SpellManager.DoesPlayerHaveSpell(arenaPlayer.WorldPlayer, spell);

                if (!cheatInfo.HasSpell)
                {
					Program.ServerForm.CheatLog.WriteMessage(String.Format("[Spell Hack] ({0}){1} -> Spell: {2}, List Level: {3}, Spell Level: {4}, List: {5}, Error: {6}", arenaPlayer.WorldPlayer.AccountId, arenaPlayer.ActiveCharacter.Name, cheatInfo.Spell.Name, cheatInfo.ListLevel, cheatInfo.SpellLevel, cheatInfo.ListName, cheatInfo.Error), Color.Red);

					arenaPlayer.WorldPlayer.DisconnectReason = Resources.Strings_Disconnect.SpellHack;
                    arenaPlayer.WorldPlayer.Disconnect = true;
                    return false;
                }

                arenaPlayer.IsAwayFromKeyboard = false;

                ProjectileGroups.Add(projectileGroup);

                return true;
            }
        }

        public Boolean CastWall(ArenaPlayer arenaPlayer, Wall wall)
        {
            lock (SyncRoot)
            {
                if (!arenaPlayer.IsAlive) return false;

                SpellCheatInfo cheatInfo = SpellManager.DoesPlayerHaveSpell(arenaPlayer.WorldPlayer, wall.Spell);

                if (!cheatInfo.HasSpell)
                {
                    Program.ServerForm.CheatLog.WriteMessage(String.Format("[Spell Hack] ({0}){1} -> Spell: {2}, List Level: {3}, Spell Level: {4}, List: {5}, Error: {6}", arenaPlayer.WorldPlayer.AccountId, arenaPlayer.ActiveCharacter.Name, cheatInfo.Spell.Name, cheatInfo.ListLevel, cheatInfo.SpellLevel, cheatInfo.ListName, cheatInfo.Error), Color.Red);

					arenaPlayer.WorldPlayer.DisconnectReason = Resources.Strings_Disconnect.SpellHack;
                    arenaPlayer.WorldPlayer.Disconnect = true;
                    return false;
                }

                arenaPlayer.IsAwayFromKeyboard = false;

                if (arenaPlayer.OwnerArena.Ruleset.Rules.HasFlag(ArenaRuleset.ArenaRule.NoSolidWalls))
                {
                    if (wall.Spell.CollisionVelocity == 0)
                    {
                        Network.Send(arenaPlayer.WorldPlayer, GamePacket.Outgoing.Arena.ThinDamage(wall.ObjectId, 1000));
                        return false;
                    }
                }

                Walls.Add(wall);
                return true;
            }
        }

        public void CastDispell(ArenaPlayer arenaPlayer, Rune rune, Spell spell)
        {
            lock (SyncRoot)
            {
                if (!arenaPlayer.IsAlive) return;

                SpellCheatInfo cheatInfo = SpellManager.DoesPlayerHaveSpell(arenaPlayer.WorldPlayer, spell);

                if (!cheatInfo.HasSpell)
                {
                    Program.ServerForm.CheatLog.WriteMessage(String.Format("[Spell Hack] ({0}){1} -> Spell: {2}, List Level: {3}, Spell Level: {4}, List: {5}, Error: {6}", arenaPlayer.WorldPlayer.AccountId, arenaPlayer.ActiveCharacter.Name, cheatInfo.Spell.Name, cheatInfo.ListLevel, cheatInfo.SpellLevel, cheatInfo.ListName, cheatInfo.Error), Color.Red);

                    arenaPlayer.WorldPlayer.DisconnectReason = Resources.Strings_Disconnect.SpellHack;
                    arenaPlayer.WorldPlayer.Disconnect = true;
                    return;
                }

                arenaPlayer.IsInCombat = true;

                Program.ServerForm.MainLog.WriteMessage($"rune.objectid: {rune.ObjectId.ToString()}", Color.Red);

                Network.SendTo(this, GamePacket.Outgoing.Arena.ObjectDeath(rune.ObjectId), Network.SendToType.Arena);
                Runes.Remove(rune);
                
            }
        }

        public void CastDispell(ArenaPlayer arenaPlayer, Wall wall, Spell spell)
        {
            lock (SyncRoot)
            {
                if (!arenaPlayer.IsAlive) return;

                SpellCheatInfo cheatInfo = SpellManager.DoesPlayerHaveSpell(arenaPlayer.WorldPlayer, spell);

                if (!cheatInfo.HasSpell)
                {
					Program.ServerForm.CheatLog.WriteMessage(String.Format("[Spell Hack] ({0}){1} -> Spell: {2}, List Level: {3}, Spell Level: {4}, List: {5}, Error: {6}", arenaPlayer.WorldPlayer.AccountId, arenaPlayer.ActiveCharacter.Name, cheatInfo.Spell.Name, cheatInfo.ListLevel, cheatInfo.SpellLevel, cheatInfo.ListName, cheatInfo.Error), Color.Red);

					arenaPlayer.WorldPlayer.DisconnectReason = Resources.Strings_Disconnect.SpellHack;
                    arenaPlayer.WorldPlayer.Disconnect = true;
                    return;
                }

                arenaPlayer.IsInCombat = true;

                DoWallDamage(arenaPlayer, wall, spell, null);
            }
        }

        public void ThinDamage(ArenaPlayer arenaPlayer, Int16 thinId, Int16 damage, bool UDP = false)
        {
            if (!arenaPlayer.IsAlive) return;

            arenaPlayer.IsInCombat = true;

            Wall wall = Walls.FindById(thinId);

            if (wall == null)
            {
                Network.Send(arenaPlayer.WorldPlayer, GamePacket.Outgoing.Arena.ThinDamage(thinId, 1000, UDP));
                return;
            }

            lock (SyncRoot)
            {
                DoWallDamage(arenaPlayer, wall, damage);
            }
        }
        public Boolean TeamHasHealer(Team team)
        {
            return ArenaPlayers.Any(t => t.ActiveTeam == team && t.ActiveCharacter.Class == Character.PlayerClass.Healer);
        }
    }
}
