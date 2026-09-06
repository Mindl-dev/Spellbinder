# Projectile Collision Bugs

## Refactor Plan: CollisionClassifier

`CollisionClassifier` is ~1000 lines of nested ifs doing broad phase, narrow phase, and resolution in one function. Refactoring into separate methods fixes the pillar bug for free and makes each collision type testable.

### Target Architecture

**1. Sweep the ray segment** — collect all grid cells the projectile passes through this tick. Extract the `leadingX/Y`, `nX/Y` calculation into a method returning a list of cells.

**2. For each cell, classify the obstacle** — each check becomes its own method:
```csharp
CheckPlayerCollision(projectile, cell)      → type 5 or null
CheckGridObjectCollision(projectile, cell)  → type 9 or null
CheckWallCollision(projectile, cell)        → type 8 or null
CheckHeightCollision(projectile, cell)      → type 2/3/6/7 or null
```

**3. Take the first hit** — closest collision along the ray wins.

### What We Get For Free
- **Pillar bug fixed** — `CheckGridObjectCollision` runs before `CheckHeightCollision`, so the bounding box catches the pillar and the phantom floor never triggers
- **Each check is independently testable** — mock a cell, pass a projectile, verify result
- **Readable** — each method is ~20 lines, not 1000
- **New collision types are easy** — add a method, slot it into the priority order
- **Bounce/AOE logic stays separate** — `CollisionClassifier` just returns what was hit, the caller decides what to do

### Migration Path
1. Extract one collision type at a time (start with player hit — type 5)
2. Keep existing function working, delegate to new method
3. Verify with existing tests + projectile tracking logs
4. Repeat for each type until the nested ifs are gone

---

## Bug 1: Phantom Wall Collision in Pillar Hallways

### Symptom
Projectiles stop mid-air in hallways with pillar outcroppings on the sides. The spell visually appears to fly through open space on the client, but the server kills it at a grid cell boundary.

### Root Cause (Verified 2026-03-23)
**The data is correct. The collision sampling is wrong.**

`GetFloorHeight()` returns the geometric floor at a single (x,y) point. For pillar cells, the floor mesh height varies across the 8×8 sub-cell grid — 224 units in the pillar center, 0 at the corners:

```
BlockType 22 (EastAndNorthCurvedRamp) FloorMeshHeight 8×8:
  0    0  224  224  224  224    0    0
  0  224  224  224  224  224  224    0
224  224  224  224  224  224  224  224
224  224  224  224  224  224  224  224
224  224  224  224  224  224  224  224
224  224  224  224  224  224  224  224
  0  224  224  224  224  224  224    0
  0    0  224  224  224  224    0    0
```

This is a cylindrical pillar shape. `FloorZ(480) + 224 = 704` — exactly the phantom floor from logs.

The collision system calls `GetFloorHeight(leadingX, leadingY)` where `leadingX/Y` is the projectile's next position. If that point lands in a 224-height sub-cell, it reads `floor=704` and triggers `oldZ(528) < floor(704)` → wall collision. But the projectile ray might actually pass through the 0-height corners and clear the pillar entirely.

**The problem is point-sampling vs ray-testing.** The current code tests a single point against the mesh height. A projectile flying through a corner of the cell should pass through the zero-height region, but if the sampled point is inside the raised mesh, it falsely collides.

### Data Sources
- `GEOMETRY.DAT`: floor/ceiling mesh height tables (130 bytes per BlockType: 64×int16 floor mesh + int16 SlopeProperty + 64×int16 ceiling mesh)
- `SUBPIXEL.DAT`: per-cell 64×64 height detail maps (indexed by `DetailMapIndex` for floor, `LogicFlag` for ceiling)
- `allgriddata.bin`: 19 bytes per cell — TileId, FloorZ, WallHeight, CeilingZ, BlockType, flags, indices

### Where in the Code
- **`Arena.cs CollisionClassifier()`** ~line 1095: calls `CollisionHeightDetection()` for X-axis sweep
- **`Arena.cs CollisionHeightDetection()`** ~line 1390: the `oldZ < floor` check
- **`Grid.cs GetFloorHeight()`** line 1068: computes `FloorZ + SubPixelLibrary[DetailMapIndex] + FloorMeshHeight[BlockType]`
- **`Grid.cs GetFloorMeshHeight()`** line 874: reads from GEOMETRY.DAT terrain table

### Log Signature
```
hit type=2 ... detail=axis=X_leading_lateral (type2) height=oldZ<floor-maxStep oldZ=528 floor=704 maxStep=0 (ret1)
```

### Possible Fixes
1. **Ray vs mesh test**: Instead of sampling a single point, test the ray segment against the mesh heightfield. Sample multiple points along the ray within the cell, or analytically intersect the ray with the mesh surface. Only collide if the ray actually passes through raised geometry.
2. **Sample at entry point**: Instead of testing `leadingX/Y` (exit edge), test where the ray enters the cell. If the entry point has mesh height 0 (corner), the projectile clears the pillar.
3. **GridObject bounding box priority**: For cells with non-zero `GetFloorMeshHeight`, skip the heightfield check and use the GridObject bounding box test instead. The bounding box is a better approximation for isolated objects like pillars.
4. **Minimum-height ray test**: Sample `GetFloorHeight` at both the entry and exit points of the ray within the cell. Only collide if BOTH points have raised floors (the ray can't avoid the geometry).

### Also Fixed (Partial)
- **Phantom ceiling collision**: `oldZ >= gridCeil` triggered for cells with low geometric ceilings (pillar tops) even when the projectile was flying level in open air. Fixed by skipping when `oldZ == newZ` (level flight). See the `SKIPPED (flying level)` path.
- **Zero span collision**: `HighBoxZ == CeilingZ` caused `Tall > span` (any Tall > 0) to always collide. Fixed by adding `&& block.HighBoxZ != block.CeilingZ` guard.

---

## Bug 2: Reflective Ice Can't Bounce

### Symptom
Reflective Ice II shows bouncing visuals on the client but the server doesn't bounce the projectile. It hits a wall (type 2 or 3) and dies.

### Root Cause
Reflective Ice II has no `bounce` or `max_bounces` fields in `Spells.dat`. The spell loader defaults these to 0. The bounce check at `Arena.cs` line ~1428:
```csharp
if (projectile.Bounce > 0 && HandleBounce(...) != 0)
```
...is always false because `Bounce == 0`.

### Where in the Code
- **`Arena.cs UpdateProjectileState()`** ~line 1428: bounce check entry point
- **`Arena.cs HandleBounce()`** ~line 1536: the actual bounce logic (reflection, direction change)
- **`Spells.dat`** `[spell16]` (Reflective Ice II): no `bounce=` or `max_bounces=` entries
- **`SpellManager.cs`**: loads `BOUNCE` and `MAX_BOUNCES` from spell data, defaults to 0

### What Needs Investigation
- Does the original game have bounce values in its spell data that our Spells.dat is missing?
- Check the original `spell70.exe` demo installer's spell data for bounce fields
- The client bounces the projectile visually — it must have the bounce data somewhere. Could be hardcoded in the client for specific spell IDs, or in a different data file.

### Temporary Fix
Add `bounce=1` and `max_bounces=3` (or similar) to Reflective Ice entries in Spells.dat manually.

---

## Bug 3: Fire DoT Doesn't Tick Damage

### Symptom
Fire Ball II applies a burning visual effect on hit, but no damage ticks from the DoT.

### Root Cause
The effect tick loop in `Arena.cs` ~line 613 only handles `SpellEffectType.Bleed`:
```csharp
switch (arenaEffect.EffectSpell.Effect)
{
    case SpellEffectType.Bleed:
        if (hasElapsed) DoPlayerDamage(...);
        break;
    // No other damage effect types handled!
}
```

The `SpellEffectType` enum has 20 entries but several are `Empty1/2/3/5` — likely placeholders for Burn, Poison, etc. that were never implemented server-side.

### Where in the Code
- **`Arena.cs ProcessMisc()`** ~line 613: effect tick loop with the switch
- **`Spell.cs SpellEffectType`** line 6: enum definition (Bleed=5, missing Burn/Poison)
- **`Arena.cs DoPlayerEffect()`** ~line 2204: where effects are applied to players

### What Needs Investigation
- Which `SpellEffectType` values correspond to Burn, Poison, etc.
- Check the original game's effect handling in IDA for the full effect type list
- The `Empty1/2/3/5` slots in the enum might be the missing damage types
- Fire Ball II's `death_spell_effect` might reference a spell with `effect=` set to one of these types

---

## Debug Tooling Added

### Projectile Tracking (`--debug=ProjectileTracking`)
Every collision now logs:
```
[Projectile] {caster} spell={name} hit type={N} at ({x},{y},{z}) player={name} wall={id} block=({bx},{by}) flags={f} pillar={bool} floor={f} ceil={c} highZ={h} detail={why}
```

The `detail` string traces exactly which code path triggered the collision, including:
- Height detection results with all values (oldZ, newZ, gridCeil, gridFloor, blockCeil, wallH)
- Which axis sweep (X, Y, diagonal) detected it
- GridObject hits with object IDs
- Player hits with target names

### Player Tracking (`--debug=PlayerTracking`)
```
[Move] {name} pos=({x},{y},{z}) dir={radians} spd={speed}
```
