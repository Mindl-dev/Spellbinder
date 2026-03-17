using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using NUnit.Framework;
using Helper;

namespace SpellServer.Tests
{
    /// <summary>Compares our INI cache against the real Win32 GetPrivateProfileString API
    /// to verify identical behavior on the actual Spells.dat file.</summary>
    [TestFixture]
    public class IniCacheVsWin32Tests
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetPrivateProfileString(
            string section, string key, string def,
            StringBuilder retVal, int size, string filePath);

        private static bool _isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        private static string Win32Read(string section, string key, string path)
        {
            if (!_isWindows) return null;
            StringBuilder sb = new StringBuilder(512);
            GetPrivateProfileString(section, key, "", sb, 512, path);
            string raw = sb.ToString();
            // Strip inline comments to match our cache behavior
            int semi = raw.IndexOf(';');
            if (semi >= 0) raw = raw.Substring(0, semi);
            return raw.Trim();
        }

        private string _spellsPath;
        private string _arenasPath;

        [OneTimeSetUp]
        public void Setup()
        {
            string[] searchPaths = new[]
            {
                Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "Build", "Debug", "Spells.dat"),
                Path.Combine(Directory.GetCurrentDirectory(), "Spells.dat"),
                Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "Build", "Debug", "Spells.dat"),
                Path.Combine(Directory.GetCurrentDirectory(), "Content", "Spells.dat"),
            };

            foreach (var p in searchPaths)
            {
                string full = Path.GetFullPath(p);
                if (File.Exists(full))
                {
                    _spellsPath = full;
                    break;
                }
            }

            string[] arenaSearchPaths = new[]
            {
                Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "Build", "Debug", "Arenas.dat"),
                Path.Combine(Directory.GetCurrentDirectory(), "Arenas.dat"),
                Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "Build", "Debug", "Arenas.dat"),
                Path.Combine(Directory.GetCurrentDirectory(), "Content", "Arenas.dat"),
            };

            foreach (var p in arenaSearchPaths)
            {
                string full = Path.GetFullPath(p);
                if (File.Exists(full))
                {
                    _arenasPath = full;
                    break;
                }
            }
        }

        [Test]
        public void SpellsDat_AllSpells_MatchWin32()
        {
            if (!_isWindows)
                Assert.Ignore("Win32 API not available on this platform");
            if (_spellsPath == null)
                Assert.Ignore("Spells.dat not found — skipping Win32 comparison");

            int numSpells = NativeMethods.GetPrivateProfileInt32("spelldefs", "numspells", _spellsPath);
            int win32NumSpells = int.Parse(Win32Read("spelldefs", "numspells", _spellsPath));
            Assert.AreEqual(win32NumSpells, numSpells, "numspells mismatch");

            string[] keys = {
                "name", "type", "power", "fatigue", "min_fatigue",
                "num_cast_sounds", "cast_sound", "cast_sound2", "cast_sound3", "cast_sound4",
                "empty_sound", "switch_sound", "fire_timer", "cast_timer", "overlay",
                "imagenum", "death_imagenum", "width", "tall", "image_timer_max",
                "death_image_timer_max", "gravity", "light_pattern", "max_flicker",
                "light_glow", "sticky_light", "duration_timer", "trans_color",
                "death_trans_color", "effect_radius", "miss_sound",
                "min_damage", "max_damage", "damage_dice", "damage_num_dice", "damage_base",
                "velocity", "z_velocity", "cast_angle", "num_projectiles",
                "projectile_spacing", "side_by_side", "cast_distance", "elevation",
                "max_step", "translucent", "death_translucent",
                "death_effect", "death_effect_range", "death_effect_chance",
                "creation_effect", "sound", "sound_range", "death_sound_range",
                "effect_sound_range", "death_sound", "hit_sound", "effect_sound",
                "element", "no_team", "skill_used"
            };

            int mismatches = 0;
            int comparisons = 0;

            for (int i = 1; i <= numSpells; i++)
            {
                string section = $"spell{i:00}";

                foreach (string key in keys)
                {
                    string cached = NativeMethods.GetPrivateProfileString(section, key, _spellsPath) ?? "";
                    string win32 = Win32Read(section, key, _spellsPath);

                    comparisons++;
                    if (cached != win32)
                    {
                        mismatches++;
                        TestContext.WriteLine($"MISMATCH [{section}] {key}: cache='{cached}' win32='{win32}'");
                    }
                }
            }

            TestContext.WriteLine($"Compared {comparisons} values across {numSpells} spells. Mismatches: {mismatches}");
            Assert.AreEqual(0, mismatches,
                $"{mismatches} mismatches between INI cache and Win32 API out of {comparisons} comparisons");
        }

        [Test]
        public void ArenasDat_AllArenas_MatchWin32()
        {
            if (!_isWindows)
                Assert.Ignore("Win32 API not available on this platform");
            if (_arenasPath == null)
                Assert.Ignore("Arenas.dat not found — skipping Win32 comparison");

            int numArenas = NativeMethods.GetPrivateProfileInt32("arenadefs", "numarenas", _arenasPath);
            int win32NumArenas = int.Parse(Win32Read("arenadefs", "numarenas", _arenasPath));
            Assert.AreEqual(win32NumArenas, numArenas, "numarenas mismatch");

            string[] keys = { "grid", "name", "short_name", "maxplayers", "timelimit", "expbonus" };

            int mismatches = 0;
            int comparisons = 0;

            for (int i = 0; i < numArenas; i++)
            {
                string section = $"arena{i:00}";

                foreach (string key in keys)
                {
                    string cached = NativeMethods.GetPrivateProfileString(section, key, _arenasPath) ?? "";
                    string win32 = Win32Read(section, key, _arenasPath);

                    comparisons++;
                    if (cached != win32)
                    {
                        mismatches++;
                        TestContext.WriteLine($"MISMATCH [{section}] {key}: cache='{cached}' win32='{win32}'");
                    }
                }
            }

            TestContext.WriteLine($"Compared {comparisons} values across {numArenas} arenas. Mismatches: {mismatches}");
            Assert.AreEqual(0, mismatches,
                $"{mismatches} mismatches between INI cache and Win32 API out of {comparisons} comparisons");
        }
    }
}
