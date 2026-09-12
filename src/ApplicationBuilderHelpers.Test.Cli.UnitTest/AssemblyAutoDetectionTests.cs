using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.Extensions;
using ApplicationBuilderHelpers.Interfaces;
using Microsoft.Extensions.Hosting;

using System.Reflection;
using System.Text.RegularExpressions;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// In-process assembly-metadata auto-detection tests for the CLI parser.
/// Exercises <c>AssemblyHelpers</c> through the public
/// <see cref="ApplicationBuilder.RunAsync(string[], CancellationToken)"/> entry point
/// by leaving the executable metadata (<c>ExecutableName</c> / <c>ExecutableTitle</c> /
/// <c>ExecutableDescription</c> / <c>ExecutableVersion</c>) unset so the help,
/// version, and error-footer paths fall back to entry-assembly auto-detection.
/// Also verifies that explicitly configured metadata overrides auto-detection.
/// Joins the non-parallel <c>ConsoleDecoupling</c> collection because the
/// console streams are process-global mutable state.
/// </summary>
[Collection("ConsoleDecoupling")]
public sealed class AssemblyAutoDetectionTests
{
    private static readonly SemaphoreSlim ConsoleGate = new(1, 1);

    [Command("autoprobe", "Probes assembly auto-detection.")]
    public sealed class AutoProbeCommand : Command
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            Console.WriteLine("probe ran");
            cancellationTokenSource.Cancel();
            return ValueTask.CompletedTask;
        }
    }

    [Fact]
    public async Task Version_WithoutExplicitVersion_MatchesEntryAssemblyInformationalVersion()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateDefaultBuilder(), ["--version"]);

        Assert.Equal(0, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");

        var version = output.Trim();
        Assert.False(string.IsNullOrWhiteSpace(version));

        var entryAssembly = Assembly.GetEntryAssembly();
        Assert.NotNull(entryAssembly);
        var rawInformational = entryAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        var baseVersion = rawInformational?.Split('+')[0] ?? "0.0.0";
        Assert.StartsWith(baseVersion, version, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Version_ShortFlag_WithoutExplicitVersion_UsesAutoDetection()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateDefaultBuilder(), ["-V"]);

        Assert.Equal(0, exitCode);
        Assert.False(string.IsNullOrWhiteSpace(output.Trim()));
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task GlobalHelp_WithoutExplicitMetadata_HeaderContainsAutoDetectedNameTitleAndVersion()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateDefaultBuilder(), ["--help"]);

        Assert.Equal(0, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");

        var entryAssembly = Assembly.GetEntryAssembly();
        Assert.NotNull(entryAssembly);
        var entryName = entryAssembly.GetName().Name;
        Assert.False(string.IsNullOrEmpty(entryName));
        var expectedTitle = entryAssembly.GetCustomAttribute<AssemblyTitleAttribute>()?.Title;
        if (string.IsNullOrEmpty(expectedTitle))
        {
            expectedTitle = entryName;
        }

        var (versionExitCode, versionOutput, _) = await RunCapturedAsync(CreateDefaultBuilder(), ["--version"]);
        Assert.Equal(0, versionExitCode);
        var autoVersion = versionOutput.Trim();

        var text = StripAnsi(output);
        Assert.Contains(entryName, text, StringComparison.Ordinal);
        Assert.Contains($"v{autoVersion}", text, StringComparison.Ordinal);
        Assert.Contains(expectedTitle, text, StringComparison.Ordinal);
        Assert.Contains("USAGE:", text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GlobalHelp_WithoutExplicitMetadata_ShowsAutoDetectedOrFallbackDescription()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateDefaultBuilder(), ["--help"]);

        Assert.Equal(0, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");

        var entryAssembly = Assembly.GetEntryAssembly();
        Assert.NotNull(entryAssembly);
        var entryName = entryAssembly.GetName().Name;
        Assert.False(string.IsNullOrEmpty(entryName));
        var descriptionAttribute = entryAssembly.GetCustomAttribute<AssemblyDescriptionAttribute>()?.Description;
        var expectedDescription = string.IsNullOrEmpty(descriptionAttribute)
            ? $"Command line application {entryName}"
            : descriptionAttribute;

        Assert.Contains("DESCRIPTION:", StripAnsi(output), StringComparison.Ordinal);
        Assert.Contains(expectedDescription, StripAnsi(output), StringComparison.Ordinal);
    }

    [Fact]
    public async Task CommandHelp_WithoutExplicitMetadata_HeaderUsesAutoDetectedValues()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateDefaultBuilder(), ["autoprobe", "--help"]);

        Assert.Equal(0, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");

        var entryAssembly = Assembly.GetEntryAssembly();
        Assert.NotNull(entryAssembly);
        var entryName = entryAssembly.GetName().Name;
        Assert.False(string.IsNullOrEmpty(entryName));

        var text = StripAnsi(output);
        Assert.Contains(entryName, text, StringComparison.Ordinal);
        Assert.Contains("Probes assembly auto-detection.", text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ErrorFooter_WithoutExplicitName_UsesAutoDetectedName()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateDefaultBuilder(), ["autoprobe", "--unknown-option"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");

        var entryAssembly = Assembly.GetEntryAssembly();
        Assert.NotNull(entryAssembly);
        var entryName = entryAssembly.GetName().Name;
        Assert.False(string.IsNullOrEmpty(entryName));

        Assert.Contains("Error: Unknown option: --unknown-option", error, StringComparison.Ordinal);
        Assert.Contains($"Run '{entryName} <command> --help'", error, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExplicitMetadata_OverridesAutoDetectionInHelp()
    {
        var builder = ApplicationBuilder.Create()
            .SetExecutableName("auto-test")
            .SetExecutableTitle("Auto Test")
            .SetExecutableDescription("Explicit metadata verification CLI.")
            .SetExecutableVersion("9.9.9")
            .AddCommand<AutoProbeCommand>();

        var (exitCode, output, error) = await RunCapturedAsync(builder, ["--help"]);

        Assert.Equal(0, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");

        var text = StripAnsi(output);
        Assert.Contains("auto-test v9.9.9 - Auto Test", text, StringComparison.Ordinal);
        Assert.Contains("Explicit metadata verification CLI.", text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExplicitVersion_OverridesAutoDetection()
    {
        var builder = ApplicationBuilder.Create()
            .SetExecutableName("auto-test")
            .SetExecutableTitle("Auto Test")
            .SetExecutableDescription("Explicit metadata verification CLI.")
            .SetExecutableVersion("9.9.9")
            .AddCommand<AutoProbeCommand>();

        var (exitCode, output, error) = await RunCapturedAsync(builder, ["--version"]);

        Assert.Equal(0, exitCode);
        Assert.Equal("9.9.9", output.Trim());
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task AutoDetectedVersion_ConsistentBetweenHelpHeaderAndVersionOutput()
    {
        var (versionExitCode, versionOutput, _) = await RunCapturedAsync(CreateDefaultBuilder(), ["--version"]);
        Assert.Equal(0, versionExitCode);
        var autoVersion = versionOutput.Trim();
        Assert.False(string.IsNullOrWhiteSpace(autoVersion));

        var (helpExitCode, helpOutput, _) = await RunCapturedAsync(CreateDefaultBuilder(), ["--help"]);
        Assert.Equal(0, helpExitCode);

        var entryAssembly = Assembly.GetEntryAssembly();
        Assert.NotNull(entryAssembly);
        var entryName = entryAssembly.GetName().Name;
        Assert.False(string.IsNullOrEmpty(entryName));

        Assert.Contains($"{entryName} v{autoVersion}", StripAnsi(helpOutput), StringComparison.Ordinal);
    }

    private static ApplicationBuilder CreateDefaultBuilder()
    {
        return ApplicationBuilder.Create()
            .AddCommand<AutoProbeCommand>();
    }

    private static string StripAnsi(string text) =>
        Regex.Replace(text, "\x1B\\[[0-9;]*m", string.Empty);

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
