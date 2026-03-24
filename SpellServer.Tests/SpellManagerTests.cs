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
            string dir = TestContext.CurrentContext.TestDirectory;
            for (int i = 0; i < 6; i++)
            {
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
                Assert.AreEqual(SpellFriendlyType.Friendly, transferII.Friendly);
                Assert.IsTrue(transferII.TargetSpellEffect > 0, "Transfer II should have target_spell_effect");
                // Note: CasterSpellEffect may be 0 due to INI parsing — investigate separately
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
            string dir = TestContext.CurrentContext.TestDirectory;
            for (int i = 0; i < 6; i++)
            {
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
                Assert.AreEqual(20, resistHeatEffect.Potency);
                Assert.AreEqual(120, resistHeatEffect.Duration);

                var blessIEffect = SpellManager.Spells[231];
                Assert.AreEqual(SpellEffectType.Bless, blessIEffect.Effect);
                Assert.AreEqual(5, blessIEffect.Potency);
                Assert.AreEqual(60, blessIEffect.Duration);

                var cureEffect = SpellManager.Spells[234];
                Assert.AreEqual(SpellEffectType.Healing, cureEffect.Effect);
                Assert.AreEqual(30, cureEffect.Potency);
                Assert.AreEqual(1, cureEffect.Duration);
            }
            finally
            {
                Directory.SetCurrentDirectory(origDir);
            }
        }
    }
}
