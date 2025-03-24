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
    /// Parses the given value to the target type.
    /// </summary>
    /// <param name="value">The value to parse.</param>
    /// <returns>The parsed value as the target type.</returns>
    object? ParseToType(object? value);

    /// <summary>
    /// Parses the given value from the target type.
    /// </summary>
    /// <param name="value">The value to parse.</param>
    /// <returns>The parsed value as an object.</returns>
    object? ParseFromType(object? value);

    /// <summary>
    /// Validates the given string value.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="validateError">The validation error message if validation fails.</param>
    /// <returns>True if the value is valid, otherwise false.</returns>
    bool Validate(object? value, [NotNullWhen(false)] out string? validateError);
}
