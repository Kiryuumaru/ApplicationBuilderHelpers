using ApplicationBuilderHelpers.Abstracts;
using ApplicationBuilderHelpers.Interfaces;
using System;

namespace ApplicationBuilderHelpers.ParserTypes;

internal class DateTimeOffsetTypeParser : CommandTypeParser<DateTimeOffset>
{
    public override DateTimeOffset ParseValue(string? value, out string? validateError)
    {
        if (DateTimeOffset.TryParse(value, out var result))
        {
            validateError = null;
            return result;
        }

        validateError = $"Invalid {Type.Name} value: '{value}'. Expected a valid {Type.Name}.";
        return default;
    }
}
