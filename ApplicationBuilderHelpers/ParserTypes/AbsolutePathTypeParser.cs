using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Diagnostics.CodeAnalysis;
using AbsolutePathHelpers;

namespace ApplicationBuilderHelpers.ParserTypes;

/// <summary>
/// Provides type parsing functionality for <see cref="AbsolutePath"/> command-line arguments.
/// </summary>
public class AbsolutePathTypeParser : ICommandTypeParser
{
    /// <summary>
    /// Gets the type that this parser handles.
    /// </summary>
    public Type Type => typeof(AbsolutePath);

    /// <summary>
    /// Parses a string value into an <see cref="AbsolutePath"/> object.
    /// </summary>
    /// <param name="value">The string value to parse.</param>
    /// <param name="validateError">When this method returns, contains the validation error message if parsing failed; otherwise, null.</param>
    /// <returns>The parsed <see cref="AbsolutePath"/> object if successful; otherwise, null.</returns>
    public object? Parse(string? value, out string? validateError)
    {
        if (AbsolutePath.TryParse(value, out var result))
        {
            validateError = null;
            return result;
        }

        validateError = $"Invalid {Type.Name} value: '{value}'. Expected a valid {Type.Name}.";
        return null;
    }

    /// <summary>
    /// Converts an object to its string representation for command-line display.
    /// </summary>
    /// <param name="value">The object to convert to a string.</param>
    /// <returns>The string representation of the object, or null if the object is null.</returns>
    public string? GetString(object? value)
    {
        return value?.ToString();
    }
}
