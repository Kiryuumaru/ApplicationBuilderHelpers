using ApplicationBuilderHelpers.Abstracts;
using ApplicationBuilderHelpers.Interfaces;
using System;

namespace ApplicationBuilderHelpers.ParserTypes;

internal class ULongTypeParser : CommandTypeParser<ulong>
{
    public override ulong ParseValue(string? value, out string? validateError)
    {
        if (ulong.TryParse(value, out var result))
        {
            validateError = null;
            return result;
        }

        validateError = $"Invalid {Type.Name} value: '{value}'. Expected a valid {Type.Name}.";
        return default;
    }
}
