using System.Text;

namespace PBJ.Networking
{
    // M3: shared player display-name sanitizer for online play (see
    // Assets/Playground/Abim/M3_OnlineRelay/PLAN.md). One shared implementation
    // used by UI entry, session property write, and the server-side read
    // (the server never trusts raw client strings).
    // Deliberately dependency-light: plain C#, no NGO / NetworkManager /
    // Unity scene / prefab / Relay references, so it stays reusable and
    // trivially testable. Cosmetic only - NOT an identity system.
    public static class PlayerNameValidator
    {
        // Hard cap keeps the result FixedString64Bytes-safe (16 chars max).
        public const int MaxLength = 16;

        // Sanitizes a raw display name: strips control characters, trims,
        // collapses internal whitespace runs to single spaces, caps length.
        // Empty/invalid input yields the fallback name "Player####" derived
        // from the clientId (cosmetic only, e.g. Player0042).
        public static string Sanitize(string rawName, ulong clientId)
        {
            string sanitized = Sanitize(rawName);
            if (sanitized.Length == 0)
            {
                return GetFallbackName(clientId);
            }
            return sanitized;
        }

        // Sanitize without fallback: returns the cleaned name, possibly empty.
        // Useful when the caller wants to decide on fallback itself.
        public static string Sanitize(string rawName)
        {
            if (string.IsNullOrEmpty(rawName))
            {
                return string.Empty;
            }

            var builder = new StringBuilder(rawName.Length);
            bool pendingWhitespace = false;
            for (int i = 0; i < rawName.Length; i++)
            {
                char c = rawName[i];
                if (char.IsControl(c) && !char.IsWhiteSpace(c))
                {
                    continue; // strip control characters entirely
                }
                if (char.IsWhiteSpace(c))
                {
                    // tab/newline (also control chars) collapse like spaces;
                    // whitespace must be handled before the control strip.
                    pendingWhitespace = true;
                    continue;
                }
                if (builder.Length > 0 && pendingWhitespace)
                {
                    builder.Append(' ');
                    if (builder.Length >= MaxLength)
                    {
                        break; // hard cap; remainder is dropped
                    }
                }
                pendingWhitespace = false;
                builder.Append(c);
                if (builder.Length >= MaxLength)
                {
                    break; // hard cap; remainder is dropped
                }
            }

            if (builder.Length == 0)
            {
                return string.Empty;
            }

            string result = builder.ToString();
            // A whitespace cap boundary could leave a trailing space.
            return result.EndsWith(" ") ? result.TrimEnd() : result;
        }

        // Fallback display name derived from clientId (cosmetic only).
        public static string GetFallbackName(ulong clientId)
        {
            ulong suffix = clientId % 10000UL;
            return $"Player{suffix:D4}";
        }
    }
}
