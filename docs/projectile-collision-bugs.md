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

### Root Cause
`GetFloorHeight()` returns the geometric floor height (accounting for sub-pixel mesh data, slope, and pillar geometry) for the entire 64-unit grid cell. Pillar outcroppings raise the geometric floor to ~704 for the whole cell, even though the pillar only occupies part of the cell. A projectile flying at Z=528 through the open hallway enters a cell where `floor=704`, triggering `oldZ < floor - MaxStep` → collision type 2.

### Where in the Code
- **`Arena.cs CollisionClassifier()`** ~line 1095: calls `CollisionHeightDetection()` for X-axis sweep
- **`Arena.cs CollisionHeightDetection()`** ~line 1390: the `oldZ < floor` check at the bottom of the function (after the ceiling checks)
- **`Grid.cs GetFloorHeight()`** line 1060: computes geometric floor from `block.FloorZ` + mesh height + sub-pixel library + slope data

### Log Signature
```
hit type=2 ... detail=axis=X_leading_lateral (type2) height=oldZ<floor-maxStep oldZ=528 floor=704 maxStep=0 (ret1)
```

### Possible Fixes
1. **GridObject bounding box only**: For cells containing pillar geometry, skip the grid floor height check and only use the GridObject bounding box test (Phase 2 check). The bounding box is accurate to the pillar's actual shape.
2. **Check if projectile was already above the floor**: If the projectile's origin cell had a lower floor and it's flying level (not descending), it shouldn't collide with a raised floor in an adjacent cell that it's clearly above.
3. **Sub-cell collision**: Instead of using `GetFloorHeight(cellX, cellY)`, check the floor height at the projectile's exact X,Y position. This would correctly return a low floor for the open space between pillars.

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
