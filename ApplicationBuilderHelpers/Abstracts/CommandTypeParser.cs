using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApplicationBuilderHelpers.Abstracts;

/// <summary>
/// This is the base class for command type parsers.
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class CommandTypeParser<T> : ICommandTypeParser
{
    public Type Type { get; } = typeof(T);

    public object? Parse(string? value, out string? validateError)
    {
        return ParseValue(value, out validateError);
    }

    public string? GetString(object? value)
    {
        if (value is null)
        {
            return GetStringValue(default);
        }
        else if (value is T typedValue)
        {
            return GetStringValue(typedValue);
        }
        else
        {
            return value.ToString();
        }
    }

    public object? GetDefaultValue()
    {
        return default(T);
    }

    public Array CreateTypedArray(int length)
    {
        return new T[length];
    }

    public virtual string? GetStringValue(T? value)
    {
        return value?.ToString();
    }

    public abstract T? ParseValue(string? value, out string? validateError);
}
