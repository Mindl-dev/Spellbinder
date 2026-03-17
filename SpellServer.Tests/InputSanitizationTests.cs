using System;
using NUnit.Framework;

namespace SpellServer.Tests
{
    [TestFixture]
    public class InputSanitizationTests
    {
        // --- String sanitization ---

        [Test]
        public void SanitizeString_NormalText_Unchanged()
        {
            Assert.AreEqual("HelloWorld", InputSanitizer.SanitizeString("HelloWorld", 20));
        }

        [Test]
        public void SanitizeString_EnforcesMaxLength()
        {
            Assert.AreEqual("12345", InputSanitizer.SanitizeString("1234567890", 5));
        }

        [Test]
        public void SanitizeString_StripsNullBytes()
        {
            Assert.AreEqual("AB", InputSanitizer.SanitizeString("A\0B", 20));
        }

        [Test]
        public void SanitizeString_StripsControlChars()
        {
            // Use \0 for null, explicit char casts for other control chars
            string withCtrl = "A" + (char)1 + (char)2 + (char)3 + "B";
            Assert.AreEqual("AB", InputSanitizer.SanitizeString(withCtrl, 20));
            Assert.AreEqual("AB", InputSanitizer.SanitizeString("A\rB", 20));
            Assert.AreEqual("AB", InputSanitizer.SanitizeString("A\nB", 20));
            Assert.AreEqual("AB", InputSanitizer.SanitizeString("A\tB", 20));
        }

        [Test]
        public void SanitizeString_AllowsPrintableAscii()
        {
            // Space through tilde is printable ASCII
            string printable = " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~";
            Assert.AreEqual(printable, InputSanitizer.SanitizeString(printable, 200));
        }

        [Test]
        public void SanitizeString_StripsHighBytes()
        {
            // Bytes > 127 shouldn't appear in ASCII game protocol
            string withHigh = "A" + (char)0x80 + (char)0xFF + "B";
            Assert.AreEqual("AB", InputSanitizer.SanitizeString(withHigh, 20));
        }

        [Test]
        public void SanitizeString_EmptyInput()
        {
            Assert.AreEqual("", InputSanitizer.SanitizeString("", 20));
        }

        [Test]
        public void SanitizeString_NullInput()
        {
            Assert.AreEqual("", InputSanitizer.SanitizeString(null, 20));
        }

        // --- Username sanitization ---

        [Test]
        public void SanitizeUsername_MaxLength20()
        {
            Assert.AreEqual("12345678901234567890", InputSanitizer.SanitizeUsername("123456789012345678901234567890"));
        }

        [Test]
        public void SanitizeUsername_StripsSpaces()
        {
            // Usernames shouldn't have spaces (game client doesn't allow them)
            Assert.AreEqual("TestUser", InputSanitizer.SanitizeUsername("Test User"));
        }

        [Test]
        public void SanitizeUsername_NormalName()
        {
            Assert.AreEqual("Test1", InputSanitizer.SanitizeUsername("Test1"));
        }

        // --- Chat message sanitization ---

        [Test]
        public void SanitizeChat_MaxLength128()
        {
            string long_msg = new string('A', 300);
            Assert.AreEqual(128, InputSanitizer.SanitizeChat(long_msg).Length);
        }

        [Test]
        public void SanitizeChat_AllowsSpaces()
        {
            Assert.AreEqual("Hello World", InputSanitizer.SanitizeChat("Hello World"));
        }

        [Test]
        public void SanitizeChat_StripsControlChars()
        {
            Assert.AreEqual("Hello World", InputSanitizer.SanitizeChat("Hello\x00\x01 World"));
        }

        // --- Character name sanitization ---

        [Test]
        public void SanitizeCharName_MaxLength20()
        {
            Assert.AreEqual("12345678901234567890", InputSanitizer.SanitizeCharName("123456789012345678901234567890"));
        }

        [Test]
        public void SanitizeCharName_AllowsSpaces()
        {
            // Character names can have spaces (e.g., "Fire Mage")
            Assert.AreEqual("Fire Mage", InputSanitizer.SanitizeCharName("Fire Mage"));
        }

        // --- Enabled/disabled flag ---

        [Test]
        public void Enabled_DefaultTrue()
        {
            Assert.IsTrue(InputSanitizer.Enabled);
        }

        [Test]
        public void Disabled_PassesThrough()
        {
            bool orig = InputSanitizer.Enabled;
            try
            {
                InputSanitizer.Enabled = false;
                string dirty = "A\0" + (char)1 + "B";
                // When disabled, SanitizeString should pass through unchanged
                Assert.AreEqual(dirty, InputSanitizer.SanitizeString(dirty, 20));
            }
            finally
            {
                InputSanitizer.Enabled = orig;
            }
        }

        [Test]
        public void ReEnabled_SanitizesAgain()
        {
            bool orig = InputSanitizer.Enabled;
            try
            {
                InputSanitizer.Enabled = false;
                InputSanitizer.Enabled = true;
                Assert.AreEqual("AB", InputSanitizer.SanitizeString("A\0B", 20));
            }
            finally
            {
                InputSanitizer.Enabled = orig;
            }
        }

        // --- Byte buffer validation ---

        [Test]
        public void ValidatePacketLength_WithinBounds_ReturnsTrue()
        {
            Assert.IsTrue(InputSanitizer.ValidateLength(100, 0, 200));
        }

        [Test]
        public void ValidatePacketLength_Zero_ReturnsTrue()
        {
            Assert.IsTrue(InputSanitizer.ValidateLength(0, 0, 200));
        }

        [Test]
        public void ValidatePacketLength_Negative_ReturnsFalse()
        {
            Assert.IsFalse(InputSanitizer.ValidateLength(-1, 0, 200));
        }

        [Test]
        public void ValidatePacketLength_ExceedsMax_ReturnsFalse()
        {
            Assert.IsFalse(InputSanitizer.ValidateLength(201, 0, 200));
        }

        [Test]
        public void ValidatePacketLength_BelowMin_ReturnsFalse()
        {
            Assert.IsFalse(InputSanitizer.ValidateLength(5, 10, 200));
        }
    }
}
