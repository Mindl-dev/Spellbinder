# Progression Redesign — One Level Per Match

## Problem

The 1999 exp curve requires 49+ kills to go from level 2→3. In 10-15 minute matches with ~15 kills, a new player needs 3+ matches per level. Early brackets become a slog, high brackets become ghost towns on a small server.

## Design

**One match = one level.** No exp system. No grinding. Bracket control is in the player's hands.

### Rules
- Complete a match → gain 1 level (participation, not performance)
- Character tracks `MaxLevelReached` (highest level ever attained) and `CurrentLevel` (active level)
- In tavern, chat commands:
  - `!level-down N` — set CurrentLevel to N (must be >= 1)
  - `!level-up N` — set CurrentLevel to N (must be <= MaxLevelReached)
- Spell selection happens at character select (pre-match), based on CurrentLevel
- Match brackets are 4-level ranges: 1-4, 5-8, 9-12, 13-16, 17-20, 21-25

### Benefits
- **Zero grind** — everyone levels at the same rate (1 match = 1 level)
- **Small server friendly** — high level players drop down to where the games are
- **No dead brackets** — players self-sort via level-down
- **Experimentation** — reached level 20? Try a level 5 build anytime
- **Still has progression** — can't access level 20 spells until you've played 20 matches

### What gets removed
- Kill exp calculation (`Arena.cs:2847`)
- Healing exp (`Arena.cs:2383`)
- Shrine/pool bias exp (`Arena.cs:2975`, `Arena.cs:2985`, `Arena.cs:3064`)
- Death exp penalty (`Arena.cs:2700`, `Arena.cs:3118`)
- `LevelExp` table (`Character.cs:224`)
- `ExpMultiplier` server setting
- `GivePlayerExperience` method
- `UpdateExperience` outgoing packet (or repurpose for match stats)

### What gets added
- `MaxLevelReached` field on Character (DB column)
- `CurrentLevel` field (already exists, just decoupled from exp)
- Match completion handler: increment level on match end
- Two chat commands: `!level-down`, `!level-up`

### Implementation

**Character.cs:**
```csharp
public Byte MaxLevelReached;  // new field, persisted to DB

// On match end:
if (CurrentLevel < MaxLevel)
{
    Level++;
    MaxLevelReached = Math.Max(MaxLevelReached, Level);
}

// Chat commands:
// !level-down 5 → Level = 5 (if >= 1)
// !level-up 12  → Level = 12 (if <= MaxLevelReached)
```

**DB migration:**
```sql
ALTER TABLE characters ADD COLUMN max_level_reached TINYINT DEFAULT 1;
UPDATE characters SET max_level_reached = level;
```

### Open Questions
- Should match completion require minimum time played (prevent join-quit farming)?
- Should losing team also level? (Yes — removes tilt, keeps everyone progressing)
- Cap at 25 or extend? 25 matches = ~6-8 hours total. Reasonable for full unlock.
- Does the client need to display MaxLevelReached anywhere, or just CurrentLevel?
