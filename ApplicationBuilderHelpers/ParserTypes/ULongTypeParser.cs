using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Diagnostics.CodeAnalysis;

namespace ApplicationBuilderHelpers.ParserTypes;

public class ULongTypeParser : ICommandLineTypeParser
{
    public Type Type => typeof(ulong);

    public string[] Choices { get; } = [];

    public object? Parse(string? value)
    {
        if (value == null || string.IsNullOrEmpty(value))
        {
            return default(ulong);
        }
        return ulong.Parse(value!);
    }

    public string? Parse(object? value)
    {
        if (value == null || value is not ulong)
        {
            return default(ulong).ToString();
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
        if (!ulong.TryParse(value, out ulong _))
        {
            validateError = "Value must be a ulong.";
            return false;
        }
        return true;
    }
}
