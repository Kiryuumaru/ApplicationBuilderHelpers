using System.Collections.Generic;

namespace ApplicationBuilderHelpers.CommandLineParser;

/// <summary>
/// Typed help content with no width, wrapping, theme, or Console knowledge.
/// Emitted by <see cref="HelpContentProvider"/>, consumed by <see cref="HelpLayoutRenderer"/>.
/// All strings are pre-composed (signatures, "\n"-joined description parts,
/// "    "-indented usage/description lines); the renderer only lays them out.
/// </summary>
internal sealed class HelpModel
{
    /// <summary>
    /// Title line, e.g. "myapp v1.0.0 - My App".
    /// </summary>
    internal string TitleLine { get; init; } = string.Empty;

    /// <summary>
    /// Pre-indented usage text (leading "    " included).
    /// </summary>
    internal string UsageText { get; init; } = string.Empty;

    /// <summary>
    /// Pre-indented description text (leading "    " included), or null when absent.
    /// </summary>
    internal string? DescriptionText { get; init; }

    /// <summary>
    /// Ordered two-column sections; the renderer shares one left-column width across all of them.
    /// </summary>
    internal List<HelpSection> Sections { get; init; } = [];

    /// <summary>
    /// Footer line (global help only), or null when absent.
    /// </summary>
    internal string? FooterText { get; init; }
}

/// <summary>
/// One two-column help section, e.g. "OPTIONS:", "COMMANDS:", "GLOBAL OPTIONS:".
/// </summary>
internal sealed class HelpSection
{
    internal string Header { get; init; } = string.Empty;

    internal List<HelpEntry> Entries { get; init; } = [];
}

/// <summary>
/// One two-column row: pre-computed left signature and "\n"-joined right description.
/// </summary>
internal sealed class HelpEntry
{
    internal string Left { get; init; } = string.Empty;

    internal string Right { get; init; } = string.Empty;
}
