# Pillar Collision Fix (Phase 2)

## Problem

Projectiles fly through pillars/earthnodes. The GridObject bounding box check only runs when `CollisionHeightDetection` fails (returns non-zero), but pillars sit in grid cells with normal floor/ceiling heights, so height detection returns 0 and the GridObject check is skipped.

## Fix

In `SpellServer/Arena/Arena.cs`, in `CollisionClassifier()`, add a standalone GridObject check **before the player loop** (currently ~line 1211). This goes right after the `FloorCeilingCollision == 2` block (return 7) and before the `for (Int32 k = ArenaPlayers.Count - 1 ...` loop:

```csharp
                    // Phase 2: Check GridObject collision independent of height detection
                    // Pillars/fixtures have normal floor/ceiling so height checks pass,
                    // but we still need to test their bounding boxes
                    GridObject standAloneObj = grid.GridObjects.GetObjectByLocation(nX, nY, grid);
                    if (standAloneObj != null)
                    {
                        OrientedBoundingBox testProjectileBox = new OrientedBoundingBox(
                            newPos, projectile.BoundingBox.Size, projectile.BoundingBox.Rotation);
                        if (standAloneObj.ContainerBox.Collides(testProjectileBox))
                        {
                            projectile.hitBlock = grid.GridBlocks.GetBlockByLocation(nX, nY);
                            return 9;
                        }
                    }
```

## Where exactly

```
    ...
    if (FloorCeilingCollision == 2)
    {
        // ... existing ceiling check + GridObject check ...
        return 7;
    }

    >>> INSERT HERE <<<

    for (Int32 k = ArenaPlayers.Count - 1; k >= 0; k--)
    {
        ArenaPlayer arenaPlayer = ArenaPlayers[k];
    ...
```

## Test

1. Build with `./dev.sh` (runs on ports 10611/10612/10613, won't touch prod)
2. Connect a client pointed at localhost:10612
3. Enter Keep or Ruins — fire projectiles directly through a pillar/earthnode
4. With `--debug=ProjectileTracking`, you should see `type=9` hits on pillars that previously showed no collision

## Why return 9

Type 9 is already used for GridObject bounding box hits and `SpecialCollision`. It's terminal (no bounce) which is correct — projectiles should stop on pillars, not bounce off them.

## Risk

Low. This only adds a new check; all existing collision paths are untouched. Worst case: projectiles stop on pillars they previously flew through (which is the intended fix).
