using ApplicationBuilderHelpers.Interfaces;
using System;

namespace ApplicationBuilderHelpers.ParserTypes;

internal class DateTimeOffsetTypeParser : ICommandTypeParser
{
    public Type Type => typeof(DateTimeOffset);

    public object? Parse(string? value, out string? validateError)
    {
        if (DateTimeOffset.TryParse(value, out var result))
        {
            validateError = null;
            return result;
        }

        validateError = $"Invalid {Type.Name} value: '{value}'. Expected a valid {Type.Name}.";
        return null;
    }

    public string? GetString(object? value)
    {
        return value?.ToString();
    }
}
