using System;
using System.Linq;
using System.Threading;
using Helper;
using Helper.Timing;
using System.Drawing;

namespace MageServer
{  
    public class ArenaManager : ListCollection<Arena>
    {
        public static ArenaManager Arenas = new ArenaManager();

        public new void Add(Arena arena)
        {
            base.Add(arena);

            Network.SendTo(GamePacket.Outgoing.World.ArenaCreated(arena), Network.SendToType.Tavern);

            for (Int32 j = 0; j < PlayerManager.Players.Count; j++)
            {
                Player player = PlayerManager.Players[j];
                if (player == null) continue;

                if (!player.IsInArena && player.TableId != 0)
                {
                    Network.Send(player, GamePacket.Outgoing.World.ArenaState(arena, player));
                }
            }
        }

        public Arena FindById(UInt32 arenaId)
        {
            return this.FirstOrDefault(a => arenaId == a.ArenaId);
        }
        public Arena FindByTableId(Int16 tableId)
        {
            return this.FirstOrDefault(a => tableId == a.TableId);
        }

        public Byte GetAvailableArenaId()
        {
            for (Byte i = 1; i <= 16; i++)
            {
                if (FindById(i) == null) return i;
            }
            return 0;
        }

        public readonly Thread WorkerThread;

        public Interval StatusTick = new Interval(2000, false);

        public ArenaManager()
        {
            WorkerThread = new Thread(ProcessArenas);
            WorkerThread.Start();
        }    

        private void ProcessArenas()
        {
            Boolean resetStatusUpdate = false;

            while (WorkerThread != null)
            {
                lock (Arenas.SyncRoot)
                {
                    for (Int32 i = Arenas.Count - 1; i >= 0; i--)
                    {
                        Arena arena = Arenas[i];
                        if (arena == null) continue;

                        lock (arena.SyncRoot)
                        {
                            if (arena.CurrentState == Arena.State.Ended) continue;

                            if (arena.CurrentState == Arena.State.CleanUp)
                            {
                                Arenas.Remove(arena);
                                continue;
                            }

                            Team winningTeam = arena.WinningTeam;

                            if (arena.CountdownTick == null && winningTeam != Team.Neutral)
                            {
                                switch (winningTeam)
                                {
                                    case Team.Dragon:
                                    {
                                        arena.CurrentState = Arena.State.DragonVictory;
                                        break;
                                    }
                                    case Team.Pheonix:
                                    {
                                        arena.CurrentState = Arena.State.PheonixVictory;
                                        break;
                                    }
                                    case Team.Gryphon:
                                    {
                                        arena.CurrentState = Arena.State.GryphonVictory;
                                        break;
                                    }
                                }

                                arena.CountdownTick = new Interval(29000, false);
                            }
                            else if (winningTeam == Team.Neutral)
                            {
                                arena.CurrentState = Arena.State.Normal;
                                arena.CountdownTick = null;
                            }

                            if (arena.Ruleset.Rules.HasFlag(ArenaRuleset.ArenaRule.GuildRules))
                            {
                                if (arena.GuildRulesBroadcast.HasElapsed)
                                {
                                    if (arena.ArenaPlayers.GetTeamPlayerCount(Team.Dragon) > 0 || !arena.ArenaTeams.Dragon.Shrine.IsDead)
                                    {
                                        Network.SendTo(arena, GamePacket.Outgoing.System.DirectTextMessage(null, String.Format("[Guild Match] Dragon: {0:0.00}", arena.ArenaTeams.Dragon.Shrine.GuildPoints)), Network.SendToType.Arena);
                                    }

                                    if (arena.ArenaPlayers.GetTeamPlayerCount(Team.Gryphon) > 0 || !arena.ArenaTeams.Gryphon.Shrine.IsDead)
                                    {
                                        Network.SendTo(arena, GamePacket.Outgoing.System.DirectTextMessage(null, String.Format("[Guild Match] Gryphon: {0:0.00}", arena.ArenaTeams.Gryphon.Shrine.GuildPoints)), Network.SendToType.Arena);
                                    }

                                    if (arena.ArenaPlayers.GetTeamPlayerCount(Team.Pheonix) > 0 || !arena.ArenaTeams.Pheonix.Shrine.IsDead)
                                    {
                                        Network.SendTo(arena, GamePacket.Outgoing.System.DirectTextMessage(null, String.Format("[Guild Match] Pheonix: {0:0.00}", arena.ArenaTeams.Pheonix.Shrine.GuildPoints)), Network.SendToType.Arena);
                                    }

                                    Team guildWinTeam = Team.Neutral;

                                    if (winningTeam == Team.Neutral)
                                    {
                                        if (arena.ArenaTeams.Gryphon.Shrine.GuildPoints > arena.ArenaTeams.Dragon.Shrine.GuildPoints && arena.ArenaTeams.Gryphon.Shrine.GuildPoints > arena.ArenaTeams.Pheonix.Shrine.GuildPoints)
                                        {
                                            guildWinTeam = Team.Gryphon;
                                        }

                                        if (arena.ArenaTeams.Pheonix.Shrine.GuildPoints > arena.ArenaTeams.Dragon.Shrine.GuildPoints && arena.ArenaTeams.Pheonix.Shrine.GuildPoints > arena.ArenaTeams.Gryphon.Shrine.GuildPoints)
                                        {
                                            guildWinTeam = Team.Pheonix;
                                        }

                                        if (arena.ArenaTeams.Dragon.Shrine.GuildPoints > arena.ArenaTeams.Gryphon.Shrine.GuildPoints && arena.ArenaTeams.Dragon.Shrine.GuildPoints > arena.ArenaTeams.Pheonix.Shrine.GuildPoints)
                                        {
                                            guildWinTeam = Team.Dragon;

                                        }
                                    }
                                    else
                                    {
                                        guildWinTeam = winningTeam;
                                    }

                                    Network.SendTo(arena, GamePacket.Outgoing.System.DirectTextMessage(null, String.Format("[Guild Match] Winning Team: {0}", (guildWinTeam == Team.Neutral) ? "None" : guildWinTeam.ToString())), Network.SendToType.Arena);
                                }

                                if (arena.Duration.RemainingSeconds < 600 && arena.GuildRulesBroadcast.Duration == 600000)
                                {
                                    arena.GuildRulesBroadcast = new Interval(120000, true);
                                }

                                if (arena.CountdownTick != null && arena.CountdownTick.ElapsedSeconds >= 9)
                                {
                                    Single pointsGiven = 1f;

                                    if (arena.CountdownTick.ElapsedSeconds >= 10)
                                    {
                                        pointsGiven += 0.33f * (arena.CountdownTick.ElapsedSeconds - 10);
                                    }

                                    switch (winningTeam)
                                    {
                                        case Team.Dragon:
                                        {
                                            arena.ArenaTeams.Dragon.Shrine.GuildPoints += pointsGiven;
                                            break;
                                        }
                                        case Team.Pheonix:
                                        {
                                            arena.ArenaTeams.Pheonix.Shrine.GuildPoints += pointsGiven;
                                            break;
                                        }
                                        case Team.Gryphon:
                                        {
                                            arena.ArenaTeams.Gryphon.Shrine.GuildPoints += pointsGiven;
                                            break;
                                        }
                                        case Team.Neutral:
                                        {
                                            break;
                                        }
                                    }
                                }

                            }

                            if (arena.CountdownTick != null && arena.CountdownTick.HasElapsed)
                            {
                                arena.EndState = arena.CurrentState;
                                arena.CurrentState = Arena.State.Ended;
                                continue;
                            }

                            if ((arena.TimeLimit - arena.Duration.ElapsedSeconds) <= 60 && arena.CurrentState == Arena.State.Normal)
                            {
                                arena.CurrentState = Arena.State.OneMinute;
                            }

                            if (arena.Duration.HasElapsed)
                            {
                                arena.CurrentState = Arena.State.Ended;
                                continue;
                            }

                            if (StatusTick.HasElapsed)
                            {
								if (arena.ArenaPlayers.Count > 0) arena.IdleDuration.Reset();
								if (arena.IdleDuration.HasElapsed) arena.CurrentState = Arena.State.Ended;

                                resetStatusUpdate = true;

                                for (Int32 j = 0; j < arena.ArenaPlayers.Count; j++)
                                {
                                    ArenaPlayer arenaPlayer = arena.ArenaPlayers[j];
                                    if (arenaPlayer == null) continue;

                                    Network.Send(arenaPlayer.WorldPlayer, GamePacket.Outgoing.World.ArenaState(arena, arenaPlayer.WorldPlayer));
                                    Network.SendTo(arena, GamePacket.Outgoing.Arena.PlayerState(arenaPlayer), Network.SendToType.Arena);
                                }
                            }
                        }
                    }
                }

                if (resetStatusUpdate)
                {
                    resetStatusUpdate = false;
                    StatusTick.Reset();
                }

                Thread.Sleep(100);
            }
        }
    }
}