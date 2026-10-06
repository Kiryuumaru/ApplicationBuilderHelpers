using ApplicationBuilderHelpers.Abstracts;
using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Globalization;

namespace ApplicationBuilderHelpers.ParserTypes;

/// <summary>Parses CLI text into a <see cref="DateTimeOffset"/> (invariant); failure surfaces as InvalidValue, exit 2.</summary>
internal class DateTimeOffsetTypeParser : CommandTypeParser<DateTimeOffset>
{
    /// <summary>Converts CLI text to the target value.</summary>
    public override DateTimeOffset ParseValue(string? value, out string? validateError)
    {
        if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var result))
        {
            validateError = null;
            return result;
        }

        validateError = ParserErrorHints.Shape(Type, value, ParserErrorHints.DateTimeOffsetValue);
        return default;
    }
}
