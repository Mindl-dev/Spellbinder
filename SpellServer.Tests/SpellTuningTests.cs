using System.IO;
using NUnit.Framework;
using SpellServer;

namespace SpellServer.Tests
{
    [TestFixture]
    public class SpellTuningTests
    {
        [Test]
        public void GetDamageMultiplier_NonProjectile_Returns1()
        {
            Assert.AreEqual(1.0f, SpellTuning.GetDamageMultiplier(new Spell { Type = SpellType.Rune }), 0.01f);
            Assert.AreEqual(1.0f, SpellTuning.GetDamageMultiplier(new Spell { Type = SpellType.Wall }), 0.01f);
            Assert.AreEqual(1.0f, SpellTuning.GetDamageMultiplier(new Spell { Type = SpellType.Effect }), 0.01f);
            Assert.AreEqual(1.0f, SpellTuning.GetDamageMultiplier(new Spell { Type = SpellType.Shield }), 0.01f);
            Assert.AreEqual(1.0f, SpellTuning.GetDamageMultiplier(null), 0.01f);
        }

        [Test]
        public void FixedDamage_IsDeterministic()
        {
            var spell = new Spell
            {
                Type = SpellType.Projectile,
                DamageBase = 1,
                DamageNumDice = 2,
                DamageDice = 4,
                Velocity = 600
            };

            var dmg1 = new SpellDamage(spell);
            var dmg2 = new SpellDamage(spell);
            var dmg3 = new SpellDamage(spell);

            Assert.AreEqual(dmg1.Damage, dmg2.Damage, "Fixed damage should be identical across calls");
            Assert.AreEqual(dmg2.Damage, dmg3.Damage, "Fixed damage should be identical across calls");
            Assert.Greater(dmg1.Damage, 0, "Damage should be positive");
        }

        [Test]
        public void Damage_CappedAt255()
        {
            var spell = new Spell
            {
                Type = SpellType.Projectile,
                DamageBase = 100,
                DamageNumDice = 10,
                DamageDice = 20,
                Velocity = 600
            };

            var dmg = new SpellDamage(spell);
            Assert.LessOrEqual(dmg.Damage, 255, "Damage should be capped at 255");
        }

        [Test]
        public void HitscanAndProjectile_BothGetMultiplied()
        {
            var proj = new Spell { Type = SpellType.Projectile, DamageBase = 10, DamageNumDice = 0, Velocity = 600 };
            var scan = new Spell { Type = SpellType.Projectile, DamageBase = 10, DamageNumDice = 0, Velocity = 2000 };

            var projDmg = new SpellDamage(proj);
            var scanDmg = new SpellDamage(scan);

            Assert.Greater(projDmg.Damage, 10, "Projectile should be multiplied above base");
            Assert.AreNotEqual(10, scanDmg.Damage, "Hitscan should be multiplied (0.8x = 8, not 10)");
        }

        [Test]
        public void RuneDamage_NotMultiplied()
        {
            var rune = new Spell { Type = SpellType.Rune, DamageBase = 10, DamageNumDice = 0, DamageDice = 0 };
            var dmg = new SpellDamage(rune);
            Assert.AreEqual(10, dmg.Damage, "Rune damage should not be multiplied");
        }

        /// <summary>
        /// spell_effects.json should merge effect data into Arcane Shield (exists in
        /// Spells.dat but has no effect type). After merge, it should have Resist effect.
        /// </summary>
        [Test]
        public void SpellEffectsJson_MergesArcaneShield()
        {
            string origDir = Directory.GetCurrentDirectory();
            string dir = TestContext.CurrentContext.TestDirectory ?? Directory.GetCurrentDirectory();
            for (int i = 0; i < 6; i++)
            {
                if (dir == null) break;
                // Check CWD itself (Docker: /app/), Build/Debug, Content
                foreach (var candidate in new[] { dir, Path.Combine(dir, "Build", "Debug"), Path.Combine(dir, "Content") })
                {
                    if (File.Exists(Path.Combine(candidate, "Spells.dat")))
                    {
                        Directory.SetCurrentDirectory(candidate);
                        goto found;
                    }
                }
                dir = Path.GetDirectoryName(dir);
            }
            Assert.Ignore("Spells.dat not found");

            found:
            if (!File.Exists("spell_effects.json"))
                Assert.Ignore("spell_effects.json not found");

            try
            {
                SpellManager.LoadSpells();

                var arcaneShield = SpellManager.Spells[108];
                Assert.IsNotNull(arcaneShield, "Spell 108 (Arcane Shield) should exist");
                Assert.AreEqual(SpellEffectType.Resist, arcaneShield.Effect,
                    "Arcane Shield should have Resist effect after JSON merge");
                Assert.Greater(arcaneShield.Potency, 0,
                    "Arcane Shield should have potency > 0 after JSON merge");
            }
            finally
            {
                Directory.SetCurrentDirectory(origDir);
            }
        }
    }
}
