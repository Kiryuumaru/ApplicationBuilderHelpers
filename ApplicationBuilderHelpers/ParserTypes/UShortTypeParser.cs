using ApplicationBuilderHelpers.Interfaces;
using System;

namespace ApplicationBuilderHelpers.ParserTypes;

internal class UShortTypeParser : ICommandTypeParser
{
    public Type Type => typeof(ushort);

    public object? Parse(string? value, out string? validateError)
    {
        if (ushort.TryParse(value, out var result))
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
