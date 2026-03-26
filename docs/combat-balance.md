# Combat Balance Reference

All values extracted from Spells.dat and Character.cs. No guesswork.

## HP by Class and Level

Formula: `HP = baseHp + floor(baseHp * scaler * (Constitution - 50) * 0.5)`

| Class | Per Level | Base | Scaler | Lv1 | Lv5 | Lv10 | Lv15 | Lv20 | Lv25 |
|-------|-----------|------|--------|-----|-----|------|------|------|------|
| Magician | +4 | 19 | 0.0085 | 19 | 36 | 57 | 78 | 99 | 119 |
| Runemage | +6 | 19 | 0.01225 | 20 | 45 | 77 | 109 | 141 | 172 |
| Mystic | +5 | 20 | 0.01175 | 21 | 42 | 68 | 95 | 121 | 148 |
| Healer | +5 | 19 | 0.0088 | 19 | 40 | 66 | 92 | 119 | 145 |

(Assuming Constitution = 60)

Source: `Character.cs:82-119`

## Projectile Velocities

Only 3 speed tiers exist:

| Velocity | Type | Spells | Notes |
|----------|------|--------|-------|
| 400 | Slow, arcing | Cold Balls, Tornado, Whirlwind | Gravity=1 on some |
| 480 | Slow, bouncing | Fire Orb I, Fire Orb II | Bounce=5-6 |
| 600 | Standard | ~65 spells — all Flame Streaks, Fire Balls, Ice, Lava, Arcane, Sunfires, etc. | The core combat speed |
| 2000 | Hitscan-like | Cramp, Fracture, Bleeds, Blinds, Mind spells, Misery, Vacuum | Debuff/CC spells |

Source: `Spells.dat` velocity field

## Spell Damage (Selected)

Damage = `num_dice * rand(1, dice) + base`. Average = `num_dice * (dice+1)/2 + base`.

### Starter Spells (Level 1-3)
| Spell | Avg Dmg | Min | Max | Vel | Radius | Notes |
|-------|---------|-----|-----|-----|--------|-------|
| Flame Streak I | 6.0 | 3 | 9 | 600 | 0 | Basic single target |
| Ice Shards I | 6.0 | 3 | 9 | 600 | 0 | |
| Light Eruption I | 5.0 | 2 | 8 | 600 | 0 | |
| Arcane Star | 5.0 | 3 | 7 | 600 | 0 | |
| Wounding | 6.0 | 3 | 9 | 600 | 0 | |

### Mid Spells (Level 5-10)
| Spell | Avg Dmg | Min | Max | Vel | Radius | Notes |
|-------|---------|-----|-----|-----|--------|-------|
| Flame Streak III | 24.0 | 12 | 36 | 600 | 0 | |
| Lesser Fire Ball | 23.0 | 11 | 35 | 600 | 96 | AOE |
| Ice Axe | 13.0 | 4 | 22 | 600 | 0 | High variance |
| Cold Ball | 28.0 | 14 | 42 | 400 | 128 | Slow + AOE |
| Magma | 28.0 | 12 | 44 | 600 | 0 | High variance |
| Lava Ball | 36.0 | 16 | 56 | 600 | 96 | AOE |
| Photonic Burst | 13.5 | 6 | 21 | 600 | 0 | |
| Clerical Wrath | 21.5 | 11 | 32 | 600 | 0 | Healer DPS |

### High Spells (Level 12-20)
| Spell | Avg Dmg | Min | Max | Vel | Radius | Notes |
|-------|---------|-----|-----|-----|--------|-------|
| Flame Streak IV | 35.0 | 17 | 53 | 600 | 0 | |
| Fire Ball | 32.0 | 14 | 50 | 600 | 96 | AOE |
| Fire Ball II | 43.0 | 18 | 68 | 600 | 128 | Large AOE |
| Sunfires | 27.0 | 13 | 41 | 600 | 0 | |
| Void Ball | 25.0 | 13 | 37 | 600 | 96 | AOE |
| Misery | 28.0 | 12 | 44 | 2000 | 0 | Hitscan |
| Torture I | 22.0 | 8 | 36 | 2000 | 0 | Hitscan |

### Top Spells (Level 18-25)
| Spell | Avg Dmg | Min | Max | Vel | Radius | Notes |
|-------|---------|-----|-----|-----|--------|-------|
| Greater Fire Ball | 49.0 | 19 | 79 | 600 | 128 | |
| Greater Cold Ball | 51.0 | 21 | 81 | 400 | 128 | Slow |
| Greater Ice Ball | 43.0 | 18 | 68 | 600 | 128 | |
| Greater Lava Ball | 48.0 | 20 | 76 | 600 | 128 | |
| Greater Misery | 36.0 | 14 | 58 | 2000 | 0 | Hitscan |
| Sunfires II | 31.0 | 13 | 49 | 600 | 0 | |
| Greater Void Ball | 35.0 | 15 | 55 | 600 | 128 | |

### Debuff/CC Spells (hitscan, vel 2000)
| Spell | Avg Dmg | Effect | Notes |
|-------|---------|--------|-------|
| Cramp | 2.0 | Hinder (slow) | Utility, not damage |
| Fracture | 2.5 | Hinder (slow) | Slightly stronger slow |
| Lesser Bleeding | 2.0 | Bleed DOT | Ticks over time |
| Minor Bleeding | 3.0 | Bleed DOT | |
| Bleeding | 4.5 | Bleed DOT | |
| Major Bleeding | 7.0 | Bleed DOT | |
| Blind I/II/III | 2.0 | Blind | |
| Mind Shear | 6.0 | Mind DOT | |
| Mind Erode | 14.5 | Mind DOT | |
| Lesser Paralyze | 3.5 | Paralyze | |

## TTK Analysis

### Theoretical TTK at 100% accuracy (1 cast/sec for vel 600, 0.5s for vel 2000)

**Target: Level 10 Magician (57 HP)**

| Spell | Avg Dmg | Hits to Kill | TTK@100% |
|-------|---------|-------------|----------|
| Flame Streak I | 6.0 | 10 | 10.0s |
| Flame Streak IV | 35.0 | 2 | 2.0s |
| Fire Ball (AOE) | 32.0 | 2 | 2.0s |
| Greater Fire Ball | 49.0 | 2 | 2.0s |
| Lava Ball (AOE) | 36.0 | 2 | 2.0s |
| Misery (hitscan) | 28.0 | 3 | 1.5s |
| Greater Misery | 36.0 | 2 | 1.0s |

### Realistic TTK with accuracy (research-based estimates)

Accuracy estimates for vel 600 projectiles in hallway combat:
- Average player: ~15-20%
- Good player: ~25-30%
- Pro: ~35-40%

| Spell | TTK@15% | TTK@25% | TTK@35% |
|-------|---------|---------|---------|
| Flame Streak I | 66.7s | 40.0s | 28.6s |
| Flame Streak IV | 13.3s | 8.0s | 5.7s |
| Fire Ball | 13.3s | 8.0s | 5.7s |
| Greater Fire Ball | 13.3s | 8.0s | 5.7s |
| Misery (hitscan) | 10.0s | 6.0s | 4.3s |

### Design Implications

1. **Starter spells (Flame Streak I) are insufficient for kills** — 40-67s realistic TTK means level 1 players can barely kill anyone solo. This is intentional: forces teamwork at low levels.

2. **Mid-tier spells (Flame Streak III, Fire Ball) hit the sweet spot** — 8-13s realistic TTK aligns with arena shooter research (3-10s for focused fire with counterplay).

3. **Top-tier spells are 2-shot kills** — at 100% accuracy, Greater Fire Ball kills a Magician in 2 hits. At realistic accuracy this is still 5-8s, which is fast but requires skill.

4. **Hitscan debuffs barely do damage** — Cramp at 2 avg damage would take 29 hits to kill. They're utility, not DPS.

5. **AOE radius matters** — Fire Ball (radius 96) can hit targets that direct shots miss. Effective accuracy is higher than the numbers suggest for AOE spells.

## Damage Mitigation (spell_effects.json)

| Effect | Potency | DR% | EHP Multiplier |
|--------|---------|-----|---------------|
| Bless I | 5 | 5% | 1.05x |
| Bless II | 10 | 10% | 1.11x |
| Bless III | 15 | 15% | 1.18x |
| Bless IV | 20 | 20% | 1.25x |
| Prayer I | 5 | 5% | 1.05x |
| Prayer II | 10 | 10% | 1.11x |
| Prayer III | 15 | 15% | 1.18x |
| Prayer IV | 20 | 20% | 1.25x |
| Resist (elemental) | 15-25 | 15-25% vs element | 1.18-1.33x |

**Max stack (Bless IV + Prayer IV):** 40% DR = 1.67x EHP. A level 10 Magician goes from 57 to ~95 effective HP. TTK roughly doubles.

**With healing:** A healer keeping someone alive adds ~10-15 HP/tick. Against 32 avg damage per hit at 25% accuracy = 8 DPS, healing at 10/sec = barely surviving. Two attackers overwhelm the healing.

## XP System

### Kill XP Formula
```
experience = 75 + (victimLevel * 14) + Max(0, (killerLevel - victimLevel) * 18)
```
Applied with server ExpMultiplier (currently 15x, target 10x).

### Death Penalty (per game manual)
- Death: -10% of session combat+objective XP (levels 1-2 exempt)
- Node/nexus res: -20% additional (total 30% from death + res)
- Healer Spirit Gate res: no additional penalty

### Bonus XP (end of match)
- 40% of combat+objective XP
- Prorated by time played (secondsPlayed / matchDuration)
- 1.5x multiplier for winning team
- Awarded to ALL players, not just winners
