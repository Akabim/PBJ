using System;

namespace PBJ.Networking
{
    // M3 LAN discovery: shared 4-character room-code definition (see
    // Assets/Playground/Abim/M3_OnlineRelay/PLAN.md). The HOST generates the
    // code and advertises it over UDP (LanRoomBroadcaster); clients compare
    // codes case-insensitively. Deliberately dependency-light plain C# (same
    // approach as PlayerNameValidator) so it stays reusable and trivially
    // testable. No UI, no NGO, no socket code lives here.
    public static class RoomCode
    {
        // Every PBJ room code is exactly 4 characters.
        public const int Length = 4;

        // Uppercase A-Z / 2-9 with visually ambiguous characters removed:
        // 'I' (reads like 1/l) and 'O' (reads like 0). '0' and '1' are already
        // excluded by the 2-9 rule. 24 letters + 8 digits = 32 symbols.
        public const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

        private static readonly object _rngLock = new object();
        private static readonly Random _rng = new Random();

        // Generates a new random room code (host side). Always canonical
        // uppercase from Alphabet, so IsValid(Generate()) is always true.
        public static string Generate()
        {
            var chars = new char[Length];
            lock (_rngLock)
            {
                for (int i = 0; i < Length; i++)
                {
                    chars[i] = Alphabet[_rng.Next(Alphabet.Length)];
                }
            }
            return new string(chars);
        }

        // Strict validation: exactly 4 characters, all canonical uppercase
        // Alphabet characters. Used by the packet contract so a malformed or
        // ambiguous code never enters the discovery layer.
        public static bool IsValid(string code)
        {
            if (string.IsNullOrEmpty(code) || code.Length != Length)
            {
                return false;
            }
            for (int i = 0; i < Length; i++)
            {
                if (Alphabet.IndexOf(code[i]) < 0)
                {
                    return false;
                }
            }
            return true;
        }

        // Normalizes user input for comparison: trims whitespace and applies
        // invariant uppercase. Invalid/unknown characters are NOT corrected
        // here; use IsValid on the result when strictness is required.
        public static string Normalize(string code)
        {
            if (string.IsNullOrEmpty(code))
            {
                return string.Empty;
            }
            return code.Trim().ToUpperInvariant();
        }

        // Case-insensitive room-code comparison (per discovery contract):
        // both codes are trimmed + uppercased, then compared ordinally.
        // Empty/whitespace codes never match anything.
        public static bool Matches(string a, string b)
        {
            string na = Normalize(a);
            string nb = Normalize(b);
            if (na.Length == 0 || nb.Length == 0)
            {
                return false;
            }
            return string.Equals(na, nb, StringComparison.Ordinal);
        }
    }
}
