using ApplicationBuilderHelpers.Abstracts;
using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Globalization;

namespace ApplicationBuilderHelpers.ParserTypes;

/// <summary>Parses CLI text into a <see cref="TimeSpan"/> (invariant); failure surfaces as InvalidValue, exit 2.</summary>
internal class TimeSpanTypeParser : CommandTypeParser<TimeSpan>
{
    /// <summary>Converts CLI text to the target value.</summary>
    public override TimeSpan ParseValue(string? value, out string? validateError)
    {
        if (TimeSpan.TryParse(value, CultureInfo.InvariantCulture, out var result))
        {
            validateError = null;
            return result;
        }

        validateError = $"Invalid {Type.Name} value: '{value}'. Expected a valid {Type.Name}.";
        return default;
    }
}
