using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Diagnostics.CodeAnalysis;

namespace ApplicationBuilderHelpers.ParserTypes;

public class SByteTypeParser : ICommandLineTypeParser
{
    public Type Type => typeof(sbyte);

    public string[] Choices { get; } = [];

    public object? Parse(string? value)
    {
        if (value == null || string.IsNullOrEmpty(value))
        {
            return default(sbyte);
        }
        return sbyte.Parse(value!);
    }

    public string? Parse(object? value)
    {
        if (value == null || value is not sbyte)
        {
            return default(sbyte).ToString();
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
        if (!sbyte.TryParse(value, out sbyte _))
        {
            validateError = "Value must be a sbyte.";
            return false;
        }
        return true;
    }
}
