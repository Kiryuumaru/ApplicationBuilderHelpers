using ApplicationBuilderHelpers.Abstracts;
using ApplicationBuilderHelpers.Interfaces;
using System;

namespace ApplicationBuilderHelpers.ParserTypes;

internal class UIntTypeParser : CommandTypeParser<uint>
{
    public override uint ParseValue(string? value, out string? validateError)
    {
        if (uint.TryParse(value, out var result))
        {
            validateError = null;
            return result;
        }

        validateError = $"Invalid {Type.Name} value: '{value}'. Expected a valid {Type.Name}.";
        return default;
    }
}
