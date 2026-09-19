using ApplicationBuilderHelpers.Abstracts;
using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Globalization;

namespace ApplicationBuilderHelpers.ParserTypes;

internal class DoubleTypeParser : CommandTypeParser<double>
{
    public override double ParseValue(string? value, out string? validateError)
    {
        if (double.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var result))
        {
            validateError = null;
            return result;
        }

        validateError = $"Invalid {Type.Name} value: '{value}'. Expected a valid {Type.Name}.";
        return default;
    }
}
