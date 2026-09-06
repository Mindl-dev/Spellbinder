using System;
using System.IO;
using NUnit.Framework;
using SpellServer;
using Helper;

namespace SpellServer.Tests
{
    [TestFixture]
    public class SpellManagerTests
    {
        /// <summary>
        /// Verify that SpellManager.Spells[id] returns the correct spell after loading.
        /// This catches the Insert-shifts-indices bug where every spell ends up at the wrong index.
        /// </summary>
        [Test]
        public void LoadSpells_LookupById_ReturnsCorrectSpell()
        {
            // LoadSpells reads from Build/Debug/Spells.dat — need to find it
            string origDir = Directory.GetCurrentDirectory();
            string dir = TestContext.CurrentContext.TestDirectory ?? Directory.GetCurrentDirectory();
            for (int i = 0; i < 6; i++)
            {
                if (dir == null) break;
                string candidate = Path.Combine(dir, "Build", "Debug");
                if (File.Exists(Path.Combine(candidate, "Spells.dat")))
                {
                    Directory.SetCurrentDirectory(candidate);
                    break;
                }
                candidate = Path.Combine(dir, "Content");
                if (File.Exists(Path.Combine(candidate, "Spells.dat")))
                {
                    Directory.SetCurrentDirectory(candidate);
                    break;
                }
                dir = Path.GetDirectoryName(dir);
            }

            try
            {
                Assume.That(File.Exists("Spells.dat"), "Spells.dat not found");

                SpellManager.LoadSpells();

                // Flame Streak I = spell 1
                Assert.IsNotNull(SpellManager.Spells[1], "Spell 1 should exist");
                Assert.AreEqual("Flame Streak I", SpellManager.Spells[1].Name);

                // Cure = spell 111
                Assert.IsNotNull(SpellManager.Spells[111], "Spell 111 (Cure) should exist");
                Assert.AreEqual("Cure", SpellManager.Spells[111].Name);

                // Haste I = spell 166
                Assert.IsNotNull(SpellManager.Spells[166], "Spell 166 (Haste I) should exist");
                Assert.AreEqual("Haste I", SpellManager.Spells[166].Name);

                // Transfer II = spell 170
                Assert.IsNotNull(SpellManager.Spells[170], "Spell 170 (Transfer II) should exist");
                Assert.AreEqual("Transfer II", SpellManager.Spells[170].Name);

                // Fire Ball II = spell 124
                Assert.IsNotNull(SpellManager.Spells[124], "Spell 124 (Fire Ball II) should exist");
                Assert.AreEqual("Fire Ball II", SpellManager.Spells[124].Name);

                // Gaps should be null
                Assert.IsNull(SpellManager.Spells[5], "Spell 5 has no [spell05] section, should be null");

                // Verify spell fields populated correctly
                var flameStreak = SpellManager.Spells[1];
                Assert.AreEqual(SpellType.Projectile, flameStreak.Type);
                Assert.IsTrue(flameStreak.DamageNumDice > 0, "Flame Streak should have damage dice");
                Assert.IsTrue(flameStreak.DamageDice > 0, "Flame Streak should have dice size");
                Assert.IsTrue(flameStreak.DurationTimer > 0, "Flame Streak should have duration");

                var cure = SpellManager.Spells[111];
                Assert.AreEqual(SpellType.Shield, cure.Type);
                Assert.AreEqual(SpellFriendlyType.Friendly, cure.Friendly, "Cure should be friendly");
                Assert.IsTrue(cure.TargetSpellEffect > 0, "Cure should have target_spell_effect");
                Assert.AreEqual(234, cure.TargetSpellEffect, "Cure target_spell_effect should be 234");

                var resistHeat = SpellManager.Spells[109];
                Assert.IsNotNull(resistHeat, "Resist Heat should exist");
                Assert.AreEqual(SpellFriendlyType.Friendly, resistHeat.Friendly);
                Assert.AreEqual(104, resistHeat.TargetSpellEffect, "Resist Heat target_spell_effect should be 104");

                var hasteI = SpellManager.Spells[166];
                Assert.AreEqual(SpellType.Effect, hasteI.Type);
                Assert.IsTrue(hasteI.Duration > 0, "Haste I should have duration");

                var transferII = SpellManager.Spells[170];
                Assert.AreEqual("Transfer II", transferII.Name);
                Assert.AreEqual(SpellType.Shield, transferII.Type);
                Assert.AreEqual(SpellFriendlyType.Friendly, transferII.Friendly);
                Assert.AreEqual(253, transferII.CasterSpellEffect, "Transfer II caster_spell_effect=253");
                Assert.AreEqual(254, transferII.TargetSpellEffect, "Transfer II target_spell_effect=254");
                Assert.AreEqual(7, transferII.Power, "Transfer II power cost");
                Assert.AreEqual(400, transferII.Range, "Transfer II range");

                // Fire Ball II — projectile with AOE and death effect
                var fireBallII = SpellManager.Spells[124];
                Assert.AreEqual(SpellType.Projectile, fireBallII.Type);
                Assert.IsTrue(fireBallII.DamageBase > 0 || fireBallII.DamageNumDice > 0, "Fire Ball II should deal damage");
                Assert.IsTrue(fireBallII.EffectRadius > 0, "Fire Ball II should have AOE radius");
                Assert.AreEqual(22, fireBallII.DeathSpellEffect, "Fire Ball II death_spell_effect=22 (burning)");
                Assert.AreEqual(SpellElementType.Fire, fireBallII.Element);

                // Reflective Ice II — projectile
                var reflIceII = SpellManager.Spells[29];
                Assert.IsNotNull(reflIceII, "Reflective Ice II should exist");
                Assert.AreEqual("Reflective Ice II", reflIceII.Name);
                Assert.AreEqual(SpellElementType.Cold, reflIceII.Element);
                Assert.AreEqual(44, reflIceII.DeathSpellEffect, "Reflective Ice II death_spell_effect=44");

                // Bless I — shield with friendly target
                var blessI = SpellManager.Spells[114];
                Assert.AreEqual("Bless I", blessI.Name);
                Assert.AreEqual(SpellType.Shield, blessI.Type);
                Assert.AreEqual(SpellFriendlyType.Friendly, blessI.Friendly);
                Assert.AreEqual(0, blessI.CasterSpellEffect, "Bless I has no caster effect");
                Assert.AreEqual(231, blessI.TargetSpellEffect, "Bless I target_spell_effect=231");

                // Spirit Gate — teleport type
                var spiritGate = SpellManager.Spells[176];
                Assert.IsNotNull(spiritGate, "Spirit Gate should exist");
                Assert.AreEqual("Spirit Gate", spiritGate.Name);

                // Bleeding — projectile with bleed death effect
                var bleeding = SpellManager.Spells[188];
                Assert.IsNotNull(bleeding, "Bleeding should exist");
                Assert.AreEqual(199, bleeding.DeathSpellEffect, "Bleeding death_spell_effect=199");
            }
            finally
            {
                Directory.SetCurrentDirectory(origDir);
            }
        }

        [Test]
        public void LoadSpellEffects_LookupById_ReturnsCorrectEffect()
        {
            string origDir = Directory.GetCurrentDirectory();
            string dir = TestContext.CurrentContext.TestDirectory ?? Directory.GetCurrentDirectory();
            for (int i = 0; i < 6; i++)
            {
                if (dir == null) break;
                string candidate = Path.Combine(dir, "Build", "Debug");
                if (File.Exists(Path.Combine(candidate, "Spells.dat")))
                {
                    Directory.SetCurrentDirectory(candidate);
                    break;
                }
                dir = Path.GetDirectoryName(dir);
            }

            try
            {
                Assume.That(File.Exists("Spells.dat"), "Spells.dat not found");
                Assume.That(File.Exists("spell_effects.json"), "spell_effects.json not found");

                SpellManager.LoadSpells();

                // Effect spells from JSON should be accessible by their ID
                Assert.IsNotNull(SpellManager.Spells[104], "Spell 104 (Resist Heat Effect) should exist");
                Assert.AreEqual("Resist Heat Effect", SpellManager.Spells[104].Name);

                Assert.IsNotNull(SpellManager.Spells[234], "Spell 234 (Cure Effect) should exist");
                Assert.AreEqual("Cure Effect", SpellManager.Spells[234].Name);

                Assert.IsNotNull(SpellManager.Spells[231], "Spell 231 (Bless I Effect) should exist");
                Assert.AreEqual("Bless I Effect", SpellManager.Spells[231].Name);

                // Original spells should still work after JSON effects loaded
                Assert.IsNotNull(SpellManager.Spells[111], "Spell 111 (Cure) should still exist");
                Assert.AreEqual("Cure", SpellManager.Spells[111].Name);

                // Verify effect spell fields
                var resistHeatEffect = SpellManager.Spells[104];
                Assert.AreEqual(SpellType.Effect, resistHeatEffect.Type);
                Assert.AreEqual(SpellEffectType.Resist, resistHeatEffect.Effect);
                Assert.AreEqual(SpellElementType.Fire, resistHeatEffect.Element);
                Assert.AreEqual(45, resistHeatEffect.Potency);
                Assert.AreEqual(3600, resistHeatEffect.Duration);

                var blessIEffect = SpellManager.Spells[231];
                Assert.AreEqual(SpellEffectType.Bless, blessIEffect.Effect);
                Assert.AreEqual(12, blessIEffect.Potency);
                Assert.AreEqual(3600, blessIEffect.Duration);

                var cureEffect = SpellManager.Spells[234];
                Assert.AreEqual(SpellEffectType.Healing, cureEffect.Effect);
                Assert.AreEqual(30, cureEffect.Potency);
                Assert.AreEqual(1, cureEffect.Duration);

                // ============================================================
                // SpellDamage chaining — verify effect spells produce correct values
                // ============================================================

                // Cure Effect (spell 234, JSON-loaded, TargetSpellEffect=0)
                // SpellDamage should use the spell itself when TargetSpellEffect=0
                var cureDmg = new SpellDamage(cureEffect);
                Assert.That(cureDmg.Healing, Is.GreaterThan(0), $"Cure Effect should produce healing (got {cureDmg.Healing})");

                // Resist Heat Effect (spell 104, JSON-loaded, Resist type)
                // Should not produce healing or damage — it's a buff stored in Effects[]
                var resistDmg = new SpellDamage(resistHeatEffect);
                Assert.AreEqual(0, resistDmg.Damage, "Resist effect should not deal damage");
                Assert.AreEqual(0, resistDmg.Healing, "Resist effect should not heal");

                // Flame Streak I (spell 1, Spells.dat-loaded, projectile with dice)
                var flameStreakDmg = new SpellDamage(SpellManager.Spells[1]);
                Assert.That(flameStreakDmg.Damage, Is.GreaterThan(0), "Flame Streak should deal damage");

                // Fire Ball II (spell 124, projectile with AOE)
                var fireballDmg = new SpellDamage(SpellManager.Spells[124]);
                Assert.That(fireballDmg.Damage, Is.GreaterThan(0), "Fire Ball II should deal damage");

                // Bless I Effect (spell 231, JSON-loaded, Bless type)
                var blessEffect = SpellManager.Spells[231];
                var blessDmg = new SpellDamage(blessEffect);
                Assert.AreEqual(0, blessDmg.Damage, "Bless effect should not deal damage");

                // ============================================================
                // SpellTuning — verify ComputeEffectValue for loaded spells
                // ============================================================

                // Cure Effect: Healing potency=30, casterLevel=0 → 30 HP
                float cureHeal = SpellTuning.ComputeEffectValue(SpellEffectType.Healing, SpellTuning.GetPotency(cureEffect), 0);
                Assert.AreEqual(30f, cureHeal, 0.1f, "Cure should heal 30 HP at level 0");

                // Cure Effect: Healing potency=30, casterLevel=10 → 35 HP
                float cureHealLv10 = SpellTuning.ComputeEffectValue(SpellEffectType.Healing, SpellTuning.GetPotency(cureEffect), 10);
                Assert.AreEqual(35f, cureHealLv10, 0.1f, "Cure should heal 35 HP at level 10");

                // Resist Heat Effect: potency=45 → 45% reduction
                float resistVal = SpellTuning.ComputeEffectValue(SpellEffectType.Resist, SpellTuning.GetPotency(resistHeatEffect), 0);
                Assert.AreEqual(45f, resistVal, 0.1f, "Resist Heat should reduce 45%");

                // Bless I Effect: potency=12, casterLevel=0 → 12%
                float blessVal = SpellTuning.ComputeEffectValue(SpellEffectType.Bless, SpellTuning.GetPotency(blessEffect), 0);
                Assert.AreEqual(12f, blessVal, 0.1f, "Bless I should reduce 12% at level 0");

                // Bless I Effect: potency=12, casterLevel=10 → 15%
                float blessValLv10 = SpellTuning.ComputeEffectValue(SpellEffectType.Bless, SpellTuning.GetPotency(blessEffect), 10);
                Assert.AreEqual(15f, blessValLv10, 0.1f, "Bless I should reduce 15% at level 10");

                // GetPotency prefers Potency field over Level
                Assert.AreEqual(30, SpellTuning.GetPotency(cureEffect), "GetPotency should return Potency field");
                Assert.AreEqual(45, SpellTuning.GetPotency(resistHeatEffect), "GetPotency should return Potency field");
            }
            finally
            {
                Directory.SetCurrentDirectory(origDir);
            }
        }
    }
}
