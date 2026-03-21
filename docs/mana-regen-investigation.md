# Mana/Ley Regen Investigation

**Confidence: LOW** — This is a preliminary analysis from pcap data and server code reading. The pool ID mismatch theory has not been verified in-game or in IDA. The client-side regen logic has not been reverse-engineered.

## Problem

Players report mana/ley regen doesn't work correctly. One session showed a full purple regen bar from standing near the nexus (first time observed), captured in `packet_captures/Full_Mana_Regen_Purple_Bar_From_Standing_Next_to_nexus.pcapng`.

## Key Finding: Mana is entirely client-side

The server has **no mana tracking at all**. `ArenaPlayer` has no mana/ley/stamina fields. The server only tracks HP regen (Arena.cs:645-650, 1%/tick out of combat, 3% with FastRegen rule).

The client manages mana internally:
- Knows earthnode positions from WORLD.DAT `[earthbloodNN]` sections
- Calculates proximity to nodes locally
- Renders the purple regen bar
- Tracks mana pool and spell costs

## What the server DOES send

### Pool/node ownership state
- `UpdateShrinePoolState` (on arena entry): sends all pool IDs with `team`, `currentBias`, `power`
- `BiasedPool` (on bias event): sends updated `poolId`, `team`, `currentBias`, `biasAmount`

### HP regen only
```csharp
// Arena.cs:645-650
if (doHealthRegen && !NoRegen && arenaPlayer.IsAlive && arenaPlayer.CurrentHp < arenaPlayer.MaxHp && !arenaPlayer.IsInCombat)
{
    Single regenAmount = FastRegen ? 0.03f : 0.01f;
    arenaPlayer.CurrentHp += (short)Math.Ceiling(arenaPlayer.MaxHp * regenAmount);
    Network.Send(arenaPlayer.WorldPlayer, UpdateHealth(arenaPlayer, UDP));
}
```

## Possible Pool ID Mismatch on Grid00 (Kaelgard Keep)

WORLD.DAT for grid00 has `numearthblood=10` but `earthblood04` is **commented out**:

```ini
[earthblood03]
power= 11
fixture= 9

;[earthblood04]       <-- commented out with semicolon
power= 11
fixture= 5

[earthblood05]
power= 11
fixture= 1
```

The server loads pools sequentially 0-9 using `GetPrivateProfileInt`:
```csharp
Int32 poolCount = GetPrivateProfileInt32("earthblooddefs", "numearthblood", WorldFilename);  // 10
for (Int32 x = 0; x < poolCount; x++)
    Pools.Add(new Pool((byte)x, GetPrivateProfileInt16($"earthblood{x:00}", "power", WorldFilename), 100));
```

If `GetPrivateProfileInt` returns 0 for the commented-out section, the server creates pool ID 4 with power=0. But if the client skips the commented section, its pool IDs 5-9 would map to different physical locations than the server's pool IDs 5-9.

**This would break biasing**: server says "pool 5 changed team" but client thinks pool 5 is at a different location. Standing near a biased node wouldn't show regen because the client thinks a different node was biased.

**This theory is unverified.** We don't know:
- Whether `GetPrivateProfileInt` (Windows INI API) skips commented sections or reads them
- Whether the client parses the same file the same way
- Whether the client even uses pool IDs from the server to determine regen proximity
- Grid01 and grid02 don't have commented-out sections, so this issue would be grid00-only

## Pool counts by grid

| Grid | Arena | numearthblood | Commented out |
|------|-------|---------------|---------------|
| grid00 | Kaelgard Keep | 10 | earthblood04 |
| grid01 | Rathespa Temple | 9 | none |
| grid02 | Tehouxican Ruins | 14 | none |

## Pcap Analysis

Opcode 0xB0 (CastProjectile) was initially suspected as mana-related but is present in all captures — it's spell casts, not regen.

Byte 9 of PlayerMoveState (0x01) was also investigated — it has full 0x00-0xFF range in all captures, not mana-specific. Likely part of the position/direction bitfield.

No server-sent packet was found that explicitly controls client mana regen rate or proximity.

## Next Steps

1. **Verify in IDA**: Find the client's earthblood parsing code — does it skip commented sections?
2. **Test pool ID alignment**: Bias a specific node on grid00, check if the correct node changes color client-side
3. **Check if regen works on grid01/grid02**: If it works there but not grid00, the commented-out section is likely the cause
4. **Fix**: Either uncomment earthblood04 or make the server skip it (change poolCount to 9 and adjust IDs)
5. **Server-side mana**: Consider whether mana should be server-authoritative (anti-cheat) — currently a cheater can cast without mana cost
