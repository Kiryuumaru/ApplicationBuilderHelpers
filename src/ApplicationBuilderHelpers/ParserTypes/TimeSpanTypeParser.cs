using ApplicationBuilderHelpers.Abstracts;
using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Globalization;

namespace ApplicationBuilderHelpers.ParserTypes;

internal class TimeSpanTypeParser : CommandTypeParser<TimeSpan>
{
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
