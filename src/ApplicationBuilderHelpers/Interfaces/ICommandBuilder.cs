using ApplicationBuilderHelpers.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ApplicationBuilderHelpers.Interfaces;

/// <summary>
/// Caller-view entry-builder state: executable metadata, help layout, registrations, and theme.
/// </summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public interface ICommandBuilder : ICommandTypeParserCollection, IApplicationDependencyCollection
{
    /// <summary>
    /// Gets the registered commands in registration order.
    /// </summary>
    internal List<TypedCommandHolder> Commands { get; }

    /// <summary>
    /// Gets or sets the executable name; null (default) auto-detects from the entry assembly.
    /// </summary>
    internal string? ExecutableName { get; set; }

    /// <summary>
    /// Gets or sets the executable title; null (default) falls back to the executable name.
    /// </summary>
    internal string? ExecutableTitle { get; set; }

    /// <summary>
    /// Gets or sets the executable description; null (default) omits it from help.
    /// </summary>
    internal string? ExecutableDescription { get; set; }

    /// <summary>
    /// Gets or sets the executable version text; null (default) uses the assembly version.
    /// </summary>
    internal string? ExecutableVersion { get; set; }

    /// <summary>
    /// Gets or sets the help width; null (default) renders at the <see cref="CommandLineParser.HelpWidths.Default"/> default, values are floored to <see cref="CommandLineParser.HelpWidths.Minimum"/> and capped at <see cref="CommandLineParser.HelpWidths.Maximum"/>.
    /// SetHelpWidth accepts the range enforced by <see cref="CommandLineParser.HelpWidths"/>.
    /// </summary>
    internal int? HelpWidth { get; set; }

    /// <summary>
    /// Gets or sets the help border width; null (default) uses the theme default.
    /// </summary>
    internal int? HelpBorderWidth { get; set; }

    /// <summary>
    /// Gets or sets the help theme; defaults to the default theme, null disables coloring.
    /// </summary>
    internal IConsoleTheme? Theme { get; set; }

    internal bool RejectDuplicateOptions { get; set; }
}
