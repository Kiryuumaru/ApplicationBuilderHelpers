using ApplicationBuilderHelpers.Interfaces;
using System;
using AbsolutePathHelpers;

namespace ApplicationBuilderHelpers.ParserTypes;

public class AbsolutePathTypeParser : ICommandTypeParser
{
    public Type Type => typeof(AbsolutePath);

    public object? Parse(string? value, out string? validateError)
    {
        if (AbsolutePath.TryParse(value, out var result))
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
