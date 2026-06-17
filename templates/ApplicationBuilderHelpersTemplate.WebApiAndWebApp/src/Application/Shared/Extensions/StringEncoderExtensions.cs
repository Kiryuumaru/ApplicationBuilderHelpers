using System.Text;

namespace Application.Shared.Extensions;

public static class StringEncoderExtensions
{
    public static string Encode(this string value)
    {
        string enc = Convert.ToBase64String(Encoding.ASCII.GetBytes(value));
        enc = enc.Replace("/", "_");
        enc = enc.Replace("+", "-");
        return enc;
    }

    public static string Decode(this string encoded)
    {
        encoded = encoded.Replace("_", "/");
        encoded = encoded.Replace("-", "+");
        byte[] buffer = Convert.FromBase64String(encoded);
        return Encoding.ASCII.GetString(buffer);
    }
}
