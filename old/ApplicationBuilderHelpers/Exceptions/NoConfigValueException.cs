using System;

namespace ApplicationBuilderHelpers.Exceptions;

public class NoConfigValueException(string configName) : Exception($"{configName} config is empty")
{
}
