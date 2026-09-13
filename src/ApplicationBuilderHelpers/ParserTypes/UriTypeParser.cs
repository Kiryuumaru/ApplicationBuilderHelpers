using ApplicationBuilderHelpers.Abstracts;
using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Linq;

namespace ApplicationBuilderHelpers.ParserTypes;

internal class UriTypeParser : CommandTypeParser<Uri>
{
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

    public override string? GetStringValue(Uri? value)
    {
        return value?.OriginalString;
    }
}
