using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Diagnostics.CodeAnalysis;

namespace ApplicationBuilderHelpers.ParserTypes;

public class UShortTypeParser : ICommandLineTypeParser
{
    public Type Type => typeof(ushort);

    public string[] Choices { get; } = [];

    public object? Parse(string? value)
    {
        if (value == null || string.IsNullOrEmpty(value))
        {
            return default(ushort);
        }
        return ushort.Parse(value!);
    }

    public string? Parse(object? value)
    {
        if (value == null || value is not ushort)
        {
            return default(ushort).ToString();
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
        if (!ushort.TryParse(value, out ushort _))
        {
            validateError = "Value must be a ushort.";
            return false;
        }
        return true;
    }
}
