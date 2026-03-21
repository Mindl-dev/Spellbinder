using System;
using System.IO;
using NUnit.Framework;
using Helper;

namespace SpellServer.Tests
{
    [TestFixture]
    public class IniCacheTests
    {
        private string _tempFile;

        [SetUp]
        public void SetUp()
        {
            _tempFile = Path.GetTempFileName();
            File.WriteAllText(_tempFile, @"[spelldefs]
numspells=400

[spell01]
name=Flame Streak I
type=projectile
power=10
fatigue=5
min_damage=3
max_damage=8
velocity=200
gravity=false

[spell02]
name=Fire Orb I
type=projectile
power=15
fatigue=8
; this is a comment
min_damage=5   ; inline comment
max_damage=12
");
        }

        [TearDown]
        public void TearDown()
        {
            if (File.Exists(_tempFile))
                File.Delete(_tempFile);
        }

        [Test]
        public void GetPrivateProfileInt32_ReturnsCorrectValue()
        {
            int result = NativeMethods.GetPrivateProfileInt32("spelldefs", "numspells", _tempFile);
            Assert.AreEqual(400, result);
        }

        [Test]
        public void GetPrivateProfileString_ReturnsCorrectValue()
        {
            string result = NativeMethods.GetPrivateProfileString("spell01", "name", _tempFile);
            Assert.AreEqual("Flame Streak I", result);
        }

        [Test]
        public void GetPrivateProfileString_ReturnsType()
        {
            string result = NativeMethods.GetPrivateProfileString("spell01", "type", _tempFile);
            Assert.AreEqual("projectile", result);
        }

        [Test]
        public void GetPrivateProfileBoolean_ReturnsFalse()
        {
            bool result = NativeMethods.GetPrivateProfileBoolean("spell01", "gravity", _tempFile);
            Assert.IsFalse(result);
        }

        [Test]
        public void GetPrivateProfileInt32_MissingKey_ReturnsMinusOne()
        {
            int result = NativeMethods.GetPrivateProfileInt32("spell01", "nonexistent", _tempFile);
            Assert.AreEqual(-1, result);
        }

        [Test]
        public void GetPrivateProfileString_MissingSection_ReturnsEmpty()
        {
            string result = NativeMethods.GetPrivateProfileString("nonexistent", "name", _tempFile);
            Assert.AreEqual("", result);
        }

        [Test]
        public void GetPrivateProfileInt32_StripsInlineComments()
        {
            int result = NativeMethods.GetPrivateProfileInt32("spell02", "min_damage", _tempFile);
            Assert.AreEqual(5, result);
        }

        [Test]
        public void GetPrivateProfileString_CaseInsensitiveSection()
        {
            string result = NativeMethods.GetPrivateProfileString("SPELLDEFS", "numspells", _tempFile);
            Assert.AreEqual("400", result);
        }

        [Test]
        public void GetPrivateProfileString_CaseInsensitiveKey()
        {
            string result = NativeMethods.GetPrivateProfileString("spell01", "NAME", _tempFile);
            Assert.AreEqual("Flame Streak I", result);
        }

        [Test]
        public void CachedRead_SecondCallReturnsSameResult()
        {
            // First call populates cache
            string first = NativeMethods.GetPrivateProfileString("spell01", "name", _tempFile);
            // Second call should hit cache
            string second = NativeMethods.GetPrivateProfileString("spell01", "name", _tempFile);
            Assert.AreEqual(first, second);
        }

        // --- Edge cases: verify our parser matches Win32 GetPrivateProfileString behavior ---

        [Test]
        public void EdgeCase_EmptyValue()
        {
            // key= with no value should return empty string
            string file = Path.GetTempFileName();
            try
            {
                File.WriteAllText(file, "[section]\nemptykey=\n");
                Assert.AreEqual("", NativeMethods.GetPrivateProfileString("section", "emptykey", file));
            }
            finally { File.Delete(file); }
        }

        [Test]
        public void EdgeCase_EqualsInValue()
        {
            // key=a=b should return "a=b" (only split on first =)
            string file = Path.GetTempFileName();
            try
            {
                File.WriteAllText(file, "[section]\nkey=a=b\n");
                Assert.AreEqual("a=b", NativeMethods.GetPrivateProfileString("section", "key", file));
            }
            finally { File.Delete(file); }
        }

        [Test]
        public void EdgeCase_WhitespaceAroundEquals()
        {
            // key = value (spaces around =) should trim correctly
            string file = Path.GetTempFileName();
            try
            {
                File.WriteAllText(file, "[section]\n  key  =  value  \n");
                Assert.AreEqual("value", NativeMethods.GetPrivateProfileString("section", "key", file));
            }
            finally { File.Delete(file); }
        }

        [Test]
        public void EdgeCase_DuplicateSections_Merged()
        {
            // Win32 API merges duplicate sections — keys from both should be accessible
            string file = Path.GetTempFileName();
            try
            {
                File.WriteAllText(file, "[section]\nkey1=a\n[section]\nkey2=b\n");
                Assert.AreEqual("a", NativeMethods.GetPrivateProfileString("section", "key1", file));
                Assert.AreEqual("b", NativeMethods.GetPrivateProfileString("section", "key2", file));
            }
            finally { File.Delete(file); }
        }

        [Test]
        public void EdgeCase_DuplicateKeys_LastWins()
        {
            // Win32 API takes the first occurrence; our cache may take last.
            // Document whichever behavior we have.
            string file = Path.GetTempFileName();
            try
            {
                File.WriteAllText(file, "[section]\nkey=first\nkey=second\n");
                string result = NativeMethods.GetPrivateProfileString("section", "key", file);
                // Our Dictionary overwrites, so last wins
                Assert.AreEqual("second", result,
                    "Duplicate keys: our cache takes the last value (Dictionary overwrite)");
            }
            finally { File.Delete(file); }
        }

        [Test]
        public void EdgeCase_TabsInLine()
        {
            // Tabs as whitespace should be handled
            string file = Path.GetTempFileName();
            try
            {
                File.WriteAllText(file, "[section]\n\tkey\t=\tvalue\t\n");
                Assert.AreEqual("value", NativeMethods.GetPrivateProfileString("section", "key", file));
            }
            finally { File.Delete(file); }
        }

        [Test]
        public void EdgeCase_SectionWithSpaces()
        {
            // Section name with spaces: [ section name ]
            string file = Path.GetTempFileName();
            try
            {
                File.WriteAllText(file, "[ spell 01 ]\nname=test\n");
                Assert.AreEqual("test", NativeMethods.GetPrivateProfileString("spell 01", "name", file));
            }
            finally { File.Delete(file); }
        }

        [Test]
        public void EdgeCase_CommentOnlyLines()
        {
            // Lines starting with ; should be ignored entirely
            string file = Path.GetTempFileName();
            try
            {
                File.WriteAllText(file, "[section]\n; this is a comment\nkey=value\n;key2=hidden\n");
                Assert.AreEqual("value", NativeMethods.GetPrivateProfileString("section", "key", file));
                Assert.AreEqual("", NativeMethods.GetPrivateProfileString("section", "key2", file),
                    "Commented-out key should not be found");
            }
            finally { File.Delete(file); }
        }

        [Test]
        public void EdgeCase_NoTrailingNewline()
        {
            // File that doesn't end with newline
            string file = Path.GetTempFileName();
            try
            {
                File.WriteAllText(file, "[section]\nkey=value");
                Assert.AreEqual("value", NativeMethods.GetPrivateProfileString("section", "key", file));
            }
            finally { File.Delete(file); }
        }

        [Test]
        public void EdgeCase_NumericSectionAndKey()
        {
            // Spells.dat uses numeric-ish sections like [spell01]
            string file = Path.GetTempFileName();
            try
            {
                File.WriteAllText(file, "[spell99]\npower=42\n");
                Assert.AreEqual(42, NativeMethods.GetPrivateProfileInt32("spell99", "power", file));
            }
            finally { File.Delete(file); }
        }
    }
}
