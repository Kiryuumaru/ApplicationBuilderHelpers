using System.Security.Cryptography;

namespace Application.Shared.Utilities;

public static class TokenHasher
{
    public static string Hash(string token)
    {
        ArgumentNullException.ThrowIfNull(token);
        var bytes = System.Text.Encoding.UTF8.GetBytes(token);
        var hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }
}
