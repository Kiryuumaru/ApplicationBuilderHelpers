using ApplicationBuilderHelpers.Abstracts;
using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Globalization;

namespace ApplicationBuilderHelpers.ParserTypes;

internal class SByteTypeParser : CommandTypeParser<sbyte>
{
    public override sbyte ParseValue(string? value, out string? validateError)
    {
        if (sbyte.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
        {
            validateError = null;
            return result;
        }

        validateError = $"Invalid {Type.Name} value: '{value}'. Expected a valid {Type.Name}.";
        return default;
    }
}
