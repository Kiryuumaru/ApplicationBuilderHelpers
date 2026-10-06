using ApplicationBuilderHelpers.Abstracts;
using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Globalization;

namespace ApplicationBuilderHelpers.ParserTypes;

/// <summary>Parses CLI text into a <see cref="float"/> (invariant); failure surfaces as InvalidValue, exit 2.</summary>
internal class FloatTypeParser : CommandTypeParser<float>
{
    /// <summary>Converts CLI text to the target value.</summary>
    public override float ParseValue(string? value, out string? validateError)
    {
        if (float.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var result))
        {
            validateError = null;
            return result;
        }

        validateError = ParserErrorHints.Shape(Type, value, ParserErrorHints.FloatingPointNumber);
        return default;
    }
}
