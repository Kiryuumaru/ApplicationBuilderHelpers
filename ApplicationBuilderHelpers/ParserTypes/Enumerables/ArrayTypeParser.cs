using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Diagnostics.CodeAnalysis;

namespace ApplicationBuilderHelpers.ParserTypes.Enumerables;

public class ArrayTypeParser(Type elementType, ICommandArgsTypeParser itemTypeParser) : ICommandArgsTypeParser
{
    public Type Type => throw new NotImplementedException();

    public string[] Choices { get; } = [];

    public object? ParseToType(object? value)
    {
        if (value == null || value is not string[] valueArr || valueArr.Length == 0)
        {
            return null;
        }
        var arrInstance = Array.CreateInstance(elementType, valueArr.Length);
        for (int i = 0; i < valueArr.Length; i++)
        {
            arrInstance.SetValue(itemTypeParser.ParseToType(valueArr[i]), i);
        }
        return arrInstance;
    }

    public object? ParseFromType(object? value)
    {
        if (value == null || value is not Array valueArr || valueArr.Length == 0)
        {
            return null;
        }
        var arrInstance = new string[valueArr.Length];
        for (int i = 0; i < valueArr.Length; i++)
        {
            arrInstance[i] = itemTypeParser.ParseFromType(valueArr.GetValue(i)).ToString();
        }
        return arrInstance;
    }

    public bool Validate(object? value, [NotNullWhen(false)] out string? validateError)
    {
        validateError = null;
        if (value == null || value is not Array valueArr || valueArr.Length == 0)
        {
            return true;
        }
        for (int i = 0; i < valueArr.Length; i++)
        {
            if (!itemTypeParser.Validate(valueArr.GetValue(i), out validateError))
            {
                return false;
            }
        }
        return true;
    }
}
