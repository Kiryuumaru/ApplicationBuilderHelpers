using System.Text;

namespace Application.Shared.Utilities;

public static class RandomHelpers
{
    private const string AlphanumericCaseSensitiveChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
    private const string AlphanumericCaseInsensitiveChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    public static byte[] ByteArray(int size)
    {
        Random random = new();
        byte[] bytes = new byte[size];
        random.NextBytes(bytes);
        return bytes;
    }

    public static string Alphanumeric(int length, bool caseSensitive = true)
    {
        StringBuilder sb = new();
        Random random = new();

        string chars = caseSensitive ? AlphanumericCaseSensitiveChars : AlphanumericCaseInsensitiveChars;

        for (int i = 0; i < length; i++)
        {
            int index = random.Next(chars.Length);
            sb.Append(chars[index]);
        }

        return sb.ToString();
    }
}
