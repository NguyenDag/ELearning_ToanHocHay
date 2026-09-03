using System.Security.Cryptography;

namespace ELearning_ToanHocHay_Control.Services.Helpers
{
    /// <summary>Opaque token generation + hashing for refresh / password-reset tokens.</summary>
    public static class SecureTokens
    {
        /// <summary>A URL-safe random token (256 bits of entropy).</summary>
        public static string NewToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(32);
            return Convert.ToBase64String(bytes)
                .Replace('+', '-').Replace('/', '_').TrimEnd('=');
        }

        /// <summary>SHA-256, base64 — what we store so a DB leak does not expose usable tokens.</summary>
        public static string Hash(string raw)
        {
            var bytes = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(raw));
            return Convert.ToBase64String(bytes);
        }
    }
}
