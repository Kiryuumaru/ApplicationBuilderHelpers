using System;

namespace ApplicationBuilderHelpers.Exceptions;

/// <summary>
/// Represents an exception that is thrown when a required configuration value is not found or is empty.
/// </summary>
/// <param name="configName">The name of the missing configuration value.</param>
public class NoConfigValueException(string configName) : Exception($"{configName} config is empty")
{
}
