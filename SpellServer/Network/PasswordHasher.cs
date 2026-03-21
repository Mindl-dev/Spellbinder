using System;
using System.Security.Cryptography;

namespace SpellServer
{
    /// <summary>PBKDF2 password hashing with per-user salt.
    /// Format: $PBKDF2$iterations$base64salt$base64hash
    /// Max username: 20 bytes (client packet limit), max password: 20 bytes.</summary>
    public static class PasswordHasher
    {
        private const int SaltSize = 16;       // 128-bit salt
        private const int HashSize = 32;       // 256-bit derived key
        private const int Iterations = 100000; // OWASP recommended minimum
        private const string Prefix = "$PBKDF2$";

        /// <summary>Hash a plaintext password. Returns a storage string.</summary>
        public static string Hash(string password)
        {
            byte[] salt = new byte[SaltSize];
            using (var rng = new RNGCryptoServiceProvider())
                rng.GetBytes(salt);

            byte[] hash = Derive(password, salt, Iterations);

            return $"{Prefix}{Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
        }

        /// <summary>Verify a plaintext password against a stored hash.
        /// Also accepts plaintext passwords for backwards compatibility during migration.</summary>
        public static bool Verify(string password, string storedHash)
        {
            if (string.IsNullOrEmpty(storedHash)) return false;

            // Backwards compatible: if stored value doesn't start with $PBKDF2$,
            // it's a legacy plaintext password — compare directly
            if (!storedHash.StartsWith(Prefix))
                return storedHash == password;

            // Parse: $PBKDF2$iterations$salt$hash
            string[] parts = storedHash.Split('$');
            // parts[0] = "", parts[1] = "PBKDF2", parts[2] = iterations, parts[3] = salt, parts[4] = hash
            if (parts.Length != 5) return false;

            int iterations;
            if (!int.TryParse(parts[2], out iterations)) return false;

            byte[] salt = Convert.FromBase64String(parts[3]);
            byte[] expectedHash = Convert.FromBase64String(parts[4]);
            byte[] actualHash = Derive(password, salt, iterations);

            return ConstantTimeEquals(expectedHash, actualHash);
        }

        /// <summary>Check if a stored password is already hashed.</summary>
        public static bool IsHashed(string storedPassword)
        {
            return !string.IsNullOrEmpty(storedPassword) && storedPassword.StartsWith(Prefix);
        }

        private static byte[] Derive(string password, byte[] salt, int iterations)
        {
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations))
                return pbkdf2.GetBytes(HashSize);
        }

        /// <summary>Constant-time comparison to prevent timing attacks.</summary>
        private static bool ConstantTimeEquals(byte[] a, byte[] b)
        {
            if (a.Length != b.Length) return false;
            int diff = 0;
            for (int i = 0; i < a.Length; i++)
                diff |= a[i] ^ b[i];
            return diff == 0;
        }
    }
}
