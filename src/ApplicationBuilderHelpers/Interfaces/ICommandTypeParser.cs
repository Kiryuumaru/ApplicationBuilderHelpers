using System;
using System.Diagnostics.CodeAnalysis;

namespace ApplicationBuilderHelpers.Interfaces;

/// <summary>
/// Defines a contract for parsing command-line arguments into strongly-typed objects.
/// </summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public interface ICommandTypeParser
{
    /// <summary>
    /// Gets the type that this parser can handle.
    /// </summary>
    Type Type { get; }

    /// <summary>
    /// Parses a string value into an object of the specified type.
    /// </summary>
    /// <param name="value">The string value to parse.</param>
    /// <param name="validateError">When this method returns, contains the validation error message if parsing failed, or null if parsing succeeded.</param>
    /// <returns>The parsed object if successful; otherwise, null.</returns>
    object? Parse(string? value, out string? validateError);

    /// <summary>
    /// Converts an object to a string representation, suitable for command-line arguments.
    /// </summary>
    /// <param name="value">The object to convert to a string.</param>
    /// <returns>The string representation of the object if successful; otherwise, null.</returns>
    string? GetString(object? value);

    /// <summary>
    /// Gets the default value for the type, which can be used when no value is provided.
    /// </summary>
    /// <returns></returns>
    object? GetDefaultValue();

    /// <summary>
    /// Creates a typed array for AOT compatibility
    /// </summary>
    /// <param name="length"></param>
    /// <returns></returns>
    Array CreateTypedArray(int length);
}
