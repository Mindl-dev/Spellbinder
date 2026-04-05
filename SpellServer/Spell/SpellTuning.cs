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
                // Healing: potency IS base HP healed, level adds scaling
                // JSON effects (potency < 1000): direct HP value
                // Spells.dat effects (potency >= 1000): divide by 100 to get HP
                //   Heal I=2000→20, Heal II=3000→30, Heal III/IV=4000→40
                case SpellEffectType.Healing:
                    float baseHeal = potency >= 1000 ? potency / 100f : potency;
                    return baseHeal + casterLevel * 0.5f;

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
        /// Damage multiplier for projectile spells. Adjusts base dice damage to hit
        /// target TTK at realistic accuracy rates.
        /// Default 1.0 = use Spells.dat values as-is.
        /// </summary>
        /// <summary>
        /// Global projectile damage multiplier. Spells.dat base values assume 100% accuracy;
        /// real accuracy is ~13-25% for vel 600 projectiles. This scales damage so TTK at
        /// realistic accuracy matches the arena shooter sweet spot (5-10s focused fire).
        /// </summary>
        public const float ProjectileDamageMultiplier = 1.2f;

        /// <summary>
        /// When true, projectiles deal fixed average damage (no dice variance).
        /// Consistent TTK — competitive/esport style.
        /// When false, projectiles roll dice per Spells.dat (original SpellBinder behavior).
        /// </summary>
        public static readonly bool FixedProjectileDamage = true;

        public const float HitscanDamageMultiplier = 0.95f;
        public static float GetDamageMultiplier(Spell spell)
        {
            if (spell == null) return 1.0f;
            if (spell.Type != SpellType.Projectile) return 1.0f;
            return spell.Velocity >= 2000 ? HitscanDamageMultiplier : ProjectileDamageMultiplier;
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
