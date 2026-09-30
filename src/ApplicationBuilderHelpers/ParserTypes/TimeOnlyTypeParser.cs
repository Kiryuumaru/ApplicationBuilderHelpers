using ApplicationBuilderHelpers.Abstracts;
using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Globalization;

namespace ApplicationBuilderHelpers.ParserTypes;

/// <summary>Parses CLI text into a <see cref="TimeOnly"/> (invariant); failure surfaces as InvalidValue, exit 2.</summary>
internal class TimeOnlyTypeParser : CommandTypeParser<TimeOnly>
{
    /// <summary>Converts CLI text to the target value.</summary>
    public override TimeOnly ParseValue(string? value, out string? validateError)
    {
        if (TimeOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var result))
        {
            validateError = null;
            return result;
        }

        validateError = $"Invalid {Type.Name} value: '{value}'. Expected a valid {Type.Name}.";
        return default;
    }
}
