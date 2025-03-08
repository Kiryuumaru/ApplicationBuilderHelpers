using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Diagnostics.CodeAnalysis;

namespace ApplicationBuilderHelpers.ParserTypes;

public class DateTimeOffsetTypeParser : ICommandLineTypeParser
{
    public Type Type => typeof(DateTimeOffset);

    public string[] Choices { get; } = [];

    public object? Parse(string? value)
    {
        if (value == null || string.IsNullOrEmpty(value))
        {
            return default(DateTimeOffset);
        }
        return DateTimeOffset.Parse(value!);
    }

    public string? Parse(object? value)
    {
        if (value == null || value is not DateTimeOffset)
        {
            return default(DateTimeOffset).ToString();
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
        if (!DateTimeOffset.TryParse(value, out DateTimeOffset _))
        {
            validateError = "Value must be a DateTimeOffset.";
            return false;
        }
        return true;
    }
}
