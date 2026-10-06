using ApplicationBuilderHelpers.Abstracts;
using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Globalization;

namespace ApplicationBuilderHelpers.ParserTypes;

/// <summary>Parses CLI text into a <see cref="DateTime"/> (invariant); failure surfaces as InvalidValue, exit 2.</summary>
internal class DateTimeTypeParser : CommandTypeParser<DateTime>
{
    /// <summary>Converts CLI text to the target value.</summary>
    public override DateTime ParseValue(string? value, out string? validateError)
    {
        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var result))
        {
            validateError = null;
            return result;
        }

        validateError = ParserErrorHints.Shape(Type, value, ParserErrorHints.DateTimeValue);
        return default;
    }
}
