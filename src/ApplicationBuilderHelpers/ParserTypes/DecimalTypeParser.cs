using ApplicationBuilderHelpers.Abstracts;
using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Globalization;

namespace ApplicationBuilderHelpers.ParserTypes;

/// <summary>Parses CLI text into a <see cref="decimal"/> (invariant, no exponents); failure surfaces as InvalidValue, exit 2.</summary>
internal class DecimalTypeParser : CommandTypeParser<decimal>
{
    /// <summary>Converts CLI text to the target value.</summary>
    public override decimal ParseValue(string? value, out string? validateError)
    {
        if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var result))
        {
            validateError = null;
            return result;
        }

        validateError = $"Invalid {Type.Name} value: '{value}'. Expected a valid {Type.Name}.";
        return default;
    }
}
