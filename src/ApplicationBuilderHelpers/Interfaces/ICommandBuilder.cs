using ApplicationBuilderHelpers.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ApplicationBuilderHelpers.Interfaces;

/// <summary>
/// Represents a builder for configuring executable metadata, help output, commands, dependencies, and type parsers.
/// </summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public interface ICommandBuilder : ICommandTypeParserCollection, IApplicationDependencyCollection
{
    internal List<TypedCommandHolder> Commands { get; }

    internal string? ExecutableName { get; set; }

    internal string? ExecutableTitle { get; set; }

    internal string? ExecutableDescription { get; set; }

    internal string? ExecutableVersion { get; set; }

    internal int? HelpWidth { get; set; }

    internal int? HelpBorderWidth { get; set; }

    internal IConsoleTheme? Theme { get; set; }
}
