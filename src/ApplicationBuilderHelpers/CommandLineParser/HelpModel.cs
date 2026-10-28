using System.Collections.Generic;
using System.Linq;

namespace ApplicationBuilderHelpers.CommandLineParser;

/// <summary>
/// Typed help content with no width, wrapping, theme, or Console knowledge.
/// Emitted by <see cref="HelpContentProvider"/>, consumed by <see cref="HelpLayoutRenderer"/>.
/// Two-column entry text is role-tagged so the renderer maps roles to theme colors;
/// usage, description, and footer text stay pre-composed plain strings.
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
/// One two-column row: role-tagged left spans and role-tagged right lines.
/// <see cref="Left"/> and <see cref="Right"/> expose the composed plain text for width math and tests.
/// </summary>
internal sealed class HelpEntry
{
    internal List<HelpSpan> LeftSpans { get; init; } = [];

    internal List<HelpLine> RightLines { get; init; } = [];

    internal string Left => string.Concat(LeftSpans.Select(span => span.Text));

    internal string Right => string.Join("\n", RightLines.Select(line => line.Text));
}

/// <summary>
/// Theme role for a span or line of help text.
/// </summary>
internal enum HelpRole
{
    Plain,
    Flag,
    Parameter,
    Description,
    Required,
}

/// <summary>
/// A run of help text with the theme role used to color it.
/// </summary>
internal readonly record struct HelpSpan(string Text, HelpRole Role);

/// <summary>
/// One logical right-column line with the theme role used to color it.
/// </summary>
internal sealed record HelpLine(string Text, HelpRole Role);
