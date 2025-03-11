using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Diagnostics.CodeAnalysis;

namespace ApplicationBuilderHelpers.ParserTypes;

public class ByteTypeParser : ICommandLineTypeParser
{
    public Type Type => typeof(byte);

    public string[] Choices { get; } = [];

    public object? ParseToType(object? value)
    {
        var valueStr = value?.ToString();
        if (valueStr == null || string.IsNullOrEmpty(valueStr))
        {
            return default(byte);
        }
        return byte.Parse(valueStr);
    }

    public object? ParseFromType(object? value)
    {
        if (value == null || value is not byte)
        {
            return default(byte).ToString();
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
        if (!byte.TryParse(value, out byte _))
        {
            validateError = "Value must be a byte.";
            return false;
        }
        return true;
    }
}
