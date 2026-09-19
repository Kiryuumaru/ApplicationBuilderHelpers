using ApplicationBuilderHelpers.Abstracts;
using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Globalization;

namespace ApplicationBuilderHelpers.ParserTypes;

internal class FloatTypeParser : CommandTypeParser<float>
{
    public override float ParseValue(string? value, out string? validateError)
    {
        if (float.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var result))
        {
            validateError = null;
            return result;
        }

        validateError = $"Invalid {Type.Name} value: '{value}'. Expected a valid {Type.Name}.";
        return default;
    }
}
