using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ApplicationBuilderHelpers.Interfaces;

/// <summary>
/// Represents a collection of command type parsers keyed by the type each parser handles.
/// </summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public interface ICommandTypeParserCollection
{
    internal Dictionary<Type, ICommandTypeParser> TypeParsers { get; }
}
