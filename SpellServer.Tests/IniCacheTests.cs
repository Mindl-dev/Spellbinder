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
    }
}
