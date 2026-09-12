using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.Extensions;
using Microsoft.Extensions.Hosting;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// Tests for issue #371 did-you-mean suggestions at CLI dead-ends.
/// Exit-code contract: every scenario below fails with exit code 2
/// (usage error per the structured CommandErrorKind contract).
/// Joins the non-parallel <c>ConsoleDecoupling</c> collection because the
/// console streams are process-global mutable state.
/// </summary>
[Collection("ConsoleDecoupling")]
public sealed class DidYouMeanTests
{
    private static readonly SemaphoreSlim ConsoleGate = new(1, 1);

    [Command("deploy", "Probes subcommand suggestions.")]
    public sealed class SuggestDeployCommand : Command
    {
        [CommandOption("verbosity", Description = "Verbosity level.", FromAmong = ["quiet", "normal", "verbose"])]
        public string Verbosity { get; set; } = "normal";

        [CommandOption("target", Description = "Deploy target.")]
        public string? Target { get; set; }

        [CommandArgument("environment", Description = "Target environment.", Position = 0, Required = true)]
        public required string Environment { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"deploy:{Environment}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("config", "Probes surplus subcommand suggestions.")]
    public sealed class SuggestConfigCommand : Command
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine("config");
            return ValueTask.CompletedTask;
        }
    }

    [Command("config get", "Probes surplus subcommand suggestions.")]
    public sealed class SuggestConfigGetCommand : Command
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine("config get");
            return ValueTask.CompletedTask;
        }
    }

    [Command("config set", "Probes surplus subcommand suggestions.")]
    public sealed class SuggestConfigSetCommand : Command
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine("config set");
            return ValueTask.CompletedTask;
        }
    }

    [Fact]
    public async Task Unknown_Option_Typo_Suggests_Long_Name()
    {
        var (exitCode, _, error) = await RunCapturedAsync(["deploy", "prod", "--verbosit"]);
        Assert.Equal(2, exitCode);
        Assert.Contains("Unknown option: --verbosit", error);
        Assert.Contains("Did you mean '--verbosity'?", error);
    }

    [Fact]
    public async Task Unknown_Option_Case_Insensitive_Suggests()
    {
        var (exitCode, _, error) = await RunCapturedAsync(["deploy", "prod", "--VERBOSITY"]);
        Assert.Equal(2, exitCode);
        Assert.Contains("Unknown option: --VERBOSITY", error);
        Assert.Contains("Did you mean '--verbosity'?", error);
    }

    [Fact]
    public async Task Unknown_Option_Transposition_Suggests()
    {
        var (exitCode, _, error) = await RunCapturedAsync(["deploy", "prod", "--verbosiyt"]);
        Assert.Equal(2, exitCode);
        Assert.Contains("Unknown option: --verbosiyt", error);
        Assert.Contains("Did you mean '--verbosity'?", error);
    }

    [Fact]
    public async Task Unknown_Option_Far_Miss_Stays_Silent()
    {
        var (exitCode, _, error) = await RunCapturedAsync(["deploy", "prod", "--zzzzqqqq"]);
        Assert.Equal(2, exitCode);
        Assert.Contains("Unknown option: --zzzzqqqq", error);
        Assert.DoesNotContain("Did you mean", error);
    }

    [Fact]
    public async Task Unknown_Option_Same_Initial_Far_Miss_Stays_Silent()
    {
        var (exitCode, _, error) = await RunCapturedAsync(["deploy", "prod", "--verbosity-garbage-xyz"]);
        Assert.Equal(2, exitCode);
        Assert.Contains("Unknown option: --verbosity-garbage-xyz", error);
        Assert.DoesNotContain("Did you mean", error);
    }

    [Fact]
    public async Task Unknown_Option_Equals_Value_Typo_Suggests()
    {
        var (exitCode, _, error) = await RunCapturedAsync(["deploy", "prod", "--verbosit=quiet"]);
        Assert.Equal(2, exitCode);
        Assert.Contains("Unknown option: --verbosit=quiet", error);
        Assert.Contains("Did you mean '--verbosity'?", error);
    }

    [Fact]
    public void Pure_Adjacent_Transposition_Scores_Exactly_One()
    {
        Assert.Equal(1, CommandLineParser.DidYouMean.DamerauLevenshtein("verbosiyt", "verbosity"));
        Assert.Equal(1, CommandLineParser.DidYouMean.DamerauLevenshtein("ab", "ba"));
    }

    [Fact]
    public async Task Zero_Match_Command_Typo_Suggests()
    {
        var (exitCode, _, error) = await RunCapturedAsync(["deply"]);
        Assert.Equal(2, exitCode);
        Assert.Contains("No command found for 'deply'", error);
        Assert.Contains("Did you mean 'deploy'?", error);
    }

    [Fact]
    public async Task Zero_Match_Command_Far_Miss_Stays_Silent()
    {
        var (exitCode, _, error) = await RunCapturedAsync(["zzzzqqqq"]);
        Assert.Equal(2, exitCode);
        Assert.Contains("No command found for 'zzzzqqqq'", error);
        Assert.DoesNotContain("Did you mean", error);
    }

    [Fact]
    public async Task Surplus_Close_To_Child_Is_Unknown_Subcommand_With_Suggestion()
    {
        var (exitCode, _, error) = await RunCapturedAsync(["config", "gett"]);
        Assert.Equal(2, exitCode);
        Assert.Contains("Unknown subcommand 'gett'", error);
        Assert.Contains("Did you mean 'get'?", error);
    }

    [Fact]
    public async Task Surplus_Far_From_Children_Is_Unexpected_Argument()
    {
        var (exitCode, _, error) = await RunCapturedAsync(["config", "zzzzqqqq"]);
        Assert.Equal(2, exitCode);
        Assert.Contains("Unexpected argument 'zzzzqqqq'", error);
        Assert.DoesNotContain("Did you mean", error);
    }

    [Fact]
    public async Task Leaf_Surplus_Extra_Argument_Has_No_Suggestion()
    {
        var (exitCode, _, error) = await RunCapturedAsync(["deploy", "prod", "extra"]);
        Assert.Equal(2, exitCode);
        Assert.Contains("Unexpected argument 'extra'", error);
        Assert.DoesNotContain("Did you mean", error);
    }

    private static ApplicationBuilder CreateBuilder()
    {
        return ApplicationBuilder.Create()
            .SetExecutableName("didyoumean-test")
            .SetExecutableTitle("DidYouMean Test")
            .SetExecutableDescription("Did-you-mean verification CLI.")
            .SetExecutableVersion("9.9.9")
            .AddCommand<SuggestDeployCommand>()
            .AddCommand<SuggestConfigCommand>()
            .AddCommand<SuggestConfigGetCommand>()
            .AddCommand<SuggestConfigSetCommand>();
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
