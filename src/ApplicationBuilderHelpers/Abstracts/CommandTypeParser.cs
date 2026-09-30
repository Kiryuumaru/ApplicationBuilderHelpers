using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApplicationBuilderHelpers.Abstracts;

/// <summary>
/// Parser base binding one CLR type to CLI text.
/// </summary>
/// <typeparam name="T">The CLR type this parser converts to and from CLI text.</typeparam>
public abstract class CommandTypeParser<T> : ICommandTypeParser
{
    /// <summary>
    /// Gets the CLR type this parser converts.
    /// </summary>
    public Type Type { get; } = typeof(T);

    /// <summary>
    /// Parses CLI text into the handled type.
    /// </summary>
    /// <param name="value">The CLI text to parse.</param>
    /// <param name="validateError">On failure, the reason shown in the <c>InvalidValue</c> message (exit 2); otherwise null.</param>
    /// <returns>The parsed value boxed, or null when parsing fails.</returns>
    public object? Parse(string? value, out string? validateError)
    {
        return ParseValue(value, out validateError);
    }

    /// <summary>
    /// Formats a boxed value for help defaults and completion.
    /// </summary>
    /// <param name="value">The boxed value to format.</param>
    /// <returns>The display text, or null when the value has no text form.</returns>
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
    /// Gets the default of <typeparamref name="T"/> boxed for omitted values.
    /// </summary>
    /// <returns>The boxed default of <typeparamref name="T"/>.</returns>
    public object? GetDefaultValue()
    {
        return default(T);
    }

    /// <summary>
    /// Allocates an exactly-typed array used for AOT-safe collection binding.
    /// </summary>
    /// <param name="length">The array length.</param>
    /// <returns>A <c>T[length]</c> array.</returns>
    public Array CreateTypedArray(int length)
    {
        return new T[length];
    }

    /// <summary>
    /// Allocates a list with the given capacity used for AOT-safe collection binding.
    /// </summary>
    /// <param name="capacity">The capacity hint.</param>
    /// <returns>A <c>List&lt;T&gt;</c> with the requested capacity.</returns>
    public virtual IList CreateTypedList(int capacity)
    {
        return new List<T>(capacity);
    }

    /// <summary>
    /// Formats a typed value for help defaults and completion.
    /// </summary>
    /// <param name="value">The typed value to format.</param>
    /// <returns>The display text, or null when the value has no text form.</returns>
    public virtual string? GetStringValue(T? value)
    {
        return value?.ToString();
    }

    /// <summary>
    /// Parses CLI text into <typeparamref name="T"/>.
    /// </summary>
    /// <param name="value">The CLI text to parse.</param>
    /// <param name="validateError">On failure, the reason shown in the <c>InvalidValue</c> message (exit 2); otherwise null.</param>
    /// <returns>The parsed value, or the default of <typeparamref name="T"/> when parsing fails.</returns>
    public abstract T? ParseValue(string? value, out string? validateError);
}
