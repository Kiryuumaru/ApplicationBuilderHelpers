using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Diagnostics.CodeAnalysis;

namespace ApplicationBuilderHelpers.ParserTypes;

public class LongTypeParser : ICommandLineTypeParser
{
    public Type Type => typeof(long);

    public string[] Choices { get; } = [];

    public object? Parse(string? value)
    {
        if (value == null || string.IsNullOrEmpty(value))
        {
            return default(long);
        }
        return long.Parse(value!);
    }

    public string? Parse(object? value)
    {
        if (value == null || value is not long)
        {
            return default(long).ToString();
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
        if (!long.TryParse(value, out long _))
        {
            validateError = "Value must be a long.";
            return false;
        }
        return true;
    }
}
