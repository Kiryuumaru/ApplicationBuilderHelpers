using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

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
    /// Parses the provided string array into an object of the specified type.
    /// </summary>
    /// <param name="value">The string array to parse. May be null.</param>
    /// <param name="validateError">When this method returns, contains any validation error message, or null if parsing succeeded.</param>
    /// <returns>The parsed object, or null if parsing failed or the input was null.</returns>
    object? Parse(string[]? value, out string? validateError);
}
