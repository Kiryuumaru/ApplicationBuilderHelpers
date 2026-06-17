using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ApplicationBuilderHelpers.Interfaces;

/// <summary>
/// Represents a command that can be executed within the application.
/// </summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public interface ICommandTypeParserCollection
{
    internal Dictionary<Type, ICommandTypeParser> TypeParsers { get; }
}
