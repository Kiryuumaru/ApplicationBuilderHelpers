using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Diagnostics.CodeAnalysis;
using AbsolutePathHelpers;

namespace ApplicationBuilderHelpers.ParserTypes;

public class AbsolutePathTypeParser : ICommandTypeParser
{
    public Type Type => typeof(AbsolutePath);

    public object? Parse(string? value, out string? validateError)
    {
        var valueStr = value?.ToString();
        if (valueStr == null || string.IsNullOrEmpty(valueStr))
        {
            validateError = null;
            return default(AbsolutePath);
        }

        validateError = $"Invalid {Type.Name} value: '{value}'. Expected a valid {Type.Name}.";
        return null;
    }

    public string? GetString(object? value)
    {
        return value?.ToString();
    }
}
