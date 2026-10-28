using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.CommandLineParser;
using ApplicationBuilderHelpers.Extensions;
using ApplicationBuilderHelpers.Interfaces;
using ApplicationBuilderHelpers.Themes;
using Microsoft.Extensions.Hosting;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// Role-to-color tests for the help layout renderer.
/// Builds the real provider model and records the exact text/color pairs the renderer
/// writes through <see cref="IHelpOutput"/>, so every assertion is deterministic and
/// independent of console redirection or terminal capabilities.
/// </summary>
public sealed class HelpColorRoleTests
{
    private static readonly IConsoleTheme DefaultTheme = DefaultConsoleTheme.Instance;

    [Command("color-row", "Exercises option row colors.")]
    public sealed class OptionRowColorCommand : Command
    {
        [CommandOption('f', "format", Description = "Output format for the deployment report.")]
        public string? Format { get; set; }

        [CommandOption("dry-run", Description = "Preview changes.")]
        public bool DryRun { get; set; }

        [CommandOption("tag", Description = "Tag to attach.")]
        public List<string> Tags { get; set; } = [];

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            return ValueTask.CompletedTask;
        }
    }

    [Command("color-description", "Exercises wrapped option descriptions.")]
    public sealed class WrappedDescriptionCommand : Command
    {
        [CommandOption('n', "narrative", Description = "Emits detailed narrative output for operators who inspect deployment records across every environment boundary.")]
        public string? Narrative { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            return ValueTask.CompletedTask;
        }
    }

    [Command("color-required", "Exercises required marker colors.")]
    public sealed class RequiredMarkerCommand : Command
    {
        [CommandOption("count", Description = "Item count.", Required = true)]
        public int Count { get; set; }

        [CommandOption("token", Required = true)]
        public string? Token { get; set; }

        [CommandOption("note", Description = "Marks the entry as (required) in generated docs.")]
        public string? Note { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            return ValueTask.CompletedTask;
        }
    }

    [Command("color-annotation", "Exercises annotation colors.")]
    public sealed class AnnotationColorCommand : Command
    {
        [CommandOption('f', "format", Description = "Output format for the report.", FromAmong = ["table", "json", "yaml"], EnvironmentVariable = "PARKER_COLOR_FORMAT")]
        public string Format { get; set; } = "table";

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            return ValueTask.CompletedTask;
        }
    }

    [Command("color-layout", "Exercises two-column and overflow layouts.")]
    public sealed class LayoutColorCommand : Command
    {
        [CommandOption('m', "mode", Description = "Selects the render mode.")]
        public string? Mode { get; set; }

        [CommandOption('r', "release-note-doc", Description = "Writes the configured release note message to every output channel after the build completes so operators can audit what shipped.")]
        public string? ReleaseNoteDoc { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            return ValueTask.CompletedTask;
        }
    }

    [Command("color-theme", "Exercises every help color role.")]
    public sealed class ThemeColorCommand : Command
    {
        [CommandOption('f', "format", Description = "Output format for the report.", FromAmong = ["table", "json"], EnvironmentVariable = "PARKER_COLOR_THEME_FORMAT")]
        public string Format { get; set; } = "table";

        [CommandOption("token", Description = "Access token.", Required = true)]
        public string? Token { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            return ValueTask.CompletedTask;
        }
    }

    [Command("deploy", "Deploy the application.")]
    public sealed class GlobalDeployCommand : Command
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            return ValueTask.CompletedTask;
        }
    }

    [Command("auxiliary", "Auxiliary verification command.")]
    public sealed class AuxiliaryCommand : Command
    {
        [CommandOption("aux-marker", Description = "Auxiliary marker.")]
        public string? AuxMarker { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            return ValueTask.CompletedTask;
        }
    }

    [Fact]
    public void OptionRow_ColorsFlagsAndParameterButLeavesSeparatorsPlain()
    {
        var rendered = RenderCommand<OptionRowColorCommand>("color-row", DefaultTheme, helpWidth: 120);

        var line = FindLine(rendered.Lines, "-f, --format <STRING>");
        var flagIndex = line.Text.IndexOf("-f, --format", StringComparison.Ordinal);
        var placeholderIndex = line.Text.IndexOf("<STRING>", StringComparison.Ordinal);

        AssertTextColored(line, "    ", null);
        AssertTextColored(line, "-f, --format", DefaultTheme.FlagColor);
        AssertRangeColored(
            line,
            flagIndex + "-f, --format".Length,
            placeholderIndex - (flagIndex + "-f, --format".Length),
            null);
        AssertTextColored(line, "<STRING>", DefaultTheme.ParameterColor);
        AssertWhitespacePlain(line);
    }

    [Fact]
    public void NegatedBoolOption_ColorsBothSpellingsAsFlagsWithoutAPlaceholder()
    {
        var rendered = RenderCommand<OptionRowColorCommand>("color-row", DefaultTheme, helpWidth: 120);

        var line = FindLine(rendered.Lines, "--dry-run, --no-dry-run");

        AssertTextColored(line, "--dry-run, --no-dry-run", DefaultTheme.FlagColor);
        Assert.DoesNotContain(line.Segments, segment => segment.Text.Contains('<', StringComparison.Ordinal));
        Assert.DoesNotContain(line.Segments, segment => segment.Color == DefaultTheme.ParameterColor);
    }

    [Fact]
    public void CollectionOption_ColorsEllipsisPlaceholderAsParameter()
    {
        var rendered = RenderCommand<OptionRowColorCommand>("color-row", DefaultTheme, helpWidth: 120);

        var line = FindLine(rendered.Lines, "--tag <STRING...>");

        AssertTextColored(line, "--tag", DefaultTheme.FlagColor);
        AssertTextColored(line, "<STRING...>", DefaultTheme.ParameterColor);
    }

    [Fact]
    public void OptionDescription_ColorsFirstLineAndWrappedContinuationWords()
    {
        const string description = "Emits detailed narrative output for operators who inspect deployment records across every environment boundary.";
        var descriptionWords = description.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet(StringComparer.Ordinal);
        var rendered = RenderCommand<WrappedDescriptionCommand>("color-description", DefaultTheme, helpWidth: 71);

        var startIndex = LineIndex(rendered.Lines, "--narrative <STRING>");
        var block = EntryBlock(rendered.Lines, startIndex);

        Assert.True(block.Count > 1, "Expected the option description to wrap onto continuation lines.");
        Assert.Contains(block[0].Segments, segment => descriptionWords.Contains(segment.Text));
        Assert.Contains(block.Skip(1), line => line.Segments.Any(segment => descriptionWords.Contains(segment.Text)));

        foreach (var line in block)
        {
            foreach (var segment in line.Segments.Where(segment => descriptionWords.Contains(segment.Text)))
            {
                Assert.Equal(DefaultTheme.DescriptionColor, segment.Color);
            }

            AssertWhitespacePlain(line);
        }
    }

    [Fact]
    public void RequiredMarker_ColorsStandaloneMarkerWithAndWithoutDescription()
    {
        var rendered = RenderCommand<RequiredMarkerCommand>("color-required", DefaultTheme, helpWidth: 120);

        var standaloneMarker = Assert.Single(rendered.Lines, line => line.Text.Trim() == "(required)");
        AssertTextColored(standaloneMarker, "(required)", DefaultTheme.RequiredColor);

        var tokenLine = FindLine(rendered.Lines, "--token");
        AssertTextColored(tokenLine, "(required)", DefaultTheme.RequiredColor);
        Assert.DoesNotContain(
            tokenLine.Segments,
            segment => segment.Text == "(required)" && segment.Color == DefaultTheme.DescriptionColor);
    }

    [Fact]
    public void DescriptionProse_KeepsRequiredWordInDescriptionColor()
    {
        var rendered = RenderCommand<RequiredMarkerCommand>("color-required", DefaultTheme, helpWidth: 120);

        var line = FindLine(rendered.Lines, "Marks the entry as (required)");

        AssertTextColored(line, "(required)", DefaultTheme.DescriptionColor);
        Assert.DoesNotContain(
            line.Segments,
            segment => segment.Text == "(required)" && segment.Color == DefaultTheme.RequiredColor);
    }

    [Fact]
    public void Annotations_KeepSecondaryLabelsAndParameterValues()
    {
        var rendered = RenderCommand<AnnotationColorCommand>("color-annotation", DefaultTheme, helpWidth: 120);

        var valuesLine = FindLine(rendered.Lines, "Possible values:");
        AssertTextColored(valuesLine, "Possible values:", DefaultTheme.SecondaryColor);
        AssertTextColored(valuesLine, "table", DefaultTheme.ParameterColor);
        AssertTextColored(valuesLine, "json", DefaultTheme.ParameterColor);
        AssertTextColored(valuesLine, "yaml", DefaultTheme.ParameterColor);

        var environmentLine = FindLine(rendered.Lines, "Environment variable:");
        AssertTextColored(environmentLine, "Environment variable:", DefaultTheme.SecondaryColor);
        AssertTextColored(environmentLine, "PARKER_COLOR_FORMAT", DefaultTheme.ParameterColor);

        var defaultLine = FindLine(rendered.Lines, "Default:");
        AssertTextColored(defaultLine, "Default:", DefaultTheme.SecondaryColor);
        AssertTextColored(defaultLine, "table", DefaultTheme.ParameterColor);
    }

    [Fact]
    public void CommandRows_ColorNamesAsFlagsAndDescriptionsAsDescription()
    {
        var rendered = RenderGlobal<GlobalDeployCommand>(DefaultTheme, helpWidth: 120);

        var line = FindLine(rendered.Lines, "Deploy the application.");

        AssertTextColored(line, "deploy", DefaultTheme.FlagColor);
        AssertTextColored(line, "Deploy the application.", DefaultTheme.DescriptionColor);
        AssertWhitespacePlain(line);
    }

    [Fact]
    public void GlobalOptions_ColorHelpAndVersionFlagsAndDescriptions()
    {
        var rendered = RenderGlobal<GlobalDeployCommand>(DefaultTheme, helpWidth: 120);

        var helpLine = FindLine(rendered.Lines, "-h, --help, -?");
        AssertTextColored(helpLine, "-h, --help, -?", DefaultTheme.FlagColor);
        AssertTextColored(helpLine, "Show help information", DefaultTheme.DescriptionColor);

        var versionLine = FindLine(rendered.Lines, "-V, --version");
        AssertTextColored(versionLine, "-V, --version", DefaultTheme.FlagColor);
        AssertTextColored(versionLine, "Show version information", DefaultTheme.DescriptionColor);
    }

    [Fact]
    public void LongOption_ColorsLeftSpansAndDescriptionInTwoColumnAndOverflowLayouts()
    {
        var model = BuildCommandModel<LayoutColorCommand>("color-layout");

        var narrow = Render(model, DefaultTheme, helpWidth: 71);

        var modeLine = narrow.Lines[LineIndex(narrow.Lines, "    -m, --mode <STRING>")];
        Assert.Equal(33, modeLine.Text.IndexOf("Selects", StringComparison.Ordinal));
        AssertTextColored(modeLine, "-m, --mode", DefaultTheme.FlagColor);
        AssertTextColored(modeLine, "<STRING>", DefaultTheme.ParameterColor);
        AssertTextColored(modeLine, "Selects the render mode.", DefaultTheme.DescriptionColor);
        AssertWhitespacePlain(modeLine);

        var overflowIndex = LineIndex(narrow.Lines, "    -r, --release-note-doc <STRING>");
        var overflowLine = narrow.Lines[overflowIndex];
        Assert.Equal("    -r, --release-note-doc <STRING>", overflowLine.Text);
        AssertTextColored(overflowLine, "-r, --release-note-doc", DefaultTheme.FlagColor);
        AssertTextColored(overflowLine, "<STRING>", DefaultTheme.ParameterColor);
        AssertWhitespacePlain(overflowLine);

        var firstDescriptionLine = narrow.Lines[overflowIndex + 1];
        Assert.StartsWith("Writes the configured release note", firstDescriptionLine.Text, StringComparison.Ordinal);
        Assert.False(char.IsWhiteSpace(firstDescriptionLine.Text[0]), "Overflow descriptions start at column zero.");
        AssertTextColored(firstDescriptionLine, "Writes the configured release note", DefaultTheme.DescriptionColor);
        AssertWhitespacePlain(firstDescriptionLine);

        var continuationLines = new List<RenderedLine>();
        for (var i = overflowIndex + 2; i < narrow.Lines.Count && narrow.Lines[i].Text.Trim().Length > 0; i++)
        {
            Assert.StartsWith("    ", narrow.Lines[i].Text, StringComparison.Ordinal);
            continuationLines.Add(narrow.Lines[i]);
        }

        Assert.NotEmpty(continuationLines);
        foreach (var line in continuationLines)
        {
            AssertTextColored(line, line.Text.Trim(), DefaultTheme.DescriptionColor);
            AssertWhitespacePlain(line);
        }

        var wide = Render(model, DefaultTheme, helpWidth: 120);
        var wideIndex = LineIndex(wide.Lines, "    -r, --release-note-doc <STRING>");
        var wideLine = wide.Lines[wideIndex];
        Assert.Equal(37, wideLine.Text.IndexOf("Writes", StringComparison.Ordinal));
        AssertTextColored(wideLine, "-r, --release-note-doc", DefaultTheme.FlagColor);
        AssertTextColored(wideLine, "<STRING>", DefaultTheme.ParameterColor);
        AssertTextColored(wideLine, "Writes the configured release note", DefaultTheme.DescriptionColor);
        AssertWhitespacePlain(wideLine);
    }

    [Fact]
    public void CustomTheme_MapsEveryRoleToSuppliedColors()
    {
        var theme = new UniqueColorTheme();
        var rendered = RenderCommand<ThemeColorCommand>("color-theme", theme, helpWidth: 120);

        var headerColor = Assert.Single(rendered.Output.Writes, write => write.Text == "USAGE:").Color;
        Assert.Equal(theme.HeaderColor, headerColor);

        var formatLine = FindLine(rendered.Lines, "-f, --format <STRING>");
        AssertTextColored(formatLine, "-f, --format", theme.FlagColor);
        AssertTextColored(formatLine, "<STRING>", theme.ParameterColor);
        AssertTextColored(formatLine, "Output", theme.DescriptionColor);

        var valuesLine = FindLine(rendered.Lines, "Possible values:");
        AssertTextColored(valuesLine, "Possible values:", theme.SecondaryColor);
        AssertTextColored(valuesLine, "table", theme.ParameterColor);

        var tokenLine = FindLine(rendered.Lines, "--token");
        var markerIndex = rendered.Lines.IndexOf(tokenLine);
        var markerLine = rendered.Lines[markerIndex + 1];
        Assert.Equal("(required)", markerLine.Text.Trim());
        AssertTextColored(markerLine, "(required)", theme.RequiredColor);

        var usedColors = rendered.Output.Writes
            .Where(write => write.Color.HasValue)
            .Select(write => write.Color!.Value)
            .ToHashSet();
        var expectedColors = new HashSet<ConsoleColor>
        {
            theme.HeaderColor,
            theme.FlagColor,
            theme.ParameterColor,
            theme.DescriptionColor,
            theme.SecondaryColor,
            theme.RequiredColor,
        };
        Assert.Equal(6, expectedColors.Count);
        Assert.Equal(expectedColors, usedColors);
    }

    [Fact]
    public void MonochromeTheme_EmitsOnlyGrayscaleColors()
    {
        var theme = MonochromeConsoleTheme.Instance;
        var rendered = RenderCommand<ThemeColorCommand>("color-theme", theme, helpWidth: 120);

        var usedColors = rendered.Output.Writes
            .Where(write => write.Color.HasValue)
            .Select(write => write.Color!.Value)
            .ToHashSet();
        var grayscale = new HashSet<ConsoleColor>
        {
            ConsoleColor.White,
            ConsoleColor.Gray,
            ConsoleColor.DarkGray,
        };

        Assert.All(usedColors, color => Assert.Contains(color, grayscale));
        Assert.Equal(grayscale, usedColors);
    }

    [Fact]
    public void NonConsoleWriter_WithColorRequest_WritesPlainTextWithoutAnsi()
    {
        var writer = new StringWriter();
        IHelpOutput output = new ConsoleOutput(writer, new StringWriter());

        output.Write("role", ConsoleColor.Red);
        output.WriteLine(" color", ConsoleColor.Green);
        output.WriteLine();

        var text = Normalize(writer.ToString());
        Assert.Equal("role color\n\n", text);
        Assert.DoesNotContain("\u001b", text, StringComparison.Ordinal);
    }

    [Fact]
    public void RedirectedWriter_RendersHelpWithoutAnsi()
    {
        var writer = new StringWriter();
        var output = new ConsoleOutput(writer, new StringWriter());
        var model = BuildCommandModel<OptionRowColorCommand>("color-row");

        new HelpLayoutRenderer(output).Render(model, DefaultTheme, helpWidth: 120, helpBorderWidth: 2);

        var text = Normalize(writer.ToString());
        Assert.DoesNotContain("\u001b", text, StringComparison.Ordinal);
        Assert.Contains("    -f, --format <STRING>", text);
        Assert.Contains("Output format for the deployment report.", text);
    }

    private sealed class UniqueColorTheme : IConsoleTheme
    {
        public ConsoleColor HeaderColor => ConsoleColor.DarkMagenta;

        public ConsoleColor FlagColor => ConsoleColor.DarkYellow;

        public ConsoleColor ParameterColor => ConsoleColor.DarkGreen;

        public ConsoleColor DescriptionColor => ConsoleColor.DarkBlue;

        public ConsoleColor SecondaryColor => ConsoleColor.DarkRed;

        public ConsoleColor RequiredColor => ConsoleColor.DarkCyan;
    }

    private sealed class RecordingHelpOutput : IHelpOutput
    {
        public List<RecordedWrite> Writes { get; } = [];

        public void Write(string? text, ConsoleColor? color = null) =>
            Writes.Add(new RecordedWrite(text ?? string.Empty, color));

        public void WriteLine() =>
            Writes.Add(new RecordedWrite("\n", null));

        public void WriteLine(string? text, ConsoleColor? color = null)
        {
            if (text is not null)
            {
                Writes.Add(new RecordedWrite(text, color));
            }

            Writes.Add(new RecordedWrite("\n", null));
        }
    }

    private readonly record struct RecordedWrite(string Text, ConsoleColor? Color);

    private sealed record RenderedLine(IReadOnlyList<RecordedWrite> Segments)
    {
        public string Text => string.Concat(Segments.Select(segment => segment.Text));
    }

    private sealed record RenderedHelp(RecordingHelpOutput Output, List<RenderedLine> Lines);

    private static RenderedHelp RenderCommand<TCommand>(string commandPath, IConsoleTheme theme, int helpWidth)
        where TCommand : Command
    {
        return Render(BuildCommandModel<TCommand>(commandPath), theme, helpWidth);
    }

    private static RenderedHelp RenderGlobal<TCommand>(IConsoleTheme theme, int helpWidth)
        where TCommand : Command
    {
        return Render(BuildGlobalModel<TCommand>(), theme, helpWidth);
    }

    private static RenderedHelp Render(HelpModel model, IConsoleTheme theme, int helpWidth, int helpBorderWidth = 2)
    {
        var output = new RecordingHelpOutput();
        new HelpLayoutRenderer(output).Render(model, theme, helpWidth, helpBorderWidth);
        return new RenderedHelp(output, ReconstructLines(output));
    }

    private static HelpModel BuildCommandModel<TCommand>(string commandPath)
        where TCommand : Command
    {
        var builder = CreateBuilder().AddCommand<TCommand>();
        var hierarchy = BuildHierarchy(builder);
        var command = hierarchy.RootCommand.FindCommand(
            commandPath.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        Assert.NotNull(command);

        var provider = new HelpContentProvider(
            builder,
            hierarchy.RootCommand,
            new OptionDefaultResolver(builder, builder));
        return provider.BuildCommandModel(command);
    }

    private static HelpModel BuildGlobalModel<TCommand>()
        where TCommand : Command
    {
        var builder = CreateBuilder().AddCommand<TCommand>();
        var hierarchy = BuildHierarchy(builder);
        var provider = new HelpContentProvider(
            builder,
            hierarchy.RootCommand,
            new OptionDefaultResolver(builder, builder));
        return provider.BuildGlobalModel();
    }

    private static CommandHierarchyBuilder BuildHierarchy(ApplicationBuilder builder)
    {
        var hierarchy = new CommandHierarchyBuilder(builder, builder, new CommandReflectionCache());
        hierarchy.BuildCommandHierarchy();
        return hierarchy;
    }

    private static ApplicationBuilder CreateBuilder() =>
        ApplicationBuilder.Create()
            .SetExecutableName("color-role-test")
            .SetExecutableTitle("Color Role Test")
            .SetExecutableDescription("Help color role verification CLI.")
            .SetExecutableVersion("9.9.9")
            .AddCommand<AuxiliaryCommand>();

    private static List<RenderedLine> ReconstructLines(RecordingHelpOutput output)
    {
        var lines = new List<RenderedLine>();
        var segments = new List<RecordedWrite>();

        foreach (var write in output.Writes)
        {
            var offset = 0;
            while (offset < write.Text.Length)
            {
                var newline = write.Text.IndexOf('\n', offset);
                if (newline < 0)
                {
                    AddSegment(segments, write.Text[offset..], write.Color);
                    break;
                }

                AddSegment(segments, write.Text[offset..newline], write.Color);
                lines.Add(new RenderedLine(segments.ToArray()));
                segments.Clear();
                offset = newline + 1;
            }
        }

        if (segments.Count > 0)
        {
            lines.Add(new RenderedLine(segments.ToArray()));
        }

        return lines;
    }

    private static void AddSegment(List<RecordedWrite> segments, string text, ConsoleColor? color)
    {
        if (text.Length > 0)
        {
            segments.Add(new RecordedWrite(text, color));
        }
    }

    private static List<RenderedLine> EntryBlock(List<RenderedLine> lines, int startIndex)
    {
        var block = new List<RenderedLine> { lines[startIndex] };
        for (var i = startIndex + 1; i < lines.Count && lines[i].Text.Trim().Length > 0; i++)
        {
            block.Add(lines[i]);
        }

        return block;
    }

    private static RenderedLine FindLine(List<RenderedLine> lines, string marker) =>
        lines[LineIndex(lines, marker)];

    private static int LineIndex(List<RenderedLine> lines, string marker)
    {
        var index = lines.FindIndex(line => line.Text.Contains(marker, StringComparison.Ordinal));
        Assert.True(index >= 0, $"Expected a rendered line containing '{marker}'. Rendered lines:{Environment.NewLine}{Describe(lines)}");
        return index;
    }

    private static void AssertTextColored(RenderedLine line, string text, ConsoleColor? expected)
    {
        var start = line.Text.IndexOf(text, StringComparison.Ordinal);
        Assert.True(start >= 0, $"Expected line to contain '{text}' but was '{line.Text}'.");
        AssertRangeColored(line, start, text.Length, expected);
    }

    private static void AssertRangeColored(RenderedLine line, int start, int length, ConsoleColor? expected)
    {
        var offset = 0;
        var covered = 0;

        foreach (var segment in line.Segments)
        {
            var segmentStart = offset;
            var segmentEnd = offset + segment.Text.Length;
            offset = segmentEnd;

            var overlapStart = Math.Max(start, segmentStart);
            var overlapEnd = Math.Min(start + length, segmentEnd);
            if (overlapStart >= overlapEnd)
            {
                continue;
            }

            covered += overlapEnd - overlapStart;

            if (segment.Text.All(char.IsWhiteSpace))
            {
                Assert.True(segment.Color is null, $"Expected plain whitespace but '{segment.Text}' was {Describe(segment.Color)} in line '{line.Text}'.");
                continue;
            }

            Assert.True(
                segment.Color == expected,
                $"Expected '{segment.Text}' to be {Describe(expected)} but was {Describe(segment.Color)} in line '{line.Text}'.");
        }

        Assert.Equal(length, covered);
    }

    private static void AssertWhitespacePlain(RenderedLine line)
    {
        foreach (var segment in line.Segments.Where(segment => segment.Text.All(char.IsWhiteSpace)))
        {
            Assert.True(segment.Color is null, $"Expected plain whitespace but '{segment.Text}' was {Describe(segment.Color)} in line '{line.Text}'.");
        }
    }

    private static string Describe(ConsoleColor? color) => color?.ToString() ?? "plain";

    private static string Describe(IEnumerable<RenderedLine> lines) =>
        string.Join(Environment.NewLine, lines.Select(line => line.Text));

    private static string Normalize(string text) =>
        text.Replace("\r\n", "\n", StringComparison.Ordinal);
}
