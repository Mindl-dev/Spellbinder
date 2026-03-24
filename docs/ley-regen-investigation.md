# Ley Regen Investigation — Client-Side Class-Specific Regen

## Problem
Healers have full regen everywhere. Mages, mystics, and runemages don't get regen from biased nodes. The earthpower HUD bars work correctly, but the purple regen bar doesn't respond to class-specific rules from the game manual.

## What Works
- Earthpower bars show correct team power (fixed: power byte was 0)
- Biasing works with connectivity checks
- Ley lines draw on minimap (client reads node links from WORLD.DAT)
- Standing on nexus shows full regen for healers (correct)

## What Doesn't Work
- Healer regen doesn't decrease with distance from nexus
- Magician gets no regen near connected team nodes
- Mystic gets no regen from total team network power
- Runemage gets no regen from node contact

## Game Manual Rules
- **Healer**: power from proximity to own nexus
- **Magician**: power from nearby team nodes with ley lines to nexus
- **Mystic**: power from team's total network (anywhere on map)
- **Runemage**: charges on node contact, drains away from nodes

## IDA Findings

### HUD Tick (sub_4929E0 at 0x4929E0)
The regen bar code at 0x4935D7-0x493676:
```c
v21 = byte_7E0DB8[playerGridPos];  // spatial grid: nearest node
if (v21 >= 20 && v21 <= 49)        // earthblood node
    v41 = dword_7F8F58[nodeIdx] * 0.01;  // bias as 0-1
    v24 = dword_7F8F54[nodeIdx];         // team
else if (v21 >= 50 && v21 <= 69)   // shrine
    v41 = dword_7D4698[shrineIdx] * 0.01;
    v24 = dword_7D4694[shrineIdx];
v52 = v24 * 0.25;  // team factor
```
This code has NO class-specific logic — same for all classes.

### Ley Power Calculation (sub_4449E0 at 0x4449E0)
Recursive BFS through node network:
```c
result = dword_7F8F58[node] * dword_7F8F5C[node];  // bias * power
// Traverse 5 links via unk_7F8F80[20*nodeId]
```
Writes team markers to `dword_7F8F98[20*nodeId]`.

### Key Data Addresses
- `dword_7F8F50[20*nodeId]` — node existence flag
- `dword_7F8F54[20*nodeId]` — node team (alignment)
- `dword_7F8F58[20*nodeId]` — node bias level (0-100)
- `dword_7F8F5C[20*nodeId]` — node power
- `unk_7F8F80[20*nodeId]`   — node links (5 per node)
- `dword_7F8F98[20*nodeId]` — team marker (set by ley calc)
- `byte_7E0DB8[X>>6<<7 + Y>>6]` — spatial grid (position → nearest node/shrine)
- `dword_6A082C + 136`      — power accumulator

### Shrine Data
- `dword_7D4690[18*shrineIdx]` — shrine existence
- `dword_7D4694[18*shrineIdx]` — shrine team
- `dword_7D4698[18*shrineIdx]` — shrine bias

## Next Steps
1. **Find xrefs to player class field** — the client must check class SOMEWHERE for regen
2. **Find power accumulator writes** — `dword_6A082C + 136` is modified by bias events. There may be a periodic update that uses class logic
3. **Check sub_440590** — called when a node reaches 100% bias while player stands on it. Might trigger regen recalculation
4. **Look for a timer-based regen tick** — the client might have a WM_TIMER or game loop tick that calculates regen based on class + position + network state
5. **Check if there's a server packet we're not sending** — maybe the server is supposed to send a periodic "your regen rate is X" update that we've never implemented
