using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ApplicationBuilderHelpers.Interfaces;

/// <summary>
/// Caller-view parser map keyed by handled type.
/// </summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public interface ICommandTypeParserCollection
{
    /// <summary>
    /// Gets the parser map keyed by handled type.
    /// </summary>
    internal Dictionary<Type, ICommandTypeParser> TypeParsers { get; }
}
