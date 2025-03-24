using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Diagnostics.CodeAnalysis;

namespace ApplicationBuilderHelpers.ParserTypes.Enumerables;

public class DictionaryTypeParser(Type itemType) : ICommandLineTypeParser
{
    public Type Type => throw new NotImplementedException();

    public string[] Choices { get; } = [];

    public object? ParseToType(object? value)
    {
        var valueStr = value?.ToString();
        if (valueStr == null || string.IsNullOrEmpty(valueStr))
        {
            return default(bool);
        }
        return bool.Parse(valueStr);
    }

    public object? ParseFromType(object? value)
    {
        if (value == null || value is not bool)
        {
            return default(bool).ToString();
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
        if (!bool.TryParse(valueStr, out bool _))
        {
            validateError = "Value must be a bool.";
            return false;
        }
        return true;
    }
}
