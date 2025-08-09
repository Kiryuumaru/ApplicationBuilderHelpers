using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApplicationBuilderHelpers.Abstracts;

/// <summary>
/// Provides a base implementation for command type parsers that handle specific types.
/// </summary>
/// <typeparam name="T">The type that this parser handles.</typeparam>
public abstract class CommandTypeParser<T> : ICommandTypeParser
{
    /// <summary>
    /// Gets the type that this parser handles.
    /// </summary>
    public Type Type { get; } = typeof(T);

    /// <summary>
    /// Parses a string value into the target type.
    /// </summary>
    /// <param name="value">The string value to parse.</param>
    /// <param name="validateError">When this method returns, contains the validation error message if parsing failed; otherwise, null.</param>
    /// <returns>The parsed object if successful; otherwise, null.</returns>
    public object? Parse(string? value, out string? validateError)
    {
        return ParseValue(value, out validateError);
    }

    /// <summary>
    /// Converts an object to its string representation for command-line display.
    /// </summary>
    /// <param name="value">The object to convert to a string.</param>
    /// <returns>The string representation of the object, or null if the object is null.</returns>
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

    /// <summary>
    /// Gets the default value for the target type.
    /// </summary>
    /// <returns>The default value for type T.</returns>
    public object? GetDefaultValue()
    {
        return default(T);
    }

    /// <summary>
    /// Creates a typed array of the specified length for the target type.
    /// </summary>
    /// <param name="length">The length of the array to create.</param>
    /// <returns>A new array of type T with the specified length.</returns>
    public Array CreateTypedArray(int length)
    {
        return new T[length];
    }

    /// <summary>
    /// Converts a typed value to its string representation.
    /// </summary>
    /// <param name="value">The typed value to convert to a string.</param>
    /// <returns>The string representation of the typed value, or null if the value is null.</returns>
    public virtual string? GetStringValue(T? value)
    {
        return value?.ToString();
    }

    /// <summary>
    /// Parses a string value into the target type T.
    /// </summary>
    /// <param name="value">The string value to parse.</param>
    /// <param name="validateError">When this method returns, contains the validation error message if parsing failed; otherwise, null.</param>
    /// <returns>The parsed value of type T if successful; otherwise, the default value for T.</returns>
    public abstract T? ParseValue(string? value, out string? validateError);
}
