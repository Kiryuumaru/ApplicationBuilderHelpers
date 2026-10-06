using ApplicationBuilderHelpers.Abstracts;
using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Globalization;

namespace ApplicationBuilderHelpers.ParserTypes;

/// <summary>Parses CLI text into a <see cref="ushort"/> (invariant, range-checked); failure surfaces as InvalidValue, exit 2.</summary>
internal class UShortTypeParser : CommandTypeParser<ushort>
{
    /// <summary>Converts CLI text to the target value.</summary>
    public override ushort ParseValue(string? value, out string? validateError)
    {
        if (ushort.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
        {
            validateError = null;
            return result;
        }

        string hint = ParserErrorHints.WholeNumberRange(ushort.MinValue, ushort.MaxValue);
        validateError = ParserErrorHints.IsWholeNumber(value)
            ? ParserErrorHints.OutOfRange(Type, value, hint)
            : ParserErrorHints.Shape(Type, value, hint);
        return default;
    }
}
