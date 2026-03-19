# GamePacket.cs Test Coverage

## Summary
- **134 packet-related tests** (104 GamePacket + 30 PacketReader/Writer)
- **57 outgoing builders tested** — byte-exact layout verification
- **47 incoming handlers tested** — null guards, parsing, edge cases

## Outgoing Builders — TESTED (57)

| Method | Size | Notes |
|--------|------|-------|
| Arena.SuccessfulArenaEntry | 4B | |
| Arena.CastEffect | 6B | + high spellId variant |
| Arena.PlayerJump | 6B | |
| Arena.PlayerGod | 4B | true + false |
| Arena.PlayerMoveState (relay) | 14B | byte preservation |
| Arena.PlayerMoveStateShort (relay) | 10B | byte preservation |
| Arena.CastTargeted (relay) | 30B | byte preservation |
| Arena.CastRune (relay) | 22B | byte preservation |
| Arena.CastBolt (relay) | 36B | byte preservation |
| Arena.CastProjectile (relay) | 18B | byte preservation |
| Arena.CastWall (relay) | 20B | byte preservation |
| Arena.PlayerLeave | 4B | |
| Arena.PlayerState | 10B | alive flag inverted (0=alive) |
| Arena.UpdateHealth | 8B | HP in LE (intentional) |
| Arena.UpdateExperience | 8B | |
| Arena.PlayerHit | 4B | |
| Arena.PlayerDeath | 6B | |
| Arena.PlayerResurrect | 6B | |
| Arena.ObjectDeath (no player) | 8B | |
| Arena.ObjectDeath (with player) | 8B | |
| Arena.ThinDamage | 8B | |
| Arena.PlayerDamage | 8B | reuses UpdateHealth opcode, HP LE |
| Arena.PlayerDamage (null attacker) | 8B | |
| Arena.CastTargetedEx | 30B | + null source variant |
| Arena.PlayerJoin | 27B | CabalTag "" not null for cabalId=0 |
| Arena.CalledGhost (relay) | 12B | byte preservation |
| Arena.TappedAtShrine | 6B | reuses PlayerResurrect opcode, canRes + cannotRes |
| Arena.BiasedShrine | 10B | |
| Arena.BiasedPool | 10B | |
| Arena.ActivatedTrigger | 7B | active + inactive states |
| Arena.SpawnPlayer | 1B | NO standard header |
| Arena.PlaySound | 10B | |
| Login.Connected | 45B | version bytes + username + padding |
| Login.Error | 7B | all error types |
| Player.SendPlayerId (Player) | 4B | LE (intentional) |
| Player.SendPlayerId (ArenaPlayer) | 4B | |
| Player.HeartbeatReply | 6B | |
| Player.SaveSuccess | 23B | |
| Player.SaveError | 44B | |
| Player.SwitchedToTable | 6B | |
| Player.HasEnteredWorld | 4B | |
| System.SendAdminStatus | 5B | true + false |
| System.DirectTextMessage | var | null player + with player |
| World.SpawnPlayer | 17B | placeholder zeros + model=0xC9 |
| World.PlayerLeave | 4B | |
| World.TableDeleted | 3B | |
| World.ArenaCreated | var | |
| World.ArenaDeleted | 3B | |

## Outgoing Builders — NOT TESTED

| Method | Complexity | Blocker |
|--------|-----------|---------|
| Arena.ArenaPlayerEnterLarge | HIGH | Batched, needs CabalManager singleton |
| Arena.CastRuneEx | MED | Needs Rune with BoundingBox + MathHelper |
| Arena.CastProjectile (constructed) | MED | Needs Projectile object |
| Arena.UpdateShrinePoolState | HIGH | Needs full Arena with 3 teams + 20 pools |
| Player.Chat | HIGH | Complex branching (null player, arena vs world, ChatType) |
| Player.InviteToTable | MED | Needs Table + BitArray |
| Player.EstablishDatagram | MED | Needs Network.LocalIPAddress() + Settings |
| Study.LeaveCabal | MED | Needs 2 Players + Cabal |
| Study.InviteCabal | MED | Same |
| Study.CabalIDUpdate | LOW | Needs Player with CabalId |
| Study.CabalJoin | HIGH | Needs CabalManager singleton |
| Study.SendCabalList | HIGH | Needs CabalManager singleton |
| Study.IsNameValid | LOW | Needs Player (unused) |
| Study.IsNameTaken | LOW | Needs Player.Username |
| Study.SendCharacterInSlot | HIGH | ~500B packet, needs DataTable schema |
| Study.HighScores | MED | Needs DataTable |
| System.DrawBoundingBox | LOW | Debug only |
| World.PlayerJoin (World) | MED | Needs Player with ActiveArena |
| World.PlayerEnterLarge | HIGH | Batched, needs Player with many fields |
| World.WorldEnterLarge | HIGH | Batched, needs Arena |
| World.TableCreated | MED | Needs Table with readonly fields |
| World.ArenaState | HIGHEST | Largest packet, full Arena/Team/Shrine tree |
| World.ArenaForceEndState | HIGH | Similar to ArenaState |

## Incoming Handlers — TESTED

| Method | What's tested |
|--------|--------------|
| 17 Arena handlers | Null guard (ActiveArena/ArenaPlayer null → returns early) |
| Arena.CastEffect | spellId parse + spell lookup + invalid spellId |
| Arena.CastTargeted | spellId, targetId, isResisted parse |
| Arena.CastBolt | Documents missing FlipBytes bug (reads LE) |
| Arena.CastProjectile | All field offsets (spellId, x, y, z, direction, angle) |
| Arena.CastWall | All field offsets |
| Arena.CastRune | All field offsets + Rune creation |
| Arena.CastDispell | Documents reversed field order (x,y,z,dir before spellId) |
| PlayerMoveState | 8 tests: bitfield parsing in isolation (direction, element, Z±, speed, X, Y, flags, full round-trip) |
| Player.Chat | Short message, oversized (>128 dropped), exact max (128) |
| Player.ExitWorld | Null guard |
| Login.Disconnect | Sets Disconnect flag + reason |
| MageHook.HackNotification | Sets Disconnect flag |
| MageHook.CheatProgramNotification | Sets Disconnect flag |
| World.Deobfuscate | Pure function: null, empty, single char, nibble swap, round-trip |

## Incoming Handlers — NOT TESTED

| Method | Complexity | Blocker |
|--------|-----------|---------|
| Arena.ArenaClientEndState | TRIVIAL | Needs ActiveArena (sets flag) |
| Arena.ScoreRegistered | MED | Parses 110-byte payload, no downstream action |
| Arena.PlayerInit | MED | Z sign encoding, opLevel admin check |
| Arena.PlayerMoveState (full handler) | HIGH | Needs Arena.PlayerMove + Network.SendToArena |
| Arena.PlayerMoveStateShort (full handler) | HIGH | Same |
| Arena.CalledGhost | MED | Dereferences before null check (known bug) |
| Arena.BiasedPool (full logic) | MED | Needs Arena.BiasedPool method |
| Arena.BiasedShrine (full logic) | MED | Needs Arena.BiasedShrine method |
| Arena.ThinDamage (full logic) | MED | Needs Arena object |
| Arena.ActivatedTrigger (full logic) | MED | Needs Arena + Trigger |
| Arena.TappedAtShrine | TRIVIAL | Just sets flag |
| Character.Save | HIGH | Full character parse + DB write |
| Character.Delete | MED | DB delete |
| Login.Authenticate | HIGH | 280+ byte packet, Subscription module |
| Player.EstablishDatagram | MED | IP + port parse, Network.Send |
| Player.Heartbeat | LOW | UInt32 parse, private setter sends reply |
| Player.HasEnteredWorld | TRIVIAL | Calls Network.Send |
| Player.EnterWorld | MED | worldId routing + World module |
| Player.SwitchedToTableOrArena | TRIVIAL | Sets TableId (has side effects) |
| Player.InviteToTable | HIGH | BitArray + player lookup |
| All Study.* incoming | MED-HIGH | DB + CabalManager dependencies |
| All World.* incoming | MED-HIGH | Player/Arena lookup + Network.Send |

## Known Protocol Quirks (Documented in Tests)

1. **SendPlayerId(Player)** — LE intentional (confirmed via pcap, opcode 0x80 overloaded)
2. **UpdateHealth / PlayerDamage** — HP in LE (same x86 native byte order pattern)
3. **PlayerState** — alive flag inverted (0=alive, 1=dead)
4. **TappedAtShrine** — reuses PlayerResurrect opcode (0x21)
5. **PlayerJoin** — 27 bytes not 25 (CabalTag returns "" not null for cabalId=0)
6. **Arena.SpawnPlayer** — NO standard packet header (just raw byte)
7. **CastBolt** — missing FlipBytes on spellId (reads LE, all others read BE)
8. **CastBolt** — incomplete handler (no relay/send after parsing)
9. **CastDispell** — reversed field order (x,y,z,direction before spellId)
10. **PlayerDamage** — reuses UpdateHealth opcode (0x13)
