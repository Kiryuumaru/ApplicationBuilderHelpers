using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApplicationBuilderHelpers.CommandLineParser;

/// <summary>
/// Layout renderer for help output: left-column width calculation, word
/// wrapping, theme application, and <see cref="ConsoleOutput"/> writes.
/// Moved verbatim from <see cref="HelpFormatter"/> (mechanical split, no
/// behavior change). Knows nothing about options, arguments, categorization,
/// or defaults — it only lays out the <see cref="HelpModel"/> it is given.
/// </summary>
internal sealed class HelpLayoutRenderer(ConsoleOutput consoleOutput)
{
    private readonly ConsoleOutput _consoleOutput = consoleOutput;

    internal void Render(HelpModel model, IConsoleTheme? theme, int helpWidth)
    {
        WriteColored(model.TitleLine, theme?.HeaderColor);
        _consoleOutput.WriteLine();

        WriteColored("USAGE:", theme?.HeaderColor);
        WriteWrappedContent(model.UsageText, helpWidth, 0, theme);
        _consoleOutput.WriteLine();

        if (!string.IsNullOrEmpty(model.DescriptionText))
        {
            WriteColored("DESCRIPTION:", theme?.HeaderColor);
            WriteWrappedContent(model.DescriptionText, helpWidth, 0, theme);
            _consoleOutput.WriteLine();
        }

        var allLeftColumnItems = new List<string>();
        foreach (var section in model.Sections)
        {
            foreach (var entry in section.Entries)
            {
                allLeftColumnItems.Add(entry.Left);
            }
        }

        var optimalLeftColumnWidth = CalculateOptimalLeftColumnWidth(allLeftColumnItems, helpWidth);

        foreach (var section in model.Sections)
        {
            ShowSectionWithFixedLayout(section.Header, section.Entries, optimalLeftColumnWidth, helpWidth, theme,
                entry => entry.Left,
                entry => entry.Right);
        }

        if (!string.IsNullOrEmpty(model.FooterText))
        {
            WriteWrappedContent(model.FooterText, helpWidth, 0, theme);
        }
    }

    private void ShowSectionWithFixedLayout<T>(string sectionHeader, List<T> items, int leftColumnWidth, int totalWidth, IConsoleTheme? theme,
        Func<T, string> getLeftColumn, Func<T, string> getRightColumn)
    {
        if (items.Count == 0) return;

        WriteColored(sectionHeader, theme?.HeaderColor);

        const int Padding = 2;

        // Display all items with the fixed left column width
        foreach (var item in items)
        {
            var leftColumn = getLeftColumn(item);
            var rightColumn = getRightColumn(item);
            var leftDisplayWidth = GetDisplayWidth(leftColumn);

            if (leftDisplayWidth > leftColumnWidth)
            {
                // Left content is too long - put right content on next line
                _consoleOutput.WriteLine(leftColumn);
                WriteWrappedText(rightColumn, totalWidth - 4, 4, theme);
            }
            else
            {
                // Standard two-column layout with fixed left column width
                var rightColumnWidth = totalWidth - leftColumnWidth - Padding;

                _consoleOutput.Write(leftColumn);
                _consoleOutput.Write(new string(' ', leftColumnWidth - leftDisplayWidth + Padding));
                WriteWrappedText(rightColumn, rightColumnWidth, leftColumnWidth + Padding, theme);
            }
        }
        _consoleOutput.WriteLine();
    }

    private int CalculateOptimalLeftColumnWidth(List<string> leftColumnItems, int totalWidth)
    {
        if (leftColumnItems.Count == 0) return 25; // Default reasonable width

        // Calculate the maximum width needed for the left column
        var maxLeftWidth = leftColumnItems.Max(GetDisplayWidth);

        // Set reasonable bounds for the left column
        const int MinLeftColumnWidth = 20;
        const int MaxLeftColumnWidth = 35;  // More reasonable maximum

        // Ensure the right column has enough space (floor from #450: 20 min-left + 40 right reservation)
        var effectiveWidth = Math.Max(totalWidth, 60); // never throws on narrow widths
        var maxAllowedLeftWidth = effectiveWidth - 40; // Ensure at least 40 chars for right column

        var leftColumnWidth = Math.Min(Math.Max(maxLeftWidth, MinLeftColumnWidth),
                                       Math.Min(MaxLeftColumnWidth, maxAllowedLeftWidth));

        return leftColumnWidth;
    }

    private sealed record LabelSpec(string Label, bool CommaPack);

    private static readonly LabelSpec[] AnnotatedLabels =
    [
        new("Possible values:", true),
        new("Environment variable:", false),
        new("Default:", false),
    ];

    private static bool TryMatchAnnotated(string line, out LabelSpec spec)
    {
        foreach (var candidate in AnnotatedLabels)
        {
            if (line.Contains(candidate.Label))
            {
                spec = candidate;
                return true;
            }
        }

        spec = null!;
        return false;
    }

    private static bool TrySplitPrefix(string line, out string prefix, out string valueText)
    {
        var colonIndex = line.IndexOf(':');
        if (colonIndex != -1)
        {
            prefix = line[..(colonIndex + 1)];
            valueText = line[(colonIndex + 1)..].Trim();
            return true;
        }

        prefix = string.Empty;
        valueText = string.Empty;
        return false;
    }

    private void WriteAnnotatedLine(LabelSpec spec, string line, int width, int indent, string indentStr, bool firstLine, IConsoleTheme? theme)
    {
        if (!TrySplitPrefix(line, out var prefix, out var valueText))
        {
            WriteWrappedLine(line, width, firstLine ? 0 : indent, indentStr);
            return;
        }

        if (spec.CommaPack)
        {
            WriteColoredText(prefix, theme?.SecondaryColor);
            var currentPos = GetDisplayWidth(prefix);

            if (!string.IsNullOrEmpty(valueText))
            {
                WriteCommaPackedValues(valueText, width, indent, indentStr, theme, currentPos);
            }
        }
        else
        {
            WriteSimpleAnnotated(prefix, valueText, theme);
        }
    }

    private void WriteCommaPackedValues(string valuesText, int width, int indent, string indentStr, IConsoleTheme? theme, int currentPos)
    {
        // Split values by comma and fit as many as possible per line
        var values = valuesText.Split(',').Select(v => v.Trim()).ToArray();

        for (int i = 0; i < values.Length; i++)
        {
            var value = values[i];
            var textToAdd = i == 0 ? $" {value}" : $", {value}";
            var textLength = GetDisplayWidth(textToAdd);

            // Check if it fits on current line with some buffer
            if (currentPos + textLength < width - 2) // Leave 2 chars buffer
            {
                WriteColoredText(textToAdd, theme?.ParameterColor);
                currentPos += textLength;
            }
            else
            {
                // Move to next line
                _consoleOutput.WriteLine();
                _consoleOutput.Write(indentStr);
                WriteColoredText(value, theme?.ParameterColor);
                currentPos = indent + GetDisplayWidth(value);
            }
        }
    }

    private void WriteSimpleAnnotated(string prefix, string valueText, IConsoleTheme? theme)
    {
        WriteColoredText(prefix, theme?.SecondaryColor);
        WriteColoredText($" {valueText}", theme?.ParameterColor);
    }

    private void WriteWrappedText(string text, int width, int indent, IConsoleTheme? theme)
    {
        if (string.IsNullOrEmpty(text))
        {
            _consoleOutput.WriteLine();
            return;
        }

        // Split the text by explicit line breaks first
        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var indentStr = new string(' ', indent);
        var firstLine = true;

        foreach (var line in lines)
        {
            if (!firstLine)
            {
                _consoleOutput.WriteLine();
                _consoleOutput.Write(indentStr);
            }

            if (TryMatchAnnotated(line, out var spec))
            {
                WriteAnnotatedLine(spec, line, width, indent, indentStr, firstLine, theme);
            }
            else
            {
                // Normal word wrapping for other lines
                WriteWrappedLine(line, width, firstLine ? 0 : indent, indentStr);
            }

            firstLine = false;
        }

        _consoleOutput.WriteLine();
    }

    private void WriteWrappedLine(string line, int width, int currentIndent, string indentStr)
    {
        var words = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var currentLineLength = currentIndent;
        var lineStarted = false;

        foreach (var word in words)
        {
            var wordLength = GetDisplayWidth(word);

            if (lineStarted && currentLineLength + wordLength + 1 > width)
            {
                _consoleOutput.WriteLine();
                _consoleOutput.Write(indentStr);
                currentLineLength = indentStr.Length;
                lineStarted = false;
            }

            if (lineStarted)
            {
                _consoleOutput.Write(" ");
                currentLineLength++;
            }

            WriteColoredText(word, null); // No coloring for regular words
            currentLineLength += wordLength;
            lineStarted = true;
        }
    }

    private void WriteColored(string text, ConsoleColor? color)
    {
        _consoleOutput.WriteLine(text, color);
    }

    private void WriteColoredText(string text, ConsoleColor? color)
    {
        _consoleOutput.Write(text, color);
    }

    private void WriteWrappedContent(string text, int maxWidth, int indent, IConsoleTheme? theme)
    {
        if (string.IsNullOrEmpty(text))
        {
            _consoleOutput.WriteLine();
            return;
        }

        var indentStr = new string(' ', indent);
        var availableWidth = maxWidth - indent;

        // Split text into words while preserving console color formatting
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var currentLineLength = 0;
        var lineStarted = false;

        foreach (var word in words)
        {
            var wordLength = GetDisplayWidth(word);

            // Check if we need to wrap to next line
            if (lineStarted && currentLineLength + wordLength + 1 > availableWidth)
            {
                _consoleOutput.WriteLine();
                _consoleOutput.Write(indentStr);
                currentLineLength = 0;
                lineStarted = false;
            }

            // Add space before word if not at line start
            if (lineStarted)
            {
                _consoleOutput.Write(" ");
                currentLineLength++;
            }
            else if (indent > 0)
            {
                _consoleOutput.Write(indentStr);
                currentLineLength = indent;
            }

            WriteColoredText(word, theme?.DescriptionColor);
            currentLineLength += wordLength;
            lineStarted = true;
        }

        _consoleOutput.WriteLine();
    }

    private int GetDisplayWidth(string text)
    {
        if (string.IsNullOrEmpty(text)) return 0;

        // Since we're not using ANSI codes anymore, just return the string length
        return text.Length;
    }
}
