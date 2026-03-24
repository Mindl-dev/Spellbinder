using Helper;
using Helper.Timing;

namespace SpellServer
{
    public enum EffectType
    {
        Default,
        Death,
        Caster,
        Target,
        Area,
        AuraCaster,
        AuraTarget,
    }

    public class Effect
    {
        public readonly ArenaPlayer Owner;
        public readonly Interval Duration;
        public readonly Spell OwnerSpell;
        public readonly Spell EffectSpell;

        public Effect(Spell spell, ArenaPlayer caster, EffectType effectType)
        {
            Owner = caster;

            switch (spell.Type)
            {
                case SpellType.Projectile:
                {
                    switch (effectType)
                    {
                        case EffectType.Death:
                        {
                            OwnerSpell = spell;
                            EffectSpell = SpellManager.Spells[spell.DeathSpellEffect];
                            break;
                        }
                        case EffectType.Area:
                        {
                            Spell areaSpell = SpellManager.Spells[spell.AreaEffectSpell];
                            OwnerSpell = areaSpell;
                            EffectSpell = SpellManager.Spells[areaSpell.TargetSpellEffect];
                            break;
                        }
                    }
                    break;
                }
                case SpellType.Rune:
                {
                    switch (effectType)
                    {
                        case EffectType.Death:
                        {
                            OwnerSpell = spell;
                            EffectSpell = SpellManager.Spells[spell.DeathSpellEffect];
                            break;
                        }
                        case EffectType.AuraCaster:
                        {
                            Spell auraCasterSpell = SpellManager.Spells[spell.AuraCasterEffect];
                            OwnerSpell = auraCasterSpell;
                            EffectSpell = SpellManager.Spells[auraCasterSpell.TargetSpellEffect];
                            break;
                        }
                        case EffectType.AuraTarget:
                        {
                            Spell auraTargetSpell = SpellManager.Spells[spell.AuraCasterEffect];
                            OwnerSpell = auraTargetSpell;
                            EffectSpell = SpellManager.Spells[auraTargetSpell.TargetSpellEffect];
                            break;
                        }
                    }

                    break;
                }
                case SpellType.Shield:
                {
                    OwnerSpell = spell;

                    switch (effectType)
                    {
                        case EffectType.Caster:
                        {
                            EffectSpell = SpellManager.Spells[spell.CasterSpellEffect];
                            break;
                        }
                        case EffectType.Target:
                        {
                            EffectSpell = SpellManager.Spells[spell.TargetSpellEffect];
                            break;
                        }
                    }
                    break;
                }                
                default:
                {
                    OwnerSpell = spell;
                    EffectSpell = spell;
                    break;
                }
            }
           
            if (EffectSpell != null)
            {
                // Duration field is in seconds. Ticking effects (Bleed, Healing) tick every 1s.
                // Non-ticking effects (buffs, shields) use a one-shot timer.
                if (EffectSpell.Effect == SpellEffectType.Bleed || EffectSpell.Effect == SpellEffectType.Healing)
                    Duration = new Interval(1000, EffectSpell.Duration);  // tick every 1s, Duration ticks
                else
                    Duration = new Interval(EffectSpell.Duration * 1000, false);  // one-shot: Duration seconds
            }
            else
            {
                Duration = new Interval(1, false);
            }
        }
    }
}
