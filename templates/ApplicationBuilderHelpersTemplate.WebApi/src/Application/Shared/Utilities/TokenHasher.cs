using System.Security.Cryptography;

namespace Application.Shared.Utilities;

/// <summary>
/// Provides token hashing functionality for secure token storage.
/// Single source of truth for token hashing algorithm.
/// </summary>
public static class TokenHasher
{
    /// <summary>
    /// Computes a secure hash of the given token.
    /// Uses SHA-256 and returns a base64-encoded string.
    /// </summary>
    /// <param name="token">The token to hash.</param>
    /// <returns>Base64-encoded hash of the token.</returns>
    public static string Hash(string token)
    {
        ArgumentNullException.ThrowIfNull(token);
        var bytes = System.Text.Encoding.UTF8.GetBytes(token);
        var hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }
}
