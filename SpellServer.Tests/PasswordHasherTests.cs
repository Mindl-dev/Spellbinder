using System;
using NUnit.Framework;

namespace SpellServer.Tests
{
    [TestFixture]
    public class PasswordHasherTests
    {
        [Test]
        public void Hash_ProducesValidFormat()
        {
            string hash = PasswordHasher.Hash("test1");
            Assert.That(hash, Does.StartWith("$PBKDF2$"));

            string[] parts = hash.Split('$');
            Assert.AreEqual(5, parts.Length, "Format: $PBKDF2$iterations$salt$hash");
            Assert.AreEqual("PBKDF2", parts[1]);

            int iterations;
            Assert.IsTrue(int.TryParse(parts[2], out iterations));
            Assert.GreaterOrEqual(iterations, 100000);
        }

        [Test]
        public void Hash_DifferentSaltEachTime()
        {
            string hash1 = PasswordHasher.Hash("test1");
            string hash2 = PasswordHasher.Hash("test1");
            Assert.AreNotEqual(hash1, hash2, "Same password must produce different hashes (unique salt)");
        }

        [Test]
        public void Verify_CorrectPassword_ReturnsTrue()
        {
            string hash = PasswordHasher.Hash("mypassword");
            Assert.IsTrue(PasswordHasher.Verify("mypassword", hash));
        }

        [Test]
        public void Verify_WrongPassword_ReturnsFalse()
        {
            string hash = PasswordHasher.Hash("mypassword");
            Assert.IsFalse(PasswordHasher.Verify("wrongpassword", hash));
        }

        [Test]
        public void Verify_EmptyPassword()
        {
            string hash = PasswordHasher.Hash("");
            Assert.IsTrue(PasswordHasher.Verify("", hash));
            Assert.IsFalse(PasswordHasher.Verify("notempty", hash));
        }

        [Test]
        public void Verify_MaxLengthPassword()
        {
            // Client sends max 20 bytes for password
            string maxPw = "12345678901234567890";
            string hash = PasswordHasher.Hash(maxPw);
            Assert.IsTrue(PasswordHasher.Verify(maxPw, hash));
            Assert.IsFalse(PasswordHasher.Verify("1234567890123456789", hash));
        }

        [Test]
        public void Verify_BackwardsCompatible_PlaintextPassword()
        {
            // Legacy: stored password is plaintext (no $PBKDF2$ prefix)
            Assert.IsTrue(PasswordHasher.Verify("test1", "test1"));
            Assert.IsFalse(PasswordHasher.Verify("test1", "test2"));
        }

        [Test]
        public void Verify_NullOrEmpty_ReturnsFalse()
        {
            Assert.IsFalse(PasswordHasher.Verify("test", null));
            Assert.IsFalse(PasswordHasher.Verify("test", ""));
        }

        [Test]
        public void IsHashed_DetectsHashedPassword()
        {
            string hash = PasswordHasher.Hash("test1");
            Assert.IsTrue(PasswordHasher.IsHashed(hash));
        }

        [Test]
        public void IsHashed_DetectsPlaintextPassword()
        {
            Assert.IsFalse(PasswordHasher.IsHashed("test1"));
            Assert.IsFalse(PasswordHasher.IsHashed(""));
            Assert.IsFalse(PasswordHasher.IsHashed(null));
        }

        [Test]
        public void Verify_MalformedHash_ReturnsFalse()
        {
            Assert.IsFalse(PasswordHasher.Verify("test", "$PBKDF2$garbage"));
            Assert.IsFalse(PasswordHasher.Verify("test", "$PBKDF2$notanumber$AAAA$BBBB"));
        }
    }
}
