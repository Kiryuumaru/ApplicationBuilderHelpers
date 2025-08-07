using System;
using System.Diagnostics.CodeAnalysis;

namespace ApplicationBuilderHelpers.Interfaces;

/// <summary>
/// Defines a contract for parsing command-line arguments into strongly-typed objects.
/// </summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public interface ICommandTypeParser
{
    Type Type { get; }

    /// <summary>
    /// Parses a string value into an object of the specified type.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="validateError"></param>
    /// <returns></returns>
    object? Parse(string? value, out string? validateError);

    /// <summary>
    /// Converts an object to a string representation, suitable for command-line arguments.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="validateError"></param>
    /// <returns></returns>
    string? GetString(object? value);
}
