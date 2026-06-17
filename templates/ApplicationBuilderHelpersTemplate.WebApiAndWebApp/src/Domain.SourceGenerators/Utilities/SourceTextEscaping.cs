using System;
using System.Text;

namespace Domain.SourceGenerators.Utilities;

internal static class SourceTextEscaping
{
    public static string EscapeForStringLiteral(string value)
    {
        if (value is null)
        {
            return string.Empty;
        }

        return value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"");
    }

    public static string EscapeForXmlDoc(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(value.Length);
        foreach (var ch in value)
        {
            builder.Append(ch switch
            {
                '<' => "&lt;",
                '>' => "&gt;",
                '&' => "&amp;",
                '"' => "&quot;",
                '\'' => "&apos;",
                _ => ch.ToString()
            });
        }

        return builder.ToString();
    }
}
