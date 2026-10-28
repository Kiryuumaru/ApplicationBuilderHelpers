using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApplicationBuilderHelpers.CommandLineParser;

/// <summary>
/// Layout renderer for help output.
/// </summary>
internal sealed class HelpLayoutRenderer(IHelpOutput output)
{
    private readonly IHelpOutput _output = output;

    /// <summary>Renders the model at the given width; values are floored to <see cref="HelpWidths.Minimum"/> and capped at <see cref="HelpWidths.Maximum"/>.</summary>
    internal void Render(HelpModel model, IConsoleTheme? theme, int helpWidth, int helpBorderWidth)
    {
        helpWidth = HelpWidths.Clamp(helpWidth);

        WriteColored(model.TitleLine, theme?.HeaderColor);
        _output.WriteLine();

        WriteColored("USAGE:", theme?.HeaderColor);
        WriteWrappedContent(model.UsageText, helpWidth, 0, theme);
        _output.WriteLine();

        if (!string.IsNullOrEmpty(model.DescriptionText))
        {
            WriteColored("DESCRIPTION:", theme?.HeaderColor);
            WriteWrappedContent(model.DescriptionText, helpWidth, 0, theme);
            _output.WriteLine();
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
            ShowSectionWithFixedLayout(section, optimalLeftColumnWidth, helpWidth, helpBorderWidth, theme);
        }

        if (!string.IsNullOrEmpty(model.FooterText))
        {
            WriteWrappedContent(model.FooterText, helpWidth, 0, theme);
        }
    }

    private void ShowSectionWithFixedLayout(HelpSection section, int leftColumnWidth, int totalWidth, int helpBorderWidth, IConsoleTheme? theme)
    {
        if (section.Entries.Count == 0) return;

        WriteColored(section.Header, theme?.HeaderColor);

        foreach (var entry in section.Entries)
        {
            var leftDisplayWidth = GetDisplayWidth(entry.Left);

            if (leftDisplayWidth > leftColumnWidth)
            {
                WriteLeftColumn(entry.LeftSpans, theme);
                _output.WriteLine();
                WriteWrappedText(entry.RightLines, 0, 4, totalWidth - 4, theme);
            }
            else
            {
                var descriptionColumn = leftColumnWidth + helpBorderWidth;

                WriteLeftColumn(entry.LeftSpans, theme);
                _output.Write(new string(' ', descriptionColumn - leftDisplayWidth));
                WriteWrappedText(entry.RightLines, descriptionColumn, descriptionColumn, totalWidth, theme);
            }
        }
        _output.WriteLine();
    }

    private void WriteLeftColumn(List<HelpSpan> spans, IConsoleTheme? theme)
    {
        foreach (var span in spans)
        {
            _output.Write(span.Text, ResolveColor(span.Role, theme));
        }
    }

    private int CalculateOptimalLeftColumnWidth(List<string> leftColumnItems, int totalWidth)
    {
        if (leftColumnItems.Count == 0) return 25;

        var maxLeftWidth = leftColumnItems.Max(GetDisplayWidth);

        const int MinLeftColumnWidth = 20;
        const int MaxLeftColumnWidth = 35;

        var effectiveWidth = Math.Max(totalWidth, HelpWidths.Minimum);
        var maxAllowedLeftWidth = effectiveWidth - 40;

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

    private void WriteAnnotatedLine(LabelSpec spec, HelpLine line, int lineStartColumn, int firstLineColumn, string indentStr, int maxColumn, IConsoleTheme? theme)
    {
        if (!TrySplitPrefix(line.Text, out var prefix, out var valueText))
        {
            WriteWrappedLine(line, lineStartColumn, indentStr, maxColumn, theme);
            return;
        }

        if (spec.CommaPack)
        {
            WriteColoredText(prefix, theme?.SecondaryColor);
            var currentPos = firstLineColumn + GetDisplayWidth(prefix);

            if (!string.IsNullOrEmpty(valueText))
            {
                WriteCommaPackedValues(valueText, maxColumn, indentStr, theme, currentPos);
            }
        }
        else
        {
            WriteSimpleAnnotated(prefix, valueText, theme);
        }
    }

    private void WriteCommaPackedValues(string valuesText, int maxColumn, string indentStr, IConsoleTheme? theme, int currentPos)
    {
        var values = valuesText.Split(',').Select(v => v.Trim()).ToArray();

        for (int i = 0; i < values.Length; i++)
        {
            var value = values[i];
            var textToAdd = i == 0 ? $" {value}" : $", {value}";
            var textLength = GetDisplayWidth(textToAdd);

            if (currentPos + textLength < maxColumn - 2)
            {
                WriteColoredText(textToAdd, theme?.ParameterColor);
                currentPos += textLength;
            }
            else
            {
                _output.WriteLine();
                _output.Write(indentStr);
                WriteColoredText(value, theme?.ParameterColor);
                currentPos = indentStr.Length + GetDisplayWidth(value);
            }
        }
    }

    private void WriteSimpleAnnotated(string prefix, string valueText, IConsoleTheme? theme)
    {
        WriteColoredText(prefix, theme?.SecondaryColor);
        WriteColoredText($" {valueText}", theme?.ParameterColor);
    }

    private void WriteWrappedText(List<HelpLine> lines, int firstLineColumn, int continuationIndent, int maxColumn, IConsoleTheme? theme)
    {
        if (lines.Count == 0)
        {
            _output.WriteLine();
            return;
        }

        var indentStr = new string(' ', continuationIndent);
        var firstSourceLine = true;

        foreach (var line in lines)
        {
            var lineStartColumn = firstSourceLine ? firstLineColumn : continuationIndent;

            if (!firstSourceLine)
            {
                _output.WriteLine();
                _output.Write(indentStr);
            }

            if (TryMatchAnnotated(line.Text, out var spec))
            {
                WriteAnnotatedLine(spec, line, lineStartColumn, firstLineColumn, indentStr, maxColumn, theme);
            }
            else
            {
                WriteWrappedLine(line, lineStartColumn, indentStr, maxColumn, theme);
            }

            firstSourceLine = false;
        }

        _output.WriteLine();
    }

    private void WriteWrappedLine(HelpLine line, int startColumn, string indentStr, int maxColumn, IConsoleTheme? theme)
    {
        var color = ResolveColor(line.Role, theme);
        var words = line.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var currentLineLength = startColumn;
        var lineStarted = false;

        foreach (var word in words)
        {
            var wordLength = GetDisplayWidth(word);

            if (lineStarted && currentLineLength + wordLength + 1 > maxColumn)
            {
                _output.WriteLine();
                _output.Write(indentStr);
                currentLineLength = indentStr.Length;
                lineStarted = false;
            }

            if (lineStarted)
            {
                _output.Write(" ");
                currentLineLength++;
            }

            WriteColoredText(word, color);
            currentLineLength += wordLength;
            lineStarted = true;
        }
    }

    private void WriteColored(string text, ConsoleColor? color)
    {
        _output.WriteLine(text, color);
    }

    private void WriteColoredText(string text, ConsoleColor? color)
    {
        _output.Write(text, color);
    }

    private void WriteWrappedContent(string text, int maxWidth, int indent, IConsoleTheme? theme)
    {
        if (string.IsNullOrEmpty(text))
        {
            _output.WriteLine();
            return;
        }

        var indentStr = new string(' ', indent);
        var availableWidth = maxWidth - indent;

        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var currentLineLength = 0;
        var lineStarted = false;

        foreach (var word in words)
        {
            var wordLength = GetDisplayWidth(word);

            if (lineStarted && currentLineLength + wordLength + 1 > availableWidth)
            {
                _output.WriteLine();
                _output.Write(indentStr);
                currentLineLength = 0;
                lineStarted = false;
            }

            if (lineStarted)
            {
                _output.Write(" ");
                currentLineLength++;
            }
            else if (indent > 0)
            {
                _output.Write(indentStr);
                currentLineLength = indent;
            }

            WriteColoredText(word, theme?.DescriptionColor);
            currentLineLength += wordLength;
            lineStarted = true;
        }

        _output.WriteLine();
    }

    private static ConsoleColor? ResolveColor(HelpRole role, IConsoleTheme? theme) => role switch
    {
        HelpRole.Flag => theme?.FlagColor,
        HelpRole.Parameter => theme?.ParameterColor,
        HelpRole.Description => theme?.DescriptionColor,
        HelpRole.Required => theme?.RequiredColor,
        _ => null,
    };

    private int GetDisplayWidth(string text)
    {
        if (string.IsNullOrEmpty(text)) return 0;

        return text.Length;
    }
}
