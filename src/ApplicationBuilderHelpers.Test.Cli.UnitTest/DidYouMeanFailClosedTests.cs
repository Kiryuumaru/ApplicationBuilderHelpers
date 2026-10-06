using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.Extensions;
using Microsoft.Extensions.Hosting;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// Fail-closed typo suggestions: confident near-misses suggest, ambiguous
/// ties and exact tokens stay silent with the plain unknown-token error.
/// Runs in the non-parallel <c>ConsoleDecoupling</c> collection.
/// </summary>
[Collection("ConsoleDecoupling")]
public sealed class DidYouMeanFailClosedTests
{
    private static readonly SemaphoreSlim ConsoleGate = new(1, 1);

    [Command("deploy", "Probes option suggestions.")]
    public sealed class FailClosedDeployCommand : Command
    {
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

    [Command("config", "Probes subcommand suggestions.")]
    public sealed class FailClosedConfigCommand : Command
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine("config");
            return ValueTask.CompletedTask;
        }
    }

    [Command("config get", "Gets a config value.")]
    public sealed class FailClosedConfigGetCommand : Command
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine("config get");
            return ValueTask.CompletedTask;
        }
    }

    [Command("config set", "Sets a config value.")]
    public sealed class FailClosedConfigSetCommand : Command
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine("config set");
            return ValueTask.CompletedTask;
        }
    }

    [Command("greet", "Greets.")]
    public sealed class FailClosedGreetCommand : Command
    {
        [CommandOption('a', "alpha", Description = "Alpha flag.")]
        public bool Alpha { get; set; }

        [CommandOption('b', "beta", Description = "Beta flag.")]
        public bool Beta { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine("greet");
            return ValueTask.CompletedTask;
        }
    }

    [Command("hub", "Abstract hub.")]
    public abstract class FailClosedAbstractHub : Command
    {
    }

    [Command("hub get", "Gets a hub value.")]
    public sealed class FailClosedHubGetCommand : FailClosedAbstractHub
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine("hub get");
            return ValueTask.CompletedTask;
        }
    }

    [Command("hub set", "Sets a hub value.")]
    public sealed class FailClosedHubSetCommand : FailClosedAbstractHub
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine("hub set");
            return ValueTask.CompletedTask;
        }
    }

    [Fact]
    public async Task Confident_Typo_Still_Suggests()
    {
        var (exitCode, _, error) = await RunCapturedAsync(["deploy", "--targt", "prod"]);
        Assert.Equal(2, exitCode);
        Assert.Contains("Unknown option: --targt", error);
        Assert.Contains("Did you mean '--target'?", error);
    }

    [Fact]
    public async Task Uppercase_H_Hints_Lowercase_Help_Short()
    {
        var (exitCode, _, error) = await RunCapturedAsync(["deploy", "-H", "prod"]);
        Assert.Equal(2, exitCode);
        Assert.Contains("Unknown option: -H", error);
        Assert.Contains("Did you mean '-h'?", error);
    }

    [Fact]
    public async Task Leaf_Surplus_Tie_Stays_Silent_With_Proper_Error()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["config", "xet"]);
        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Unexpected argument 'xet'", error);
        Assert.DoesNotContain("Did you mean", error);
        Assert.DoesNotContain("Unknown subcommand", error);
        Assert.Contains("Run 'failclosed-test config --help' for more information on specific command options.", error);
    }

    [Fact]
    public async Task Abstract_Hub_Tie_Stays_Silent_With_Subcommand_Choices()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["hub", "xet"]);
        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("'hub' requires a subcommand", error);
        Assert.Contains("Available subcommands: get, set", error);
        Assert.DoesNotContain("Did you mean", error);
        Assert.DoesNotContain("Unknown subcommand", error);
        Assert.Contains("Run 'failclosed-test hub --help' to see available subcommands and options.", error);
    }

    [Fact]
    public async Task Zero_Match_Tie_Stays_Silent_With_Proper_Error()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["xet"]);
        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("No command found for 'xet'", error);
        Assert.DoesNotContain("Did you mean", error);
        Assert.Contains("Run 'failclosed-test --help' for more information on available commands and options.", error);
    }

    [Fact]
    public async Task Zero_Match_Near_Miss_Of_Known_Command_Suggests()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["confg"]);
        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("No command found for 'confg'", error);
        Assert.Contains("Did you mean 'config'?", error);
    }

    [Fact]
    public void Tie_Matcher_Returns_Null_While_Unique_Near_Miss_Suggests()
    {
        var candidates = new List<(string Key, string Display)>
        {
            ("get", "get"),
            ("set", "set"),
        };

        Assert.Null(CommandLineParser.DidYouMean.SuggestSubcommand("xet", ["get", "set"]));
        Assert.Equal("get", CommandLineParser.DidYouMean.SuggestSubcommand("gett", ["get", "set"]));
        Assert.Null(CommandLineParser.DidYouMean.SuggestSubcommand("get", ["get", "set"]));
        Assert.Equal("--target", CommandLineParser.DidYouMean.SuggestBlamedToken("--targt", [("target", "--target")]));
    }

    [Fact]
    public void Prefix_Bonus_Accepts_Long_Rejects_Short_And_Far()
    {
        var longPair = new List<(string Key, string Display)> { ("ab", "--ab") };

        Assert.Equal("--ab", CommandLineParser.DidYouMean.SuggestBlamedToken("abcdef", longPair));
        Assert.Null(CommandLineParser.DidYouMean.SuggestBlamedToken("zzzzqqqq", longPair));
        Assert.Null(CommandLineParser.DidYouMean.SuggestBlamedToken("abcde", longPair));
    }

    [Fact]
    public async Task Exact_Surplus_Token_Never_Echoes_Itself()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["config", "--", "get"]);
        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Unexpected argument 'get'", error);
        Assert.DoesNotContain("Did you mean 'get'?", error);
        Assert.DoesNotContain("Unknown subcommand 'get'", error);
    }

    [Fact]
    public async Task Single_Dash_Cluster_Fragment_Reports_Char_Only()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["greet", "-verbose", "Alice"]);
        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Unknown option: -v", error);
        Assert.DoesNotContain("Unknown option: -verbose", error);
        Assert.DoesNotContain("Did you mean", error);
    }

    private static ApplicationBuilder CreateBuilder()
    {
        return ApplicationBuilder.Create()
            .SetExecutableName("failclosed-test")
            .SetExecutableTitle("Fail Closed Test")
            .SetExecutableDescription("Fail-closed verification CLI.")
            .SetExecutableVersion("9.9.9")
            .AddCommand<FailClosedDeployCommand>()
            .AddCommand<FailClosedConfigCommand>()
            .AddCommand<FailClosedConfigGetCommand>()
            .AddCommand<FailClosedConfigSetCommand>()
            .AddCommand<FailClosedGreetCommand>()
            .AddCommand<FailClosedHubGetCommand>()
            .AddCommand<FailClosedHubSetCommand>();
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
