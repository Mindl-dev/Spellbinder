# Ley Regen Investigation — Client-Side Class-Specific Regen

## RESOLVED — 2026-03-26

**Root cause: OpLevel (admin level) in client slot 3 bypasses all class-specific regen.**

The regen function `sub_444A60` checks `sub_4984C0(3)` (OpLevel) at `0x444AA4`. If nonzero, returns 50 (max regen) immediately — admin characters get full regen as a built-in perk. All testing was done with admin accounts, masking the working class-specific logic.

**Fix: Test with non-admin accounts.** No server code changes needed.

## Class-Specific Regen (sub_444A60)

Fully implemented in the client. Branched on `sub_4984C0(114)` (class slot):

- **Class 0 (Magician)**: Proximity to own team nodes (connected to shrine via team markers) + team network power. Distance falloff within 1024 units. Returns `networkPower * (1024 - dist) / 1024`.
- **Class 1 (Runemage)**: Reads charge field at `this[42] / 100`. Capped 0-50. Charges on node contact, drains away.
- **Class 2 (Mystic)**: `networkPower / 2 + 13`, capped at 38. Works anywhere on map.
- **Class 3 (Healer)**: Distance to nearest shrine. Own shrine → positive regen from `25 * bias / 100 + networkPower` with distance falloff using shrine power radius. Enemy shrine → negative regen `-50 * bias / 100`.

## Team Encoding — Confirmed Correct

Server Team enum matches client WORLD.DAT alignment values:
- Dragon = chaos = 1
- Gryphon = order = 2
- Phoenix = balance = 3

No TeamWire conversion needed. Verified via `tools/read_shrine_teams.py` (ReadProcessMemory).

## Key IDA Addresses

### Regen Calculator (sub_444A60)
- `0x444AA4` — OpLevel check: `if (sub_4984C0(3)) return 50`
- `0x444AAC` — Class branch: `if (v3)` where v3 = `sub_4984C0(114)`
- `0x444BAF` — Healer path (class 3)
- `0x444CCC` — Mystic path (class 2)
- `0x444CFD` — Runemage path (class 1)
- `0x444AB8` — Magician path (class 0, default)

### Team Network Power (sub_444910)
- Scans `dword_7D4694` for shrine matching team arg
- Checks `dword_7D4698` (shrine bias) — returns 0 if bias is 0
- Traverses shrine links → BFS via `sub_4449E0`
- Stores result / 100 in `dword_645E64[team]`

### HUD Tick (sub_4929E0)
- `0x493520-0x493540` — Calls `sub_444910(1)`, `sub_444910(2)`, `sub_444910(3)` for team power
- `0x4934B1` — Calls `sub_444A60` for regen bar value
- `0x4934D4-0x493502` — `sub_47F630(0-3, ...)` sets HP/mana/stamina/regen bars
- `0x4935D7-0x493676` — Earthpower proximity display (spatial grid lookup)

### BFS Ley Power (sub_4449E0)
- Recursive: accumulates `bias * power` for connected nodes
- Sets `dword_7F8F98[20*nodeId]` team markers
- Only traverses nodes where `dword_7F8F54[node] == team`

### Client Slot System
- `sub_4984C0(N)` — read slot N
- `sub_498400(val, ?, slot, ?)` — write slot
- Slot 3: OpLevel (admin level, 0=normal, 5=dev)
- Slot 4: Player team (1=Dragon, 2=Gryphon, 3=Phoenix)
- Slot 114: Player class (0=Magician, 1=Runemage, 2=Mystic, 3=Healer)
- Slot 118: Player ID

### Data Structures
- **Node**: 20 DWORDs (80 bytes) at `dword_7F8F50[20*nodeId]`
  - +0x00: existence, +0x04: team, +0x08: bias, +0x0C: power
  - +0x30: links (5 entries), +0x48: team marker (BFS output)
- **Shrine**: 18 DWORDs (72 bytes) at `dword_7D4690[18*shrineIdx]`
  - +0x00: existence, +0x04: team, +0x08: bias, +0x0C: power
  - +0x14: X pos, +0x18: Y pos, +0x44: power radius
  - +0x2C: links (5 entries)
- **Spatial grid**: `byte_7E0DB8[X>>6<<7 + Y>>6]` — 20-49=earthblood, 50-69=shrine

### WORLD.DAT
- Valid for SpellBinder (not Magestorm as suspected)
- Shrine fixtures (10,11,12) map to nif=417 nexus models in NIFS.DAT
- NIFS.DAT also has nif=403 nexuses at fixtures 19,20,21 (second set)
- Shrine alignment: shrine00=balance(Phoenix), shrine01=order(Gryphon), shrine02=chaos(Dragon)

## Tools
- `tools/read_shrine_teams.py` — ReadProcessMemory tool to dump shrine/node/player team values from running client. `--watch` flag for live updates. No debugger needed.
