using System;
using System.Text;

namespace SpellServer
{
    /// <summary>Input sanitization for data arriving from the game client.
    /// All strings from the wire pass through here before touching the DB or being relayed.
    /// Toggle with InputSanitizer.Enabled (default: true). Disable for protocol debugging.</summary>
    public static class InputSanitizer
    {
        /// <summary>Master toggle. Set to false to bypass all sanitization (protocol debugging).</summary>
        public static bool Enabled = true;

        /// <summary>Max field sizes matching the client packet format.</summary>
        public const int MaxUsername = 20;
        public const int MaxPassword = 20;
        public const int MaxCharName = 20;
        public const int MaxChatMessage = 128;
        public const int MaxCabalName = 20;
        public const int MaxCabalTag = 4;
        public const int MaxSerial = 32;

        /// <summary>Strip non-printable ASCII and enforce length limit.
        /// Allows space (0x20) through tilde (0x7E).</summary>
        public static string SanitizeString(string input, int maxLength)
        {
            if (input == null) return "";
            if (!Enabled) return input;

            var sb = new StringBuilder(Math.Min(input.Length, maxLength));
            for (int i = 0; i < input.Length && sb.Length < maxLength; i++)
            {
                char c = input[i];
                if (c >= 0x20 && c <= 0x7E)
                    sb.Append(c);
            }
            return sb.ToString();
        }

        /// <summary>Sanitize username — printable ASCII, no spaces, max 20.</summary>
        public static string SanitizeUsername(string input)
        {
            if (input == null) return "";
            if (!Enabled) return input;

            var sb = new StringBuilder(Math.Min(input.Length, MaxUsername));
            for (int i = 0; i < input.Length && sb.Length < MaxUsername; i++)
            {
                char c = input[i];
                // Alphanumeric + basic punctuation, no spaces
                if (c >= 0x21 && c <= 0x7E)
                    sb.Append(c);
            }
            return sb.ToString();
        }

        /// <summary>Sanitize chat message — printable ASCII with spaces, max 128.</summary>
        public static string SanitizeChat(string input)
        {
            return SanitizeString(input, MaxChatMessage);
        }

        /// <summary>Sanitize character name — printable ASCII with spaces, max 20.</summary>
        public static string SanitizeCharName(string input)
        {
            return SanitizeString(input, MaxCharName);
        }

        /// <summary>Validate a packet length field is within expected bounds.</summary>
        public static bool ValidateLength(int length, int min, int max)
        {
            if (!Enabled) return true;
            return length >= min && length <= max;
        }
    }
}
