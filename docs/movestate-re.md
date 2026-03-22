# PlayerMoveState Packet RE

Reverse engineering notes from 2026-03-21/22 session. Findings from controlled pcap tests + IDA analysis of game.dll.

## Packet Structure

```
[12-byte 1B1B header] [12-byte payload] [2-byte checksum]

Payload:
  bytes 0-1:  padding (0x00) + func_id (0x01) — skipped by Seek(2)
  bytes 2-11: 10 bytes of data read into data[0-9]
```

## Data Layout (from pcap evidence)

```
data[0-1]:  walk direction + element + flags (see assembly below) 
data[2-3]:  changes during E/W movement — server labels this "Z" but it's X (E/W position)
data[4-5]:  changes during N/S movement — server labels this "X" but it's Y (N/S position)
data[6-7]:  wraps 0-4095 during pure rotation — server labels this "Y" but it's HEADING
data[8-9]:  changes during jumping — vertical velocity or Z-related
```

### Heading Convention

- 0 = south, 1024 = west, 2048 = north, 3072 = east
- Degrees: `((raw * 360 / 4096) + 180) % 360`
- 12-bit value, wraps at 4096

### Coordinate Convention (from IDA F_DISPELL handler at 0x42BB10)

- X += -sin(angle)
- Y += cos(angle)
- Z = height
- angle 0 = north (+Y direction), increases clockwise

## Assembly Analysis (sub_42A470 — MoveState packet builder)

Stack frame = 12-byte packet buffer:

```
var_C  (esp+0x08, 2 bytes) = data[0-1]  walk angle + element + flags
var_A  (esp+0x0A, 2 bytes) = data[2-3]  Z height + speed
var_8  (esp+0x0C, 2 bytes) = data[4-5]  X position + flags
var_6  (esp+0x0E, 2 bytes) = data[6-7]  Y position + flags
var_4  (esp+0x10, 2 bytes) = data[8-9]  heading + team
var_2  (esp+0x12, 1 byte)  = data[10]   vertical velocity
var_1  (esp+0x13, 1 byte)  = data[11]   class/element encoding
```

### data[0-1] (var_C): Walk Direction + Element + Flags

Source: `[player_struct + 0x1C]` (walk angle, 9-bit with sign handling)

```
bits 0-8:   walk direction angle (0-511, 256 offset for negative)
bits 9-10:  element ID (0-3)
bit 11:     0x800 flag (movement flags != 0)
bit 12:     0x1000 flag (dword_645E60 bit 4)
bit 13:     0x2000 flag (dword_645E60 bit 5)
bit 14:     0x4000 flag (sub_440770 result)
bit 15:     0x8000 flag (sub_440780 result)
```

### data[2-3] (var_A): Z Height + Speed

Source: `[player_struct + 0x10]` minus origin at `[esi + 0x2C]`

```
bits 0-10:  Z position (11-bit absolute value)
bit 11:     Z sign (0x800 = negative)
bits 12-15: speed scalar (4-bit, 0-15, derived from sub_4984C0(107) / sub_445290())
```

### data[4-5] (var_8): X Position + Flags

Source: `[player_struct + 0x00]` (WORD, low 13 bits)

```
bits 0-12:  X position (13-bit, & 0x1FFF)
bit 13:     0x2000 flag (sub_453BE0 result, effect 4)
bit 14:     0x4000 flag ([esi + 0xB0] != 0)
bit 15:     0x8000 flag (arg_0 != 0)
```

### data[6-7] (var_6): Y Position + Flags

Source: `[player_struct + 0x04]` (WORD, low 13 bits)

```
bits 0-12:  Y position (13-bit, & 0x1FFF)
bit 13:     0x2000 flag (effect 26)
bit 14:     0x4000 flag (effect 27)
bit 15:     0x8000 flag (effects 5, 22, 11, 34, or 30)
```

### data[8-9] (var_4): Heading + Team

Source: `[player_struct + 0x18]` (heading, 12-bit)

```
bits 0-11:  heading angle (12-bit, 0-4095, & 0xFFF)
bit 12:     0x1000 flag ([esi + 0x1C] == 2, combat state)
bits 13-15: team ID (3-bit, (arg1 & 7) << 13)
```

### data[10] (var_2): Vertical Velocity

Source: `[player_struct + 0x24]` (signed, clamped to -512..+512)

```
bit 7:      sign (0 = up/positive, 1 = down/negative)
bits 0-6:   magnitude (value / 4, clamped to 0x7F)
```

### data[11] (var_1): Class + Team Encoding

```
bits 0-1:   class (0xC9=0/Magician, 0xCA=1/Mystic, 0xCB=2/Healer, 0xCC=3/Runemage)
bits 2-4:   ((team_byte_0x18 & 3) + 4 * (team_byte_0x14 & 3)) << 3
```

## Discrepancy: Assembly vs Wire

The IDA assembly shows heading packed into var_4 (data[8-9]), but pcap analysis conclusively shows heading in data[6-7] (wraps 0-4095 during pure rotation with no position change, all other bytes constant).

Possible explanations:
- The compiler may have reordered the stack variables differently than IDA decompiled them
- The send function `sub_432A60(&v24)` may reorder bytes before transmission
- The IDA stack frame offsets may be wrong

The pcap data is authoritative — heading IS in data[6-7] on the wire.

## C# Server Bugs

The C# server (GamePacket.cs PlayerMoveState) reads:

| C# reads as | From | Actually is |
|-------------|------|-------------|
| rawAngle (heading) | data[0-1] | walk direction (always ~0 when still) |
| Z position | data[2-3] | could be Z, needs more testing |
| X position | data[4-5] | position axis (N/S confirmed) |
| Y position | data[6-7] | HEADING (confirmed by rotation test) |

The server works because:
1. It relays raw bytes to other clients (clients decode correctly)
2. Hit detection uses projectile packet direction, not PlayerMoveState direction

## Player Struct (dword_6A0864)

```
offset 0x00:  X position (displayed as X/64 in Location debug command)
offset 0x04:  Y position (displayed as Y/64 in Location debug command)
offset 0x10:  Z height (minus origin for display)
offset 0x18:  heading (raw angle)
offset 0x1C:  walk direction
offset 0x24:  vertical velocity
```

## Verified by Pcap Tests

| Test | data[0-1] | data[2-3] | data[4-5] | data[6-7] | data[8-9] |
|------|-----------|-----------|-----------|-----------|-----------|
| Standing still | constant | constant | constant | constant | 0x0000 |
| Walk N/S | constant | constant | **changes** | constant | 0x0000 |
| Walk E/W | constant | **changes** | constant | constant | 0x0000 |
| Pure rotation | constant | constant | constant | **wraps 0-4095** | 0x0000 |
| Jumping | **changes** | constant | constant | constant | **changes** |
| Walk off ramp | **changes** | constant | **changes** | constant | 0x8200-0x8800 |

## Related Findings

### Debug Mode

- `dword_6BF810`: debug verbosity (0=off, 1=fps, 2=full, 3=stats)
- Patch at `patches/debug_verbose.json` enables full debug from startup
- Debug strings print packet names but not field values

### Earthblood / Power System

- Four HUD bars: health.bmp, fatigue.bmp, power.bmp, ley.bmp
- Node data: `dword_7F8F54/58/5C[20*nodeId]` = team/bias/power
- Power accumulator: `dword_6A082C + 136` (divided by 100 for display)
- Spatial grid: `byte_7E0DB8[X>>6<<7 + Y>>6]` maps position to nearest node ID
- Stat system: `sub_4984C0(slot)` / `sub_498400(value, ..., slot, ...)` with anti-tamper
- Known slots: 4=team, 107=HP, 118=player_port

### Score Formula (from end-of-match scoreboard)

```
score = kills + (heals / 2) + (power / 400) - deaths
```

Where power = `dword_6A082C[136] / 100`

### String Deobfuscation

Nibble swap on odd-indexed bytes (GamePacket.cs World.Deobfuscate):
```
for each byte at odd index: swapped = (b << 4 & 0xF0) | (b >> 4 & 0x0F)
```

## Open Questions

1. What do data[0-1] actually encode? Walk direction from struct+0x1C per assembly, but also Z-correlated in pcap (changes during ramp/jump). Needs more testing.
2. How is Y position (struct+0x04) packed on the wire? Our pcap shows data[6-7] is heading, but assembly says Y goes there. Compiler reordering?
3. The stat system (`sub_4984C0`) has anti-tamper checks — what are all the stat slot IDs?
4. How does the power/ley regen formula work per class? (Mage=proximity, Mystic=fraction, Healer=nexus, Runemage=contact)
5. Where is the game tick function that calculates power bar fill level?
