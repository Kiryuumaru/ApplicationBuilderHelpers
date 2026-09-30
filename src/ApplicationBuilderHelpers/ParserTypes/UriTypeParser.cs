using ApplicationBuilderHelpers.Abstracts;
using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Linq;

namespace ApplicationBuilderHelpers.ParserTypes;

/// <summary>Parses CLI text into a <see cref="Uri"/> (relative or absolute); failure surfaces as InvalidValue, exit 2.</summary>
internal class UriTypeParser : CommandTypeParser<Uri>
{
    /// <summary>Converts CLI text to the target value.</summary>
    public override Uri? ParseValue(string? value, out string? validateError)
    {
        if (!string.IsNullOrWhiteSpace(value)
            && !value.Any(char.IsWhiteSpace)
            && !value.StartsWith(':')
            && Uri.IsWellFormedUriString(value, UriKind.RelativeOrAbsolute)
            && Uri.TryCreate(value, UriKind.RelativeOrAbsolute, out var result))
        {
            validateError = null;
            return result;
        }

        validateError = $"Invalid {Type.Name} value: '{value}'. Expected a valid {Type.Name}.";
        return default;
    }

    /// <summary>Renders the value as its original text.</summary>
    public override string? GetStringValue(Uri? value)
    {
        return value?.OriginalString;
    }
}
