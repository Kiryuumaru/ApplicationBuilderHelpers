using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Diagnostics.CodeAnalysis;
using AbsolutePathHelpers;

namespace ApplicationBuilderHelpers.ParserTypes;

public class AbsolutePathTypeParser : ICommandArgsTypeParser
{
    public Type Type => typeof(AbsolutePath);

    public string[] Choices { get; } = [];

    public object? ParseToType(object? value)
    {
        var valueStr = value?.ToString();
        if (valueStr == null || string.IsNullOrEmpty(valueStr))
        {
            return default(AbsolutePath);
        }
        return AbsolutePath.Create(valueStr);
    }

    public object? ParseFromType(object? value)
    {
        if (value == null || value is not AbsolutePath)
        {
            return null;
        }
        return value.ToString();
    }

    public bool Validate(object? value, [NotNullWhen(false)] out string? validateError)
    {
        validateError = null;
        if (value == null || value is not string valueStr || string.IsNullOrEmpty(valueStr))
        {
            return true;
        }
        try
        {
            _ = AbsolutePath.Create(valueStr);
        }
        catch
        {
            validateError = "Value must be a path.";
            return false;
        }
        return true;
    }
}
