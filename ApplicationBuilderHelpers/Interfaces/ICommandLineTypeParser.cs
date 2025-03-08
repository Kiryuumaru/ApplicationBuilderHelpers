using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace ApplicationBuilderHelpers.Interfaces;

/// <summary>
/// Interface for parsing command line arguments to a specific type.
/// </summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public interface ICommandLineTypeParser
{
    /// <summary>
    /// Gets the type that this parser handles.
    /// </summary>
    Type Type { get; }

    /// <summary>
    /// Gets the available choices for the command line argument.
    /// </summary>
    string[] Choices { get; }

    /// <summary>
    /// Parses the given string value to the target type.
    /// </summary>
    /// <param name="value">The string value to parse.</param>
    /// <returns>The parsed object of the target type.</returns>
    object? Parse(string? value);

    /// <summary>
    /// Parses the given object value to a string.
    /// </summary>
    /// <param name="value">The object value to parse.</param>
    /// <returns>The parsed string value.</returns>
    string? Parse(object? value);

    /// <summary>
    /// Validates the given string value.
    /// </summary>
    /// <param name="value">The string value to validate.</param>
    /// <param name="validateError">The validation error message if validation fails.</param>
    /// <returns>True if the value is valid, otherwise false.</returns>
    bool Validate(string? value, [NotNullWhen(false)] out string? validateError);
}
