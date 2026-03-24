using System;

namespace SpellServer
{
    /// <summary>
    /// Central tuning functions for all spell effects.
    /// Maps raw spell data (potency, duration) + player state (level) into actual game values.
    ///
    /// All balance tuning lives here. One place to see all scaling.
    ///
    /// The fundamental unit is BP (bias power). Every effect's value can be expressed
    /// as BP enabled or denied over its duration.
    /// </summary>
    public static class SpellTuning
    {
        /// <summary>
        /// Compute the per-tick or per-application value of a spell effect.
        /// Returns the value in the natural unit for that type (HP, %, multiplier).
        /// </summary>
        public static float ComputeEffectValue(SpellEffectType type, int potency, int casterLevel)
        {
            switch (type)
            {
                // Healing: potency/100 gives base HP, level adds scaling
                // Heal I (potency=2000, lv1): 20 + 0.5 = 20.5 HP per tick
                // Heal IV (potency=4000, lv10): 40 + 5 = 45 HP per tick
                case SpellEffectType.Healing:
                    return potency / 100f + casterLevel * 0.5f;

                // Bleed/DoT: potency is raw damage per tick, level adds a fraction
                case SpellEffectType.Bleed:
                    return potency + casterLevel / 3f;

                // Resist: potency is the % damage reduction (same element)
                // Half effectiveness for different element (handled in DoPlayerDamage)
                case SpellEffectType.Resist:
                    return potency;

                // Bless: potency is base % reduction, level adds scaling
                // Bless I (potency=5, lv1): 5.3% → Bless IV (potency=20, lv10): 23%
                case SpellEffectType.Bless:
                    return potency + casterLevel * 0.3f;

                // Prayer: same as Bless (different buff slot, stacks separately)
                case SpellEffectType.Prayer:
                    return potency + casterLevel * 0.3f;

                // Speed: potency/100 = speed multiplier
                // Haste I (potency=10000): 100% = 2x speed? or 100 = base speed?
                case SpellEffectType.Speed:
                    return potency / 100f;

                // HealingReduction: potency is % healing blocked
                case SpellEffectType.HealingReduction:
                    return potency;

                // Resurrect: potency is % HP restored on res
                case SpellEffectType.Resurrect:
                    return potency;

                // Hinder: potency is % speed reduction
                case SpellEffectType.Hinder:
                    return potency;

                // Movement: Leaping/Levitate/Fly — potency is magnitude
                case SpellEffectType.Leaping:
                case SpellEffectType.Levitate:
                case SpellEffectType.Fly:
                    return potency;

                // Presence/Light: visual/utility effects — potency unused
                case SpellEffectType.Presence:
                case SpellEffectType.Light:
                    return potency;

                default:
                    return potency;
            }
        }

        /// <summary>
        /// Compute the effect's priority for stacking.
        /// Higher priority overwrites lower. Same priority = no overwrite.
        /// </summary>
        public static int ComputeStackPriority(SpellEffectType type, int potency, int casterLevel)
        {
            // Simple: potency IS priority. Stronger effect overwrites weaker.
            return (int)ComputeEffectValue(type, potency, casterLevel);
        }

        /// <summary>
        /// Read the potency value from a spell.
        /// JSON-loaded spells set Potency directly.
        /// Spells.dat-loaded spells have "effect=" cast to SpellEffectType (losing the raw int)
        /// and "level=" which is 0 for most effects. Falls back to (int)Effect for dat-loaded.
        /// </summary>
        public static int GetPotency(Spell spell)
        {
            if (spell.Potency > 0)
                return spell.Potency;
            if (spell.Level > 0)
                return spell.Level;
            return (int)spell.Effect;
        }
    }
}
