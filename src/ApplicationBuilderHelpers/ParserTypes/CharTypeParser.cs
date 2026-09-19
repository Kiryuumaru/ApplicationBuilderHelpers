using ApplicationBuilderHelpers.Abstracts;
using ApplicationBuilderHelpers.Interfaces;
using System;

namespace ApplicationBuilderHelpers.ParserTypes;

internal class CharTypeParser : CommandTypeParser<char>
{
    public override char ParseValue(string? value, out string? validateError)
    {
        // No provider overload exists; the single-char grammar has no culture-sensitive elements.
        if (char.TryParse(value, out var result))
        {
            validateError = null;
            return result;
        }

        validateError = $"Invalid {Type.Name} value: '{value}'. Expected a valid {Type.Name}.";
        return default;
    }
}
