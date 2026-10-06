using ApplicationBuilderHelpers.Abstracts;
using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Globalization;

namespace ApplicationBuilderHelpers.ParserTypes;

/// <summary>Parses CLI text into a <see cref="double"/> (invariant); failure surfaces as InvalidValue, exit 2.</summary>
internal class DoubleTypeParser : CommandTypeParser<double>
{
    /// <summary>Converts CLI text to the target value.</summary>
    public override double ParseValue(string? value, out string? validateError)
    {
        if (double.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var result))
        {
            validateError = null;
            return result;
        }

        validateError = ParserErrorHints.Shape(Type, value, ParserErrorHints.FloatingPointNumber);
        return default;
    }
}
