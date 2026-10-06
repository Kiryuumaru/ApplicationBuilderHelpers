using ApplicationBuilderHelpers.Abstracts;
using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Globalization;

namespace ApplicationBuilderHelpers.ParserTypes;

/// <summary>Parses CLI text into an <see cref="int"/> (invariant, range-checked); failure surfaces as InvalidValue, exit 2.</summary>
internal class IntTypeParser : CommandTypeParser<int>
{
    /// <summary>Converts CLI text to the target value.</summary>
    public override int ParseValue(string? value, out string? validateError)
    {
        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
        {
            validateError = null;
            return result;
        }

        string hint = ParserErrorHints.WholeNumberRange(int.MinValue, int.MaxValue);
        validateError = ParserErrorHints.IsWholeNumber(value)
            ? ParserErrorHints.OutOfRange(Type, value, hint)
            : ParserErrorHints.Shape(Type, value, hint);
        return default;
    }
}
