using System;
using System.Collections.Generic;
using System.Text;

namespace ApplicationBuilderHelpers.Interfaces;

/// <summary>
/// Interface for parsing command line arguments to a specific type.
/// </summary>
public interface ICommandLineTypeParser
{
    /// <summary>
    /// Gets the type that this parser handles.
    /// </summary>
    Type Type { get; }

    /// <summary>
    /// Parses the given string value to the target type.
    /// </summary>
    /// <param name="value">The string value to parse.</param>
    /// <returns>The parsed object of the target type.</returns>
    object? Parse(string? value);

    /// <summary>
    /// Validates the given string value.
    /// </summary>
    /// <param name="value">The string value to validate.</param>
    void Validate(string? value);

    /// <summary>
    /// Gets the possible choices for the target type.
    /// </summary>
    /// <returns>An array of possible choices.</returns>
    string[] Choices();
}
