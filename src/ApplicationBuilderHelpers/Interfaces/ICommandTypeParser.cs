using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace ApplicationBuilderHelpers.Interfaces;

/// <summary>
/// Caller-view value-parser contract binding CLI text to one CLR type.
/// </summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public interface ICommandTypeParser
{
    /// <summary>
    /// Gets the CLR type this parser converts.
    /// </summary>
    Type Type { get; }

    /// <summary>
    /// Parses CLI text into the handled type.
    /// </summary>
    /// <param name="value">The CLI text to parse.</param>
    /// <param name="validateError">On failure, the reason shown in the <c>InvalidValue</c> message (exit 2); otherwise null.</param>
    /// <returns>The parsed value boxed, or null when parsing fails.</returns>
    object? Parse(string? value, out string? validateError);

    /// <summary>
    /// Formats a boxed value for help defaults and completion.
    /// </summary>
    /// <param name="value">The boxed value to format.</param>
    /// <returns>The display text, or null when the value has no text form.</returns>
    string? GetString(object? value);

    /// <summary>
    /// Gets the boxed type default used when no value is provided.
    /// </summary>
    /// <returns>The boxed default of the handled type.</returns>
    object? GetDefaultValue();

    /// <summary>
    /// Allocates an exactly-typed array used for AOT-safe collection binding.
    /// </summary>
    /// <param name="length">The array length.</param>
    /// <returns>An array of the parser element type with the requested length.</returns>
    Array CreateTypedArray(int length);

    /// <summary>
    /// Allocates a list with the given capacity used for AOT-safe collection binding.
    /// </summary>
    /// <param name="capacity">The capacity hint.</param>
    /// <returns>A list of the parser element type with the requested capacity.</returns>
    IList CreateTypedList(int capacity);
}
