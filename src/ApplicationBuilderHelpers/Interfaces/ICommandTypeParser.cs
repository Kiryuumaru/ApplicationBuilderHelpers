using System;
using System.Collections;
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
    /// <returns>The default value for the type.</returns>
    object? GetDefaultValue();

    /// <summary>
    /// Creates a typed array for AOT compatibility.
    /// </summary>
    /// <param name="length">The length of the array to create.</param>
    /// <returns>A new array of the parser element type with the specified length.</returns>
    Array CreateTypedArray(int length);

    /// <summary>
    /// Creates a typed list for AOT compatibility.
    /// Matches <see cref="CreateTypedArray(int)"/>: the generic
    /// <c>CommandTypeParser&lt;T&gt;</c> factory is <c>new List&lt;T&gt;(capacity)</c>.
    /// </summary>
    /// <param name="capacity">The capacity hint for the list to create.</param>
    /// <returns>A new list of the parser element type with the specified capacity.</returns>
    IList CreateTypedList(int capacity);
}
