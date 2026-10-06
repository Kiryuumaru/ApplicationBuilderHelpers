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

        if (decimal.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out _))
        {
            validateError = ParserErrorHints.Shape(Type, value, ParserErrorHints.DecimalWithoutExponent);
        }
        else if (ParserErrorHints.IsNumeric(value))
        {
            validateError = ParserErrorHints.OutOfRange(Type, value, ParserErrorHints.DecimalNumber);
        }
        else
        {
            validateError = ParserErrorHints.Shape(Type, value, ParserErrorHints.DecimalNumber);
        }
        return default;
    }
}
