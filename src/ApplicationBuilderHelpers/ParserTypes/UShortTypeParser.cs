using ApplicationBuilderHelpers.Abstracts;
using ApplicationBuilderHelpers.Interfaces;
using System;

namespace ApplicationBuilderHelpers.ParserTypes;

internal class UShortTypeParser : CommandTypeParser<ushort>
{
    public override ushort ParseValue(string? value, out string? validateError)
    {
        if (ushort.TryParse(value, out var result))
        {
            validateError = null;
            return result;
        }

        validateError = $"Invalid {Type.Name} value: '{value}'. Expected a valid {Type.Name}.";
        return default;
    }
}
