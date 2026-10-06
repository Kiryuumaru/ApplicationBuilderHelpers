using ApplicationBuilderHelpers.Abstracts;
using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Globalization;

namespace ApplicationBuilderHelpers.ParserTypes;

/// <summary>Parses CLI text into a <see cref="short"/> (invariant, range-checked); failure surfaces as InvalidValue, exit 2.</summary>
internal class ShortTypeParser : CommandTypeParser<short>
{
    /// <summary>Converts CLI text to the target value.</summary>
    public override short ParseValue(string? value, out string? validateError)
    {
        if (short.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
        {
            validateError = null;
            return result;
        }

        string hint = ParserErrorHints.WholeNumberRange(short.MinValue, short.MaxValue);
        validateError = ParserErrorHints.IsWholeNumber(value)
            ? ParserErrorHints.OutOfRange(Type, value, hint)
            : ParserErrorHints.Shape(Type, value, hint);
        return default;
    }
}
