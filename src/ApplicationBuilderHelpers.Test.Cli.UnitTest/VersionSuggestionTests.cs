using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.Extensions;
using Microsoft.Extensions.Hosting;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// Pins that the virtual <c>--version</c> and <c>--help</c> flags compete in
/// did-you-mean ranking: transposed input (e.g. <c>--versoin</c>) suggests
/// <c>--version</c>, <c>--ver</c> wins over <c>--verbose</c> on an exact
/// distance+prefix (4-4) tie while a strictly closer user option still wins,
/// and <c>--hepl</c> suggests <c>--help</c> exactly once.
/// Runs in the non-parallel <c>ConsoleDecoupling</c> collection.
/// </summary>
[Collection("ConsoleDecoupling")]
public sealed class VersionSuggestionTests
{
    private static readonly SemaphoreSlim ConsoleGate = new(1, 1);

    [Command("probe", "Probes version suggestion ranking.")]
    public sealed class SuggestProbeCommand : Command
    {
        [CommandOption("verbose", Description = "Verbose flag.")]
        public bool Verbose { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"probe:{Verbose}");
            return ValueTask.CompletedTask;
        }
    }

    [Fact]
    public async Task Unknown_Option_Versoin_Suggests_Version()
    {
        var (exitCode, _, error) = await RunCapturedAsync(["probe", "--versoin"]);
        Assert.Equal(2, exitCode);
        Assert.Contains("Unknown option: --versoin", error);
        Assert.Contains("Did you mean '--version'?", error);
    }

    [Fact]
    public async Task Unknown_Option_Ver_Suggests_Version_Over_Verbose()
    {
        var (exitCode, _, error) = await RunCapturedAsync(["probe", "--ver"]);
        Assert.Equal(2, exitCode);
        Assert.Contains("Unknown option: --ver", error);
        Assert.Contains("Did you mean '--version'?", error);
        Assert.DoesNotContain("Did you mean '--verbose'?", error);
    }

    [Fact]
    public async Task Unknown_Option_Hepl_Suggests_Help_Exactly_Once()
    {
        var (exitCode, _, error) = await RunCapturedAsync(["probe", "--hepl"]);
        Assert.Equal(2, exitCode);
        Assert.Contains("Unknown option: --hepl", error);
        Assert.Contains("Did you mean '--help'?", error);
        Assert.Equal(1, CountOccurrences(error, "Did you mean '--help'?"));
    }

    [Fact]
    public async Task Exact_Version_Still_Shows_Version()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["probe", "--version"]);
        Assert.Equal(0, exitCode);
        Assert.Contains("9.9.9", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task Exact_Verbose_Still_Binds()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["probe", "--verbose"]);
        Assert.Equal(0, exitCode);
        Assert.Contains("probe:True", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public void Reserved_Wins_Exact_Distance_And_Prefix_Tie_Regardless_Of_Order()
    {
        var verboseFirst = new List<(string Key, string Display)> { ("verbose", "--verbose"), ("version", "--version") };
        var versionFirst = new List<(string Key, string Display)> { ("version", "--version"), ("verbose", "--verbose") };

        Assert.Equal("--version", CommandLineParser.DidYouMean.FindBestMatch("ver", verboseFirst));
        Assert.Equal("--version", CommandLineParser.DidYouMean.FindBestMatch("ver", versionFirst));
    }

    [Fact]
    public void Closer_Distance_Beats_Reserved_Regardless_Of_Order()
    {
        var verboseFirst = new List<(string Key, string Display)> { ("verbose", "--verbose"), ("version", "--version") };
        var versionFirst = new List<(string Key, string Display)> { ("version", "--version"), ("verbose", "--verbose") };

        Assert.Equal("--version", CommandLineParser.DidYouMean.FindBestMatch("versoin", verboseFirst));
        Assert.Equal("--version", CommandLineParser.DidYouMean.FindBestMatch("versoin", versionFirst));
        Assert.Equal("--verbose", CommandLineParser.DidYouMean.FindBestMatch("verbosex", verboseFirst));
        Assert.Equal("--verbose", CommandLineParser.DidYouMean.FindBestMatch("verbosex", versionFirst));
    }

    [Fact]
    public void Exact_Match_Boundary_Unchanged()
    {
        var candidates = new List<(string Key, string Display)> { ("verbose", "--verbose"), ("version", "--version") };

        Assert.Equal("--verbose", CommandLineParser.DidYouMean.FindBestMatch("verbose", candidates));
        Assert.Equal("--version", CommandLineParser.DidYouMean.FindBestMatch("version", candidates));
    }

    private static int CountOccurrences(string text, string value)
    {
        var count = 0;
        var index = 0;
        while ((index = text.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
    }

    private static ApplicationBuilder CreateBuilder()
    {
        return ApplicationBuilder.Create()
            .SetExecutableName("versionsuggest-test")
            .SetExecutableTitle("VersionSuggest Test")
            .SetExecutableDescription("Version suggestion verification CLI.")
            .SetExecutableVersion("9.9.9")
            .AddCommand<SuggestProbeCommand>();
    }

    private static async Task<(int ExitCode, string Output, string Error)> RunCapturedAsync(string[] args)
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
                var exitCode = await CreateBuilder().RunAsync(args);
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
