using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Domain.SourceGenerators.Utilities;

internal static class IdentifierNaming
{
    private static readonly HashSet<string> ReservedKeywords = new(StringComparer.Ordinal)
    {
        "abstract","as","base","bool","break","byte","case","catch","char","checked","class","const","continue","decimal","default","delegate","do","double","else","enum","event","explicit","extern","false","finally","fixed","float","for","foreach","goto","if","implicit","in","int","interface","internal","is","lock","long","namespace","new","null","object","operator","out","override","params","private","protected","public","readonly","ref","return","sbyte","sealed","short","sizeof","stackalloc","static","string","struct","switch","this","throw","true","try","typeof","uint","ulong","unchecked","unsafe","ushort","using","virtual","void","volatile","while"
    };

    public static string ToPermissionClassName(string identifier, bool parentHasReadScope, bool parentHasWriteScope)
    {
        var raw = ToIdentifier(identifier);

        if (parentHasReadScope && string.Equals(raw, "Read", StringComparison.Ordinal))
        {
            return "ReadPermission";
        }

        if (parentHasWriteScope && string.Equals(raw, "Write", StringComparison.Ordinal))
        {
            return "WritePermission";
        }

        return raw;
    }

    public static string ToRoleClassName(string code) => ToIdentifier(code);

    public static string ToParameterIdentifier(string parameterName) => ToIdentifier(parameterName);

    public static string ToIdentifier(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return "_";
        }

        if (string.Equals(input, "_read", StringComparison.Ordinal))
        {
            return "Read";
        }

        if (string.Equals(input, "_write", StringComparison.Ordinal))
        {
            return "Write";
        }

        var trimmed = input.Trim();

        var builder = new StringBuilder(trimmed.Length);
        var makeUpper = true;

        for (var i = 0; i < trimmed.Length; i++)
        {
            var ch = trimmed[i];

            if (!char.IsLetterOrDigit(ch))
            {
                makeUpper = true;
                continue;
            }

            if (builder.Length == 0)
            {
                if (char.IsDigit(ch))
                {
                    builder.Append('_');
                    builder.Append(ch);
                    makeUpper = true;
                    continue;
                }

                builder.Append(char.ToUpperInvariant(ch));
                makeUpper = false;
                continue;
            }

            if (makeUpper)
            {
                builder.Append(char.ToUpperInvariant(ch));
                makeUpper = false;
                continue;
            }

            // If we transition from digit -> letter, capitalize for readability.
            var prev = builder[builder.Length - 1];
            if (char.IsDigit(prev) && char.IsLetter(ch))
            {
                builder.Append(char.ToUpperInvariant(ch));
                continue;
            }

            builder.Append(ch);
        }

        var result = builder.Length == 0 ? "_" : builder.ToString();

        // Avoid producing a C# keyword. This should be rare given PascalCase output.
        if (ReservedKeywords.Contains(result.ToLower(CultureInfo.InvariantCulture)))
        {
            return result + "Value";
        }

        return result;
    }
}
