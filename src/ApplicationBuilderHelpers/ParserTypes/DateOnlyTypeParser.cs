using ApplicationBuilderHelpers.Abstracts;
using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Globalization;

namespace ApplicationBuilderHelpers.ParserTypes;

/// <summary>Parses CLI text into a <see cref="DateOnly"/> (invariant); failure surfaces as InvalidValue, exit 2.</summary>
internal class DateOnlyTypeParser : CommandTypeParser<DateOnly>
{
    /// <summary>Converts CLI text to the target value.</summary>
    public override DateOnly ParseValue(string? value, out string? validateError)
    {
        if (DateOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var result))
        {
            validateError = null;
            return result;
        }

        validateError = ParserErrorHints.Shape(Type, value, ParserErrorHints.DateOnlyValue);
        return default;
    }
}
