using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Diagnostics.CodeAnalysis;

namespace ApplicationBuilderHelpers.ParserTypes;

public class UIntTypeParser : ICommandLineTypeParser
{
    public Type Type => typeof(uint);

    public string[] Choices { get; } = [];

    public object? Parse(string? value)
    {
        if (value == null || string.IsNullOrEmpty(value))
        {
            return default(uint);
        }
        return uint.Parse(value!);
    }

    public string? Parse(object? value)
    {
        if (value == null || value is not uint)
        {
            return default(uint).ToString();
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
        if (!uint.TryParse(value, out uint _))
        {
            validateError = "Value must be a uint.";
            return false;
        }
        return true;
    }
}
