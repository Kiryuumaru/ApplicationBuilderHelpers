using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Diagnostics.CodeAnalysis;
using AbsolutePathHelpers;

namespace ApplicationBuilderHelpers.ParserTypes;

public class AbsolutePathTypeParser : ICommandLineTypeParser
{
    public Type Type => typeof(AbsolutePath);

    public string[] Choices { get; } = [];

    public object? Parse(string? value)
    {
        if (value == null || string.IsNullOrEmpty(value))
        {
            return default(AbsolutePath);
        }
        return AbsolutePath.Create(value!);
    }

    public string? Parse(object? value)
    {
        if (value == null || value is not AbsolutePath)
        {
            return null;
        }
        return value.ToString();
    }

    public bool Validate(string? value, [NotNullWhen(false)] out string? validateError)
    {
        validateError = null;
        if (value == null || string.IsNullOrEmpty(value))
        {
            return true;
        }
        try
        {
            _ = AbsolutePath.Create(value);
        }
        catch
        {
            validateError = "Value must be a path.";
            return false;
        }
        return true;
    }
}
