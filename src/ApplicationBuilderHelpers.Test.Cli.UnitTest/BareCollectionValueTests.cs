using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.Extensions;
using Microsoft.Extensions.Hosting;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// In-process bare-collection tests for the CLI parser.
/// A bare valued collection (long, short, or trailing cluster) with no merged
/// value fails MissingRequired (exit 2); accumulation and help neighbors stay intact.
/// Runs in the non-parallel <c>ConsoleDecoupling</c> collection.
/// </summary>
[Collection("ConsoleDecoupling")]
public sealed class BareCollectionValueTests
{
    private static readonly SemaphoreSlim ConsoleGate = new(1, 1);

    [Command("colprobe", "Probes required collection binding.")]
    public sealed class RequiredCollectionCommand : Command
    {
        [CommandOption('t', "tags", Description = "Tags.", Required = true)]
        public string[]? Tags { get; set; }

        [CommandOption('v', "verbose", Description = "Verbose flag.")]
        public bool Verbose { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Tags: {(Tags is null ? "null" : string.Join(",", Tags))}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("colopt", "Probes optional collection binding.")]
    public sealed class OptionalCollectionCommand : Command
    {
        [CommandOption('g', "groups", Description = "Groups.")]
        public string[]? Groups { get; set; }

        [CommandOption('v', "verbose", Description = "Verbose flag.")]
        public bool Verbose { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Groups: {(Groups is null ? "null" : string.Join(",", Groups))}");
            return ValueTask.CompletedTask;
        }
    }

    [Fact]
    public async Task Required_Long_Bare_ReportsMissing()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["colprobe", "--tags"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Missing required option: -t, --tags", error);
    }

    [Fact]
    public async Task Required_Short_Bare_ReportsMissing()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["colprobe", "-t"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Missing required option: -t, --tags", error);
    }

    [Fact]
    public async Task Required_Cluster_Bare_ReportsMissing()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["colprobe", "-vt"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Missing required option: -t, --tags", error);
    }

    [Fact]
    public async Task Optional_Long_Bare_ReportsMissing()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["colopt", "--groups"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Missing value for option: -g, --groups", error);
    }

    [Fact]
    public async Task Optional_Short_Bare_ReportsMissing()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["colopt", "-g"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Missing value for option: -g, --groups", error);
    }

    [Fact]
    public async Task Optional_Cluster_Bare_ReportsMissing()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["colopt", "-vg"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Missing value for option: -g, --groups", error);
    }

    [Fact]
    public async Task Required_Accumulation_Intact()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["colprobe", "--tags=a", "--tags=b"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Tags: a,b", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task Optional_Accumulation_Intact()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["colopt", "--groups=a", "--groups=b"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Groups: a,b", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task Optional_Bare_With_Help_Neighbor_ShowsHelp()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["colopt", "--groups", "--help"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("USAGE:", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task Required_SatisfiedThenBare_ReportsMissing()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["colprobe", "--tags=a", "--tags"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Missing required option: -t, --tags", error);
    }

    [Fact]
    public async Task Optional_SatisfiedThenBare_KeepsValues()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["colopt", "--groups=a", "--groups"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Groups: a", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task Optional_BareThenValued_Heals()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["colopt", "--groups", "--groups=a"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Groups: a", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    private static ApplicationBuilder CreateBuilder()
    {
        return ApplicationBuilder.Create()
            .SetExecutableName("bare-collection-test")
            .SetExecutableTitle("Bare Collection Test")
            .SetExecutableDescription("Bare collection verification CLI.")
            .SetExecutableVersion("9.9.9")
            .AddCommand<RequiredCollectionCommand>()
            .AddCommand<OptionalCollectionCommand>();
    }

    private static async Task<(int ExitCode, string Output, string Error)> RunCapturedAsync(string[] args)
        => await RunCapturedAsync(CreateBuilder(), args);

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
