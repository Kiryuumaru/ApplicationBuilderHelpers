using System;

namespace ApplicationBuilderHelpers.Exceptions;

/// <summary>
/// Missing-configuration fault naming only the key.
/// </summary>
/// <param name="configName">The missing configuration key name.</param>
public class NoConfigValueException(string configName) : Exception($"{configName} config is empty")
{
}
