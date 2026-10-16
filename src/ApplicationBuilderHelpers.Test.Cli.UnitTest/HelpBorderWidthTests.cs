using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.Extensions;
using Microsoft.Extensions.Hosting;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// Help inter-column gap tests for <c>SetHelpBorderWidth</c>.
/// Exercises the public <see cref="ApplicationBuilder.RunAsync(string[], CancellationToken)"/>
/// entry point and pins the observable gap effect: unset renders exactly like an
/// explicit width of 2, zero removes the padding, and wider values shift every
/// description column uniformly. Verifies the default help width stays orthogonal.
/// Runs in the non-parallel <c>ConsoleDecoupling</c> collection.
/// </summary>
[Collection("ConsoleDecoupling")]
public sealed class HelpBorderWidthTests
{
    private static readonly SemaphoreSlim ConsoleGate = new(1, 1);

    [Command("deploy", "Deploy the application to the target environment.")]
    public sealed class DeployCommand : Command
    {
        [CommandOption("format", Description = "Output format for the deployment report.")]
        public string Format { get; set; } = "table";

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            Console.WriteLine($"deploy:{Format}");
            cancellationTokenSource.Cancel();
            return ValueTask.CompletedTask;
        }
    }

    [Command("publish", "Publish the specified artifact.")]
    public sealed class PublishCommand : Command
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            Console.WriteLine("publish ran");
            cancellationTokenSource.Cancel();
            return ValueTask.CompletedTask;
        }
    }

    [Fact]
    public async Task UnsetBorderWidth_MatchesExplicitDefaultWidth()
    {
        var (unsetExitCode, unsetOutput, unsetError) = await RunCapturedAsync(CreateBuilder(null), ["--help"]);
        var (explicitExitCode, explicitOutput, explicitError) = await RunCapturedAsync(CreateBuilder(2), ["--help"]);

        Assert.Equal(0, unsetExitCode);
        Assert.Equal(0, explicitExitCode);
        Assert.True(string.IsNullOrWhiteSpace(unsetError), $"Expected empty stderr but got: {unsetError}");
        Assert.True(string.IsNullOrWhiteSpace(explicitError), $"Expected empty stderr but got: {explicitError}");
        Assert.Equal(Normalize(unsetOutput), Normalize(explicitOutput));
    }

    [Fact]
    public async Task ZeroBorderWidth_RemovesInterColumnGap()
    {
        var (unsetExitCode, unsetOutput, unsetError) = await RunCapturedAsync(CreateBuilder(null), ["--help"]);
        var (zeroExitCode, zeroOutput, zeroError) = await RunCapturedAsync(CreateBuilder(0), ["--help"]);

        Assert.Equal(0, unsetExitCode);
        Assert.Equal(0, zeroExitCode);
        Assert.True(string.IsNullOrWhiteSpace(unsetError), $"Expected empty stderr but got: {unsetError}");
        Assert.True(string.IsNullOrWhiteSpace(zeroError), $"Expected empty stderr but got: {zeroError}");

        var unsetNormalized = Normalize(unsetOutput);
        var zeroNormalized = Normalize(zeroOutput);

        Assert.NotEqual(unsetNormalized, zeroNormalized);
        Assert.Contains("Deploy the application to the target environment.", zeroNormalized);
        Assert.Contains("Publish the specified artifact.", zeroNormalized);
        AssertUniformShift(DescriptionColumnOffsets(unsetNormalized), DescriptionColumnOffsets(zeroNormalized), -2);
    }

    [Fact]
    public async Task WideBorderWidth_WidensInterColumnGap()
    {
        var (unsetExitCode, unsetOutput, unsetError) = await RunCapturedAsync(CreateBuilder(null), ["--help"]);
        var (wideExitCode, wideOutput, wideError) = await RunCapturedAsync(CreateBuilder(8), ["--help"]);

        Assert.Equal(0, unsetExitCode);
        Assert.Equal(0, wideExitCode);
        Assert.True(string.IsNullOrWhiteSpace(unsetError), $"Expected empty stderr but got: {unsetError}");
        Assert.True(string.IsNullOrWhiteSpace(wideError), $"Expected empty stderr but got: {wideError}");

        var unsetNormalized = Normalize(unsetOutput);
        var wideNormalized = Normalize(wideOutput);

        Assert.NotEqual(unsetNormalized, wideNormalized);
        Assert.Contains("Deploy the application to the target environment.", wideNormalized);
        Assert.Contains("Publish the specified artifact.", wideNormalized);
        AssertUniformShift(DescriptionColumnOffsets(unsetNormalized), DescriptionColumnOffsets(wideNormalized), 6);
    }

    [Fact]
    public async Task CommandHelp_RespectsBorderWidth()
    {
        var (unsetExitCode, unsetOutput, unsetError) = await RunCapturedAsync(CreateBuilder(null), ["deploy", "--help"]);
        var (zeroExitCode, zeroOutput, zeroError) = await RunCapturedAsync(CreateBuilder(0), ["deploy", "--help"]);

        Assert.Equal(0, unsetExitCode);
        Assert.Equal(0, zeroExitCode);
        Assert.True(string.IsNullOrWhiteSpace(unsetError), $"Expected empty stderr but got: {unsetError}");
        Assert.True(string.IsNullOrWhiteSpace(zeroError), $"Expected empty stderr but got: {zeroError}");

        var unsetNormalized = Normalize(unsetOutput);
        var zeroNormalized = Normalize(zeroOutput);

        Assert.NotEqual(unsetNormalized, zeroNormalized);
        Assert.Contains("Output format for the deployment report.", zeroNormalized);
        Assert.Contains("    --format <STRING>Output format for the deployment report.", zeroNormalized);
        Assert.DoesNotContain("    --format <STRING>  Output format for the deployment report.", zeroNormalized);
        Assert.Equal(
            EntryOffset(unsetNormalized, "Show help information") - 2,
            EntryOffset(zeroNormalized, "Show help information"));
        Assert.Equal(
            EntryOffset(unsetNormalized, "Show version information") - 2,
            EntryOffset(zeroNormalized, "Show version information"));
    }

    [Fact]
    public async Task ExplicitDefaultHelpWidth_MatchesUnsetOutput()
    {
        var (unsetExitCode, unsetOutput, unsetError) = await RunCapturedAsync(CreateBuilder(null), ["--help"]);
        var (explicitExitCode, explicitOutput, explicitError) = await RunCapturedAsync(CreateBuilder(null, 120), ["--help"]);

        Assert.Equal(0, unsetExitCode);
        Assert.Equal(0, explicitExitCode);
        Assert.True(string.IsNullOrWhiteSpace(unsetError), $"Expected empty stderr but got: {unsetError}");
        Assert.True(string.IsNullOrWhiteSpace(explicitError), $"Expected empty stderr but got: {explicitError}");
        Assert.Equal(Normalize(unsetOutput), Normalize(explicitOutput));
    }

    private static int EntryOffset(string output, string marker)
    {
        foreach (var line in output.Split('\n'))
        {
            var index = line.IndexOf(marker, StringComparison.Ordinal);
            if (index >= 0)
            {
                return index;
            }
        }

        throw new Xunit.Sdk.XunitException($"Expected output to contain '{marker}'.");
    }

    private static void AssertUniformShift(List<int> baseline, List<int> actual, int expectedShift)
    {
        Assert.Equal(baseline.Count, actual.Count);
        Assert.True(baseline.Count >= 2, "Expected entries in at least two sections.");
        Assert.All(
            baseline.Zip(actual, (baselineOffset, actualOffset) => actualOffset - baselineOffset),
            shift => Assert.Equal(expectedShift, shift));
    }

    private static List<int> DescriptionColumnOffsets(string output)
    {
        var offsets = new List<int>();
        foreach (var line in output.Split('\n'))
        {
            if (!line.StartsWith("    ", StringComparison.Ordinal))
            {
                continue;
            }

            var body = line[4..].TrimEnd();
            if (body.Length == 0 || char.IsWhiteSpace(body[0]))
            {
                continue;
            }

            var separator = body.IndexOf("  ", StringComparison.Ordinal);
            if (separator < 0)
            {
                continue;
            }

            var descriptionStart = separator;
            while (descriptionStart < body.Length && body[descriptionStart] == ' ')
            {
                descriptionStart++;
            }

            if (descriptionStart >= body.Length)
            {
                continue;
            }

            offsets.Add(4 + descriptionStart);
        }

        return offsets;
    }

    private static ApplicationBuilder CreateBuilder(int? helpBorderWidth, int? helpWidth = null)
    {
        var builder = ApplicationBuilder.Create()
            .SetExecutableName("border-width-test")
            .SetExecutableTitle("Border Width Test")
            .SetExecutableDescription("Help border width verification CLI.")
            .SetExecutableVersion("9.9.9")
            .AddCommand<DeployCommand>()
            .AddCommand<PublishCommand>();

        if (helpBorderWidth.HasValue)
        {
            builder.SetHelpBorderWidth(helpBorderWidth.Value);
        }

        if (helpWidth.HasValue)
        {
            builder.SetHelpWidth(helpWidth.Value);
        }

        return builder;
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
