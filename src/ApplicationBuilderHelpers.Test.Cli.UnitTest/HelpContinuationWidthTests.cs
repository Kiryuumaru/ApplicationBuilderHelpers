using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.Extensions;
using Microsoft.Extensions.Hosting;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// Two-column continuation width tests at help widths 71 and 75.
/// Exercises the public <see cref="ApplicationBuilder.RunAsync(string[], CancellationToken)"/>
/// entry point and pins the absolute right-column budget: wrapped description lines continue
/// at the shared description column and may use the whole configured width. Also freezes the
/// intentionally unchanged overlong-left overflow layout.
/// Runs in the non-parallel <c>ConsoleDecoupling</c> collection.
/// </summary>
[Collection("ConsoleDecoupling")]
public sealed class HelpContinuationWidthTests
{
    private const int HelpWidth = 71;
    private const int LeftColumnWidth = 31;
    private const int BorderWidth = 2;
    private const int DescriptionColumn = LeftColumnWidth + BorderWidth;

    private const int WideHelpWidth = 75;
    private const int WideLeftColumnWidth = 35;
    private const int WideBorderWidth = 2;
    private const int WideDescriptionColumn = WideLeftColumnWidth + WideBorderWidth;
    private const int BorderlessDescriptionColumn = LeftColumnWidth;
    private const int OverflowContinuationIndent = 4;
    private const string ReleaseNoteDescription = "Write the configured release note message to every output channel after the build completes so operators can audit what shipped.";

    private static readonly SemaphoreSlim ConsoleGate = new(1, 1);

    private static readonly string ReleaseNoteBlockAt71 = string.Join(
        '\n',
        "    -m, --release-note <STRING>  Write the configured release note",
        Indented("message to every output channel after"),
        Indented("the build completes so operators can"),
        Indented("audit what shipped."));

    private static readonly string PossibleValuesBlockAt71 = string.Join(
        '\n',
        Indented("Possible values: Immediate, Nightly"),
        Indented("Weekly, Biweekly, Monthly"),
        Indented("Quarterly, Semiannual, Annual"));

    private static readonly string ReleaseNoteDocBlockAt75 = string.Join(
        '\n',
        "    -r, --release-note-doc <STRING>  Write the configured release note",
        Indented(WideDescriptionColumn, "message to every output channel after"),
        Indented(WideDescriptionColumn, "the build completes so operators can"),
        Indented(WideDescriptionColumn, "audit what shipped."));

    private static readonly string ReleaseNoteBlockAt71WithZeroBorder = string.Join(
        '\n',
        "    -m, --release-note <STRING>Write the configured release note",
        Indented(BorderlessDescriptionColumn, "message to every output channel after"),
        Indented(BorderlessDescriptionColumn, "the build completes so operators can"),
        Indented(BorderlessDescriptionColumn, "audit what shipped."));

    private static readonly string OverflowOptionBlockAt71 = string.Join(
        '\n',
        "    -r, --release-note-document <VALUE>",
        "Write the configured release note message to every output channel",
        Indented(OverflowContinuationIndent, "after the build completes so operators can audit what shipped."),
        Indented(OverflowContinuationIndent, "Possible values: Immediate, Nightly, Weekly, Biweekly, Monthly"),
        Indented(OverflowContinuationIndent, "Quarterly, Semiannual, Annual"),
        Indented(OverflowContinuationIndent, "Default: Immediate"));

    public enum ReleaseCadence
    {
        Immediate,
        Nightly,
        Weekly,
        Biweekly,
        Monthly,
        Quarterly,
        Semiannual,
        Annual
    }

    [Command("manifest", "Render the release manifest.")]
    public sealed class ManifestCommand : Command
    {
        [CommandOption('m', "release-note", Description = "Write the configured release note message to every output channel after the build completes so operators can audit what shipped.")]
        public string? ReleaseNote { get; set; }

        [CommandOption('c', "cadence", Description = "Release cadence.")]
        public ReleaseCadence Cadence { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            Console.WriteLine($"manifest:{ReleaseNote}:{Cadence}");
            cancellationTokenSource.Cancel();
            return ValueTask.CompletedTask;
        }
    }

    [Command("archive", "Archive the release manifest.")]
    public sealed class ArchiveCommand : Command
    {
        [CommandOption('k', "keep", Description = "Number of releases to keep.")]
        public int Keep { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            Console.WriteLine($"archive:{Keep}");
            cancellationTokenSource.Cancel();
            return ValueTask.CompletedTask;
        }
    }

    [Command("document", "Render the release documentation.")]
    public sealed class DocumentCommand : Command
    {
        [CommandOption('r', "release-note-doc", Description = ReleaseNoteDescription)]
        public string? ReleaseNoteDoc { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            Console.WriteLine($"document:{ReleaseNoteDoc}");
            cancellationTokenSource.Cancel();
            return ValueTask.CompletedTask;
        }
    }

    [Command("document-set", "Render the release documentation set.")]
    public sealed class OverflowDocumentCommand : Command
    {
        [CommandOption('r', "release-note-document", Description = ReleaseNoteDescription)]
        public ReleaseCadence Cadence { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            Console.WriteLine($"document-set:{Cadence}");
            cancellationTokenSource.Cancel();
            return ValueTask.CompletedTask;
        }
    }

    [Fact]
    public async Task CommandHelp_AtWidth71_WrapsDescriptionUsingFullContinuationWidth()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder(), ["manifest", "--help"]);

        Assert.Equal(0, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");

        var lines = Normalize(output).Split('\n');
        Assert.All(
            lines,
            line => Assert.True(line.Length <= HelpWidth, $"Line exceeds width {HelpWidth} ({line.Length}): {line}"));

        var start = Array.FindIndex(lines, line => line.StartsWith("    -m, --release-note <STRING>", StringComparison.Ordinal));
        Assert.True(start >= 0, "Expected the release-note option row.");

        var continuationIndent = new string(' ', DescriptionColumn);
        var continuationLines = new List<string>();
        for (var i = start + 1; i < lines.Length && lines[i].StartsWith(continuationIndent, StringComparison.Ordinal); i++)
        {
            continuationLines.Add(lines[i]);
        }

        Assert.Equal(3, continuationLines.Count);
        var blockLines = new[] { lines[start] }.Concat(continuationLines).ToArray();
        Assert.Equal(4, blockLines.Length);
        Assert.Equal(ReleaseNoteBlockAt71, string.Join('\n', blockLines));
        Assert.Equal(66, blockLines[0].Length);

        var contents = blockLines.Select(line => line[DescriptionColumn..].TrimEnd()).ToArray();
        Assert.Equal(new[] { 33, 37, 36, 19 }, contents.Select(content => content.Length));
    }

    [Fact]
    public async Task CommandHelp_AtWidth71_KeepsBareQuestionMarkAliasesOnOneLine()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder(), ["manifest", "--help"]);

        Assert.Equal(0, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");

        var lines = Normalize(output).Split('\n');
        Assert.All(
            lines,
            line => Assert.True(line.Length <= HelpWidth, $"Line exceeds width {HelpWidth} ({line.Length}): {line}"));

        var aliasLine = Indented("Also -? /? bare");
        var aliasIndex = Array.IndexOf(lines, aliasLine);
        Assert.True(aliasIndex >= 0, $"Expected '{aliasLine}' on one line.");

        var globalOptionsIndex = Array.FindIndex(lines, line => string.Equals(line, "GLOBAL OPTIONS:", StringComparison.Ordinal));
        Assert.True(globalOptionsIndex >= 0, "Expected the GLOBAL OPTIONS section.");
        Assert.True(aliasIndex > globalOptionsIndex, "Expected the bare aliases in the global-options row.");

        Assert.DoesNotContain(Indented("Also"), lines);
        Assert.DoesNotContain(Indented("-? /?"), lines);
        Assert.DoesNotContain(Indented("bare"), lines);
    }

    [Fact]
    public async Task CommandHelp_AtWidth71_PacksPossibleValuesAcrossContinuationLines()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder(), ["manifest", "--help"]);

        Assert.Equal(0, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");

        var lines = Normalize(output).Split('\n');
        Assert.All(
            lines,
            line => Assert.True(line.Length <= HelpWidth, $"Line exceeds width {HelpWidth} ({line.Length}): {line}"));

        var annotationIndent = Indented("Possible values:");
        var start = Array.FindIndex(lines, line => line.StartsWith(annotationIndent, StringComparison.Ordinal));
        Assert.True(start >= 0, "Expected the Possible values annotation.");

        Assert.Equal(PossibleValuesBlockAt71, string.Join('\n', lines[start..(start + 3)]));
        Assert.All(
            lines[(start + 1)..(start + 3)],
            line => Assert.Contains(",", line[DescriptionColumn..], StringComparison.Ordinal));
    }

    [Fact]
    public async Task CommandHelp_AtWidth75_WrapsDescriptionUsingFullContinuationWidth()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateDocumentBuilder(), ["document", "--help"]);

        Assert.Equal(0, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");

        var lines = Normalize(output).Split('\n');
        Assert.All(
            lines,
            line => Assert.True(line.Length <= WideHelpWidth, $"Line exceeds width {WideHelpWidth} ({line.Length}): {line}"));

        var start = Array.FindIndex(lines, line => line.StartsWith("    -r, --release-note-doc <STRING>", StringComparison.Ordinal));
        Assert.True(start >= 0, "Expected the release-note-doc option row.");

        var continuationIndent = new string(' ', WideDescriptionColumn);
        var continuationLines = new List<string>();
        for (var i = start + 1; i < lines.Length && lines[i].StartsWith(continuationIndent, StringComparison.Ordinal); i++)
        {
            continuationLines.Add(lines[i]);
        }

        Assert.Equal(3, continuationLines.Count);
        var blockLines = new[] { lines[start] }.Concat(continuationLines).ToArray();
        Assert.Equal(4, blockLines.Length);
        Assert.Equal(ReleaseNoteDocBlockAt75, string.Join('\n', blockLines));
        Assert.Equal(70, blockLines[0].Length);

        var contents = blockLines.Select(line => line[WideDescriptionColumn..].TrimEnd()).ToArray();
        Assert.Equal(new[] { 33, 37, 36, 19 }, contents.Select(content => content.Length));
        Assert.Equal(WideDescriptionColumn, blockLines[0].Length - contents[0].Length);

        var rightColumnWidth = WideHelpWidth - WideDescriptionColumn;
        Assert.Equal(38, rightColumnWidth);
        Assert.All(
            contents,
            content => Assert.True(content.Length <= rightColumnWidth, $"Continuation exceeds the {rightColumnWidth}-char right column: {content}"));
    }

    [Fact]
    public async Task CommandHelp_AtWidth71_WithZeroBorder_WrapsDescriptionToColumn31()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBorderlessBuilder(), ["manifest", "--help"]);

        Assert.Equal(0, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");

        var lines = Normalize(output).Split('\n');
        Assert.All(
            lines,
            line => Assert.True(line.Length <= HelpWidth, $"Line exceeds width {HelpWidth} ({line.Length}): {line}"));

        var start = Array.FindIndex(lines, line => line.StartsWith("    -m, --release-note <STRING>", StringComparison.Ordinal));
        Assert.True(start >= 0, "Expected the release-note option row.");

        var continuationIndent = new string(' ', BorderlessDescriptionColumn);
        var continuationLines = new List<string>();
        for (var i = start + 1; i < lines.Length && lines[i].StartsWith(continuationIndent, StringComparison.Ordinal); i++)
        {
            continuationLines.Add(lines[i]);
        }

        Assert.Equal(3, continuationLines.Count);
        var blockLines = new[] { lines[start] }.Concat(continuationLines).ToArray();
        Assert.Equal(4, blockLines.Length);
        Assert.Equal(ReleaseNoteBlockAt71WithZeroBorder, string.Join('\n', blockLines));
        Assert.Equal(64, blockLines[0].Length);

        var contents = blockLines.Select(line => line[BorderlessDescriptionColumn..].TrimEnd()).ToArray();
        Assert.Equal(new[] { 33, 37, 36, 19 }, contents.Select(content => content.Length));
        Assert.Equal(BorderlessDescriptionColumn, blockLines[0].Length - contents[0].Length);

        var rightColumnWidth = HelpWidth - BorderlessDescriptionColumn;
        Assert.Equal(40, rightColumnWidth);
        Assert.All(
            contents,
            content => Assert.True(content.Length <= rightColumnWidth, $"Continuation exceeds the {rightColumnWidth}-char right column: {content}"));
    }

    /// <summary>
    /// Freeze: the overlong-left overflow branch intentionally starts its first description
    /// line at column 0 and indents all later lines by four spaces. This layout must stay
    /// stable across the absolute-column continuation contract.
    /// </summary>
    [Fact]
    public async Task OverflowCommandHelp_AtWidth71_KeepsColumnZeroDescriptionAndFourSpaceContinuations()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateOverflowBuilder(), ["document-set", "--help"]);

        Assert.Equal(0, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");

        var lines = Normalize(output).Split('\n');
        Assert.All(
            lines,
            line => Assert.True(line.Length <= HelpWidth, $"Line exceeds width {HelpWidth} ({line.Length}): {line}"));

        var start = Array.FindIndex(lines, line => string.Equals(line, "    -r, --release-note-document <VALUE>", StringComparison.Ordinal));
        Assert.True(start >= 0 && start + 6 < lines.Length, "Expected the overflow option block.");

        Assert.Equal(OverflowOptionBlockAt71, string.Join('\n', lines[start..(start + 6)]));
        Assert.Equal(string.Empty, lines[start + 6]);
        Assert.Equal(0, lines[start + 1].Length - lines[start + 1].TrimStart().Length);
        Assert.All(
            lines[(start + 2)..(start + 6)],
            line => Assert.True(
                line.Length > OverflowContinuationIndent && !char.IsWhiteSpace(line[OverflowContinuationIndent]),
                $"Expected a four-space continuation indent: {line}"));
    }

    private static string Indented(string text) =>
        new string(' ', DescriptionColumn) + text;

    private static string Indented(int column, string text) =>
        new string(' ', column) + text;

    private static ApplicationBuilder CreateBuilder()
    {
        return ApplicationBuilder.Create()
            .SetExecutableName("continuation-test")
            .SetExecutableTitle("Continuation Test")
            .SetExecutableDescription("Two-column continuation width verification CLI.")
            .SetExecutableVersion("9.9.9")
            .SetHelpWidth(HelpWidth)
            .SetHelpBorderWidth(BorderWidth)
            .AddCommand<ManifestCommand>()
            .AddCommand<ArchiveCommand>();
    }

    private static ApplicationBuilder CreateDocumentBuilder()
    {
        return ApplicationBuilder.Create()
            .SetExecutableName("documentation-test")
            .SetExecutableTitle("Documentation Test")
            .SetExecutableDescription("Two-column continuation width verification CLI.")
            .SetExecutableVersion("9.9.9")
            .SetHelpWidth(WideHelpWidth)
            .SetHelpBorderWidth(WideBorderWidth)
            .AddCommand<DocumentCommand>()
            .AddCommand<ArchiveCommand>();
    }

    private static ApplicationBuilder CreateBorderlessBuilder()
    {
        return ApplicationBuilder.Create()
            .SetExecutableName("continuation-test")
            .SetExecutableTitle("Continuation Test")
            .SetExecutableDescription("Two-column continuation width verification CLI.")
            .SetExecutableVersion("9.9.9")
            .SetHelpWidth(HelpWidth)
            .SetHelpBorderWidth(0)
            .AddCommand<ManifestCommand>()
            .AddCommand<ArchiveCommand>();
    }

    private static ApplicationBuilder CreateOverflowBuilder()
    {
        return ApplicationBuilder.Create()
            .SetExecutableName("overflow-test")
            .SetExecutableTitle("Overflow Test")
            .SetExecutableDescription("Overflow row verification CLI.")
            .SetExecutableVersion("9.9.9")
            .SetHelpWidth(HelpWidth)
            .SetHelpBorderWidth(BorderWidth)
            .AddCommand<OverflowDocumentCommand>()
            .AddCommand<ArchiveCommand>();
    }

    private static string Normalize(string text) =>
        text.Replace("\r\n", "\n", StringComparison.Ordinal);

    private static async Task<(int ExitCode, string Output, string Error)> RunCapturedAsync(ApplicationBuilder builder, string[] args)
    {
        await ConsoleGate.WaitAsync();
        try
        {
            var originalOut = Console.Out;
            var originalError = Console.Error;
            using var outWriter = new StringWriter();
            using var errorWriter = new StringWriter();
            Console.SetOut(outWriter);
            Console.SetError(errorWriter);
            try
            {
                var exitCode = await builder.RunAsync(args);
                outWriter.Flush();
                errorWriter.Flush();
                return (exitCode, outWriter.ToString(), errorWriter.ToString());
            }
            finally
            {
                Console.SetOut(originalOut);
                Console.SetError(originalError);
            }
        }
        finally
        {
            ConsoleGate.Release();
        }
    }
}
