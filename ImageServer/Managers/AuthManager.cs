using System;
using System.Collections.Generic;
using System.Text;

namespace ImageServer.Managers
{
    /// <summary>
/// Handles user authentication.
/// </summary>
    public class AuthManager
    {
        private readonly Dictionary<string, string> _credentials = new(StringComparer.OrdinalIgnoreCase)
        {
            ["admin"] = "password123",
            ["trent"] = "password123",
            ["demo"] = "demo123"
        };

        public bool TryValidateCredentials(byte[] payload, out string username)
        {
            username = string.Empty;

            string raw = Encoding.UTF8.GetString(payload ?? Array.Empty<byte>());
            string[] parts = raw.Split(':', 2, StringSplitOptions.TrimEntries);

            if (parts.Length != 2)
            {
                return false;
            }

            username = parts[0];

            return _credentials.TryGetValue(parts[0], out string? expectedPassword)
                && expectedPassword == parts[1];
        }
    }
}