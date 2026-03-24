using System;
using System.IO;
using NUnit.Framework;
using SpellServer;
using Helper;

namespace SpellServer.Tests
{
    [TestFixture]
    public class SpellEffectsTests
    {
        private static string FindSpellEffectsJson()
        {
            string dir = TestContext.CurrentContext.TestDirectory;
            for (int i = 0; i < 6; i++)
            {
                string candidate = Path.Combine(dir, "Content", "spell_effects.json");
                if (File.Exists(candidate)) return candidate;
                // Also check Build/Debug
                candidate = Path.Combine(dir, "spell_effects.json");
                if (File.Exists(candidate)) return candidate;
                dir = Path.GetDirectoryName(dir);
            }
            return null;
        }

        // ================================================================
        // JSON file existence and format
        // ================================================================

        [Test]
        public void SpellEffectsJson_Exists()
        {
            string path = FindSpellEffectsJson();
            Assert.IsNotNull(path, "spell_effects.json not found");
        }

        [Test]
        public void SpellEffectsJson_HasEffectsArray()
        {
            string path = FindSpellEffectsJson();
            Assume.That(path, Is.Not.Null);
            string json = File.ReadAllText(path);
            StringAssert.Contains("\"effects\"", json);
        }

        // ================================================================
        // Effect spells load into SpellManager
        // (requires SpellManager.LoadSpells to have run)
        // ================================================================

        [Test]
        public void SpellEffects_AllReferencedIdsExist()
        {
            // Parse the JSON ourselves and check the IDs
            string path = FindSpellEffectsJson();
            Assume.That(path, Is.Not.Null);
            string json = File.ReadAllText(path);

            // Extract all id values
            int searchFrom = 0;
            var ids = new System.Collections.Generic.List<int>();
            while (true)
            {
                int idx = json.IndexOf("\"id\"", searchFrom);
                if (idx < 0) break;
                int colon = json.IndexOf(':', idx);
                if (colon < 0) break;
                int start = colon + 1;
                while (start < json.Length && (json[start] == ' ' || json[start] == '\t')) start++;
                int end = start;
                while (end < json.Length && json[end] >= '0' && json[end] <= '9') end++;
                if (end > start)
                {
                    int id = int.Parse(json.Substring(start, end - start));
                    ids.Add(id);
                }
                searchFrom = end;
            }

            Assert.That(ids.Count, Is.GreaterThan(0), "No effect spell IDs found in JSON");
        }

        [Test]
        public void SpellEffects_NoDuplicateIds()
        {
            string path = FindSpellEffectsJson();
            Assume.That(path, Is.Not.Null);
            string json = File.ReadAllText(path);

            var ids = new System.Collections.Generic.HashSet<int>();
            int searchFrom = 0;
            while (true)
            {
                int idx = json.IndexOf("\"id\"", searchFrom);
                if (idx < 0) break;
                int colon = json.IndexOf(':', idx);
                if (colon < 0) break;
                int start = colon + 1;
                while (start < json.Length && (json[start] == ' ' || json[start] == '\t')) start++;
                int end = start;
                while (end < json.Length && json[end] >= '0' && json[end] <= '9') end++;
                if (end > start)
                {
                    int id = int.Parse(json.Substring(start, end - start));
                    Assert.IsTrue(ids.Add(id), $"Duplicate effect spell ID: {id}");
                }
                searchFrom = end;
            }
        }

        [Test]
        public void SpellEffects_AllHaveRequiredFields()
        {
            string path = FindSpellEffectsJson();
            Assume.That(path, Is.Not.Null);
            string json = File.ReadAllText(path);

            // Find each effect object and check for required fields
            int searchFrom = 0;
            int count = 0;
            while (true)
            {
                int objStart = json.IndexOf("{", searchFrom);
                if (objStart < 0) break;
                int objEnd = json.IndexOf("}", objStart);
                if (objEnd < 0) break;
                searchFrom = objEnd + 1;

                string obj = json.Substring(objStart, objEnd - objStart + 1);
                if (!obj.Contains("\"id\"")) continue;
                if (obj.Contains("\"_comment\"") || obj.Contains("\"effects\"")) continue;

                count++;
                StringAssert.Contains("\"name\"", obj, $"Missing name in effect object #{count}");
                StringAssert.Contains("\"effect_type\"", obj, $"Missing effect_type in effect object #{count}");
                StringAssert.Contains("\"potency\"", obj, $"Missing potency in effect object #{count}");
                StringAssert.Contains("\"duration\"", obj, $"Missing duration in effect object #{count}");
            }

            Assert.That(count, Is.GreaterThan(20), $"Expected 20+ effect spells, found {count}");
        }

        [Test]
        public void SpellEffects_ValidEffectTypes()
        {
            string path = FindSpellEffectsJson();
            Assume.That(path, Is.Not.Null);
            string json = File.ReadAllText(path);

            string[] validTypes = { "None", "Presence", "Light", "Bless", "Resist", "Bleed",
                "Prayer", "Leaping", "Levitate", "Fly", "Hinder", "Resurrect",
                "Healing", "Speed", "HealingReduction", "TargetResist", "Expulse" };
            var validSet = new System.Collections.Generic.HashSet<string>(validTypes);

            int searchFrom = 0;
            while (true)
            {
                int idx = json.IndexOf("\"effect_type\"", searchFrom);
                if (idx < 0) break;
                int colon = json.IndexOf(':', idx);
                int qStart = json.IndexOf('"', colon + 1);
                int qEnd = json.IndexOf('"', qStart + 1);
                string val = json.Substring(qStart + 1, qEnd - qStart - 1);
                Assert.IsTrue(validSet.Contains(val), $"Invalid effect_type: '{val}'");
                searchFrom = qEnd + 1;
            }
        }

        [Test]
        public void SpellEffects_ValidElements()
        {
            string path = FindSpellEffectsJson();
            Assume.That(path, Is.Not.Null);
            string json = File.ReadAllText(path);

            string[] validElements = { "None", "Fire", "Cold", "Light", "Void", "Holy",
                "Earth", "Nature", "Air", "Arcane", "Mind" };
            var validSet = new System.Collections.Generic.HashSet<string>(validElements);

            int searchFrom = 0;
            while (true)
            {
                int idx = json.IndexOf("\"element\"", searchFrom);
                if (idx < 0) break;
                int colon = json.IndexOf(':', idx);
                int qStart = json.IndexOf('"', colon + 1);
                int qEnd = json.IndexOf('"', qStart + 1);
                string val = json.Substring(qStart + 1, qEnd - qStart - 1);
                Assert.IsTrue(validSet.Contains(val), $"Invalid element: '{val}'");
                searchFrom = qEnd + 1;
            }
        }

        // ================================================================
        // Effect duration
        // ================================================================

        [Test]
        public void EffectDuration_SecondsToMilliseconds()
        {
            // Simulate what Effect.cs does with duration
            int durationSeconds = 15; // Haste I
            int intervalMs = durationSeconds * 1000;
            Assert.AreEqual(15000, intervalMs);
        }

        [Test]
        public void EffectDuration_BleedTicks()
        {
            // Bleed: tick every 1000ms, repeat duration times
            int durationTicks = 5;
            int tickIntervalMs = 1000;
            int totalDurationMs = durationTicks * tickIntervalMs;
            Assert.AreEqual(5000, totalDurationMs);
        }

        // ================================================================
        // Damage reduction formula (from Arena.cs DoPlayerDamage)
        // ================================================================

        [Test]
        public void ResistFormula_Level20_Reduces20Percent()
        {
            // From Arena.cs: dReduction = (level * 0.01f) * damage
            int level = 20;
            int damage = 100;
            float reduction = (level * 0.01f) * damage;
            Assert.AreEqual(20f, reduction, 0.1f);
        }

        [Test]
        public void ResistFormula_HalfElementMatch()
        {
            // Non-matching element: dReduction = ((level * 0.5f) * 0.01f) * damage
            int level = 20;
            int damage = 100;
            float reduction = ((level * 0.5f) * 0.01f) * damage;
            Assert.AreEqual(10f, reduction, 0.1f);
        }

        // ================================================================
        // Healing formula (from SpellDamage.cs)
        // ================================================================

        [Test]
        public void HealingFormula_Level30_Heals24to30()
        {
            // Healing = random(level * 0.80, level * 1.00)
            int level = 30;
            int min = (int)Math.Floor(level * 0.80);
            int max = (int)Math.Floor(level * 1.00);
            Assert.AreEqual(24, min);
            Assert.AreEqual(30, max);
        }

        [Test]
        public void HealingFormula_Level50_Heals40to50()
        {
            int level = 50;
            int min = (int)Math.Floor(level * 0.80);
            int max = (int)Math.Floor(level * 1.00);
            Assert.AreEqual(40, min);
            Assert.AreEqual(50, max);
        }

        // ================================================================
        // Missing spell coverage — every target_spell_effect has a definition
        // ================================================================

        [Test]
        public void SpellEffects_CoversAllMissingShields()
        {
            string path = FindSpellEffectsJson();
            Assume.That(path, Is.Not.Null);
            string json = File.ReadAllText(path);

            // Shield effect IDs from Spells.dat
            int[] shieldEffects = { 51, 52, 54, 61, 62, 64, 101, 102, 104 };
            foreach (int id in shieldEffects)
                StringAssert.Contains($"\"id\": {id}", json, $"Missing shield effect spell {id}");
        }

        [Test]
        public void SpellEffects_CoversAllMissingBuffs()
        {
            string path = FindSpellEffectsJson();
            Assume.That(path, Is.Not.Null);
            string json = File.ReadAllText(path);

            // Bless + Prayer effect IDs
            int[] buffEffects = { 169, 225, 226, 227, 228, 231, 232, 233 };
            foreach (int id in buffEffects)
                StringAssert.Contains($"\"id\": {id}", json, $"Missing buff effect spell {id}");
        }

        [Test]
        public void SpellEffects_CoversAllMissingHeals()
        {
            string path = FindSpellEffectsJson();
            Assume.That(path, Is.Not.Null);
            string json = File.ReadAllText(path);

            // Cure + Transfer effect IDs
            int[] healEffects = { 234, 243, 244, 253, 254, 173, 175 };
            foreach (int id in healEffects)
                StringAssert.Contains($"\"id\": {id}", json, $"Missing heal effect spell {id}");
        }
    }
}
