using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.Extensions;
using Microsoft.Extensions.Hosting;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// Single-letter long names gain a single-dash alias: <c>[CommandOption("a")]</c>
/// binds both <c>-a</c> and <c>--a</c>. Runs in the non-parallel
/// <c>ConsoleDecoupling</c> collection.
/// </summary>
[Collection("ConsoleDecoupling")]
public sealed class SingleLetterAliasTests
{
    private static readonly SemaphoreSlim ConsoleGate = new(1, 1);

    [Command("single", "Probes single-letter long auto short alias.")]
    public sealed class SingleLetterProbeCommand : Command
    {
        [CommandOption("a", Description = "Alpha value.")]
        public string? Alpha { get; set; }

        [CommandOption('b', "b", Description = "Beta value.")]
        public string? Beta { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Alpha: {Alpha ?? "null"}");
            Console.WriteLine($"Beta: {Beta ?? "null"}");
            return ValueTask.CompletedTask;
        }
    }

    [Fact]
    public async Task ShortForm_BindsSingleLetterLong()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["single", "-a", "x"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Alpha: x", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task LongForm_StillBindsSingleLetterLong()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["single", "--a", "y"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Alpha: y", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task AttachedShortForm_BindsSingleLetterLong()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["single", "-az"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Alpha: z", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task InferredSingleLetterProperty_GainsShortAlias()
    {
        var property = typeof(InferredSingleLetterHolder).GetProperty(nameof(InferredSingleLetterHolder.A))!;
        var info = CommandLineParser.SubCommandOptionInfo.FromProperty(
            property, new CommandOptionAttribute("placeholder") { Term = null });

        Assert.Equal("a", info.LongName);
        Assert.Equal('a', info.ShortName);
        Assert.True(info.MatchesArgument("-a"));
        Assert.True(info.MatchesArgument("--a"));
        Assert.Equal("-a, --a", info.GetDisplayName());
    }

    [Fact]
    public async Task ReservedShort_PromotedSingleLetter_FaultsFailClosed()
    {
        var (exitCode, output, error) = await RunHarnessAsync<ReservedSingleLetterProbeCommand>(["resh"]);

        Assert.Equal(1, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Reserved short name conflict", error);
        Assert.Contains("'-h'", error);
    }

    [Fact]
    public async Task VersionShort_PromotedSingleLetter_FaultsFailClosed()
    {
        var (exitCode, output, error) = await RunHarnessAsync<ReservedVersionLetterProbeCommand>(["resv"]);

        Assert.Equal(1, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Reserved short name conflict", error);
        Assert.Contains("'-V'", error);
    }

    [Fact]
    public async Task ExplicitShort_WinsOverSingleLetterLong()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["single", "-b", "q"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Beta: q", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    private sealed class InferredSingleLetterHolder
    {
        public string? A { get; set; }
    }

    [Command("resh", "Probes promoted reserved single-letter long.")]
    private sealed class ReservedSingleLetterProbeCommand : Command
    {
        [CommandOption("h", Description = "Host value.")]
        public string? Host { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken) =>
            ValueTask.CompletedTask;
    }

    [Command("resv", "Probes promoted version single-letter long.")]
    private sealed class ReservedVersionLetterProbeCommand : Command
    {
        [CommandOption("V", Description = "Verbose value.")]
        public string? Verbose { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken) =>
            ValueTask.CompletedTask;
    }

    private static async Task<(int ExitCode, string Output, string Error)> RunHarnessAsync<TCommand>(string[] args)
        where TCommand : Command, new()
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
                var exitCode = await ApplicationBuilder.Create()
                    .SetExecutableName("single-letter-test")
                    .SetExecutableTitle("Single Letter Test")
                    .SetExecutableDescription("Single letter alias verification CLI.")
                    .SetExecutableVersion("9.9.9")
                    .AddCommand<TCommand>()
                    .RunAsync(args);
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

    private static ApplicationBuilder CreateBuilder()
    {
        return ApplicationBuilder.Create()
            .SetExecutableName("single-letter-test")
            .SetExecutableTitle("Single Letter Test")
            .SetExecutableDescription("Single letter alias verification CLI.")
            .SetExecutableVersion("9.9.9")
            .AddCommand<SingleLetterProbeCommand>();
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
