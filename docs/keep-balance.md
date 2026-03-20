# Kaelgard Keep (grid00) — Balance Notes

## Known Issue
Gryphon/Order (yellow) has a node that's too exposed near center of map, making their shrine easier to attack. Community feedback says this team consistently loses on Keep.

## Team Mapping
| Game Team | Magestorm Name | Color | WORLD.DAT alignment |
|-----------|---------------|-------|-------------------|
| Dragon | Chaos | Red | chaos |
| Gryphon | Order | Yellow | order |
| Phoenix | Balance | Blue | balance |

## Shrine Positions (from NIFS.DAT fixtures)
| Team | Shrine | Fixture | Position (x, y, z) |
|------|--------|---------|-------------------|
| Dragon/Chaos | shrine02 | fixture10 | (1824, 4064, 8) — far west |
| Gryphon/Order | shrine01 | fixture12 | (3936, 2336, 8) — north of center |
| Phoenix/Balance | shrine00 | fixture11 | (6048, 4064, 8) — far east |

## Earthnode Positions
| Earthblood | Fixture | Position (x, y, z) | Linked Shrine | Notes |
|-----------|---------|-------------------|--------------|-------|
| earthblood00 | fixture13 | (2112, 3072, 260) | Chaos | |
| earthblood01 | fixture04 | (3648, 3456, 196) | **Order** | **Exposed — near map center** |
| earthblood02 | fixture06 | (4864, 3008, 132) | Order | East side |
| earthblood03 | fixture09 | (5888, 3072, 260) | Balance | |
| earthblood04 | fixture05 | (3936, 5280, 132) | (commented out) | Disabled in WORLD.DAT |
| earthblood05 | fixture01 | (3072, 2624, 420) | Order | Northwest, elevated |
| earthblood06 | fixture07 | (5120, 4032, 164) | Balance | |
| earthblood07 | fixture02 | (2816, 4032, 164) | Chaos | |
| earthblood08 | fixture03 | (3136, 5056, 4) | Chaos | |
| earthblood09 | fixture08 | (4800, 5056, 4) | Balance | |

## Shrine → Node Links (from WORLD.DAT)
```
shrine00 (Balance/Phoenix): earthblood 3, 6, 9
shrine01 (Order/Gryphon):   earthblood 5, 1, 2   ← node 1 is the problem
shrine02 (Chaos/Dragon):    earthblood 0, 7, 8
```

## How to Fix
Earthnode positions are server-side only. Edit fixture x/y in NIFS.DAT, restart server. Client renders nodes wherever the server says.

**File to edit:** `Content/Grids/grid00/NIFS/NIFS.DAT`

**Target:** fixture04 (earthblood01) — currently at (3648, 3456). Move it into a more defensible room north of its current position, closer to the Gryphon shrine.

**Steps:**
1. Edit `fixture04` x= and y= values in NIFS.DAT
2. Restart server
3. Test in-game — node should appear at new position
4. No client patch needed

## Map Reference
- Map bounds: (640, 640) to (6656, 6656) — from `[map]` section
- Grid center: ~3648, 3648
- Each grid tile = 64 world units (world coord >> 6 = grid coord)
