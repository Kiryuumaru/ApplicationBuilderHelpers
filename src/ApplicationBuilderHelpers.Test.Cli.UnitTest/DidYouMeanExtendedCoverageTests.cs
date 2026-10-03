using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.Extensions;
using Microsoft.Extensions.Hosting;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// Extended fail-closed suggestion coverage: confident near-misses suggest,
/// ties, self-echo, short and distant guesses stay silent with exit 2 and
/// empty stdout, and every error keeps its proper message plus help footer.
/// Runs in the non-parallel <c>ConsoleDecoupling</c> collection.
/// </summary>
[Collection("ConsoleDecoupling")]
public sealed class DidYouMeanExtendedCoverageTests
{
    private static readonly SemaphoreSlim ConsoleGate = new(1, 1);

    [Command("deploy", "Probes option suggestions.")]
    public sealed class ExtendedDeployCommand : Command
    {
        [CommandOption("target", Description = "Deploy target.")]
        public string? Target { get; set; }

        [CommandOption("verbosity", Description = "Verbosity level.")]
        public string Verbosity { get; set; } = "normal";

        [CommandArgument("environment", Description = "Target environment.", Position = 0, Required = true)]
        public required string Environment { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"deploy:{Environment}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("config", "Probes surplus suggestions.")]
    public sealed class ExtendedConfigCommand : Command
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine("config");
            return ValueTask.CompletedTask;
        }
    }

    [Command("config get", "Gets a config value.")]
    public sealed class ExtendedConfigGetCommand : Command
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine("config get");
            return ValueTask.CompletedTask;
        }
    }

    [Command("config set", "Sets a config value.")]
    public sealed class ExtendedConfigSetCommand : Command
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine("config set");
            return ValueTask.CompletedTask;
        }
    }

    [Command("hub", "Abstract hub.")]
    public abstract class ExtendedHub : Command
    {
    }

    [Command("hub get", "Gets a hub value.")]
    public sealed class ExtendedHubGetCommand : ExtendedHub
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine("hub get");
            return ValueTask.CompletedTask;
        }
    }

    [Command("hub set", "Sets a hub value.")]
    public sealed class ExtendedHubSetCommand : ExtendedHub
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine("hub set");
            return ValueTask.CompletedTask;
        }
    }

    [Command("greet", "Greets.")]
    public sealed class ExtendedGreetCommand : Command
    {
        [CommandOption('a', "alpha", Description = "Alpha flag.")]
        public bool Alpha { get; set; }

        [CommandOption('b', "beta", Description = "Beta flag.")]
        public bool Beta { get; set; }

        [CommandOption('v', "verbose", Description = "Verbose flag.")]
        public bool Verbose { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine("greet");
            return ValueTask.CompletedTask;
        }
    }

    [Command("get", "Tie helper get.")]
    public sealed class TieGetCommand : Command
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine("get");
            return ValueTask.CompletedTask;
        }
    }

    [Command("set", "Tie helper set.")]
    public sealed class TieSetCommand : Command
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine("set");
            return ValueTask.CompletedTask;
        }
    }

    [Fact]
    public async Task Confident_Option_Typo_Suggests_Canonical_Name()
    {
        var (exitCode, output, error) = await RunMainAsync(["deploy", "--targt", "prod"]);
        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Unknown option: --targt", error);
        Assert.Contains("Did you mean '--target'?", error);
        Assert.Contains("Run 'extended-suggest-test deploy --help' for more information on specific command options.", error);
    }

    [Fact]
    public async Task Ambiguous_Hub_Token_Lists_Choices_Without_Hint()
    {
        var (exitCode, output, error) = await RunMainAsync(["hub", "het"]);
        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("'hub' requires a subcommand", error);
        Assert.Contains("Available subcommands: get, set", error);
        Assert.DoesNotContain("Did you mean", error);
        Assert.DoesNotContain("Unknown subcommand", error);
        Assert.Contains("Run 'extended-suggest-test hub --help' to see available subcommands and options.", error);
    }

    [Fact]
    public async Task Ambiguous_Top_Level_Token_Reports_No_Command_Without_Hint()
    {
        var (exitCode, output, error) = await RunTieAsync(["xet"]);
        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("No command found for 'xet'", error);
        Assert.DoesNotContain("Did you mean", error);
        Assert.Contains("Run 'extended-tie-test --help' for more information on available commands and options.", error);
    }

    [Fact]
    public async Task Close_Top_Level_Token_Suggests_Canonical_Command()
    {
        var (exitCode, output, error) = await RunMainAsync(["confg"]);
        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("No command found for 'confg'", error);
        Assert.Contains("Did you mean 'config'?", error);
        Assert.Contains("Run 'extended-suggest-test --help' for more information on available commands and options.", error);
    }

    [Fact]
    public async Task Close_Hub_Top_Level_Token_Suggests_Hub()
    {
        var (exitCode, output, error) = await RunMainAsync(["hubb"]);
        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("No command found for 'hubb'", error);
        Assert.Contains("Did you mean 'hub'?", error);
        Assert.Contains("Run 'extended-suggest-test --help' for more information on available commands and options.", error);
    }

    [Fact]
    public void Exact_Option_Display_Stays_Silent()
    {
        Assert.Null(CommandLineParser.DidYouMean.SuggestBlamedToken("--target", [("target", "--target")]));
        Assert.Null(CommandLineParser.DidYouMean.SuggestBlamedToken("--verbosity", [("verbosity", "--verbosity")]));
        Assert.Null(CommandLineParser.DidYouMean.FindBestMatch("--target", [("target", "--target")]));
    }

    [Fact]
    public void Exact_Subcommand_Name_Stays_Silent()
    {
        Assert.Null(CommandLineParser.DidYouMean.SuggestSubcommand("get", ["get", "set"]));
        Assert.Null(CommandLineParser.DidYouMean.SuggestSubcommand("config", ["config", "deploy"]));
        Assert.Null(CommandLineParser.DidYouMean.SuggestSubcommand("hub", ["hub", "greet"]));
    }

    [Fact]
    public void Case_Variant_Still_Suggests_Canonical()
    {
        Assert.Equal("get", CommandLineParser.DidYouMean.SuggestSubcommand("Get", ["get", "set"]));
        Assert.Equal("--verbosity", CommandLineParser.DidYouMean.SuggestBlamedToken("--VERBOSITY", [("verbosity", "--verbosity")]));
    }

    [Fact]
    public void Prefix_Bonus_Long_Token_Suggests()
    {
        Assert.Equal("--ab", CommandLineParser.DidYouMean.SuggestBlamedToken("abcdef", [("ab", "--ab")]));
        Assert.Equal("--ab", CommandLineParser.DidYouMean.SuggestBlamedToken("abwxyz", [("ab", "--ab")]));
        Assert.Equal("--verbosity", CommandLineParser.DidYouMean.SuggestBlamedToken("verbose", [("verbosity", "--verbosity")]));
    }

    [Fact]
    public void Prefix_Bonus_Short_Token_Stays_Silent()
    {
        Assert.Null(CommandLineParser.DidYouMean.SuggestBlamedToken("abcde", [("ab", "--ab")]));
        Assert.Null(CommandLineParser.DidYouMean.SuggestBlamedToken("abxyz", [("ab", "--ab")]));
    }

    [Fact]
    public void Distance_Two_Without_Prefix_Still_Suggests()
    {
        Assert.Equal("--ab", CommandLineParser.DidYouMean.SuggestBlamedToken("zz", [("ab", "--ab")]));
        Assert.Equal("set", CommandLineParser.DidYouMean.SuggestSubcommand("gett", ["set"]));
    }

    [Fact]
    public void Distance_Three_Without_Prefix_Stays_Silent()
    {
        Assert.Null(CommandLineParser.DidYouMean.SuggestBlamedToken("zzz", [("ab", "--ab")]));
        Assert.Null(CommandLineParser.DidYouMean.FindBestMatch("abc", [("def", "--def")]));
        Assert.Null(CommandLineParser.DidYouMean.SuggestSubcommand("zzzqqq", ["get", "set"]));
    }

    [Fact]
    public void Distance_Five_With_Prefix_Stays_Silent()
    {
        Assert.Null(CommandLineParser.DidYouMean.SuggestBlamedToken("abcdefg", [("ab", "--ab")]));
        Assert.Null(CommandLineParser.DidYouMean.SuggestBlamedToken("zzzzqqqq", [("ab", "--ab")]));
        Assert.Null(CommandLineParser.DidYouMean.SuggestSubcommand("zzzzqqqq", ["get", "set"]));
    }

    [Fact]
    public void Ambiguous_Pair_Stays_Silent_While_Unique_Suggests()
    {
        Assert.Null(CommandLineParser.DidYouMean.SuggestBlamedToken("xb", [("ab", "--ab"), ("cb", "--cb")]));
        Assert.Equal("--ab", CommandLineParser.DidYouMean.SuggestBlamedToken("xb", [("ab", "--ab")]));
        Assert.Null(CommandLineParser.DidYouMean.SuggestSubcommand("xet", ["get", "set"]));
        Assert.Equal("get", CommandLineParser.DidYouMean.SuggestSubcommand("gett", ["get", "set"]));
    }

    [Fact]
    public void Unrelated_Namespace_Typo_Stays_Silent()
    {
        Assert.Null(CommandLineParser.DidYouMean.SuggestBlamedToken("targt", [("alpha", "--alpha")]));
        Assert.Null(CommandLineParser.DidYouMean.SuggestSubcommand("gett", ["deploy", "config"]));
        Assert.Null(CommandLineParser.DidYouMean.SuggestSubcommand("confg", ["deploy", "greet"]));
    }

    [Fact]
    public async Task Leaf_Surplus_Far_Token_Reports_Unexpected_Without_Hint()
    {
        var (exitCode, output, error) = await RunMainAsync(["config", "zzzzqqqq"]);
        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Unexpected argument 'zzzzqqqq'", error);
        Assert.DoesNotContain("Did you mean", error);
        Assert.DoesNotContain("Unknown subcommand", error);
        Assert.Contains("Run 'extended-suggest-test config --help' for more information on specific command options.", error);
    }

    [Fact]
    public async Task Leaf_Surplus_Close_Token_Reports_Unknown_Subcommand_With_Hint()
    {
        var (exitCode, output, error) = await RunMainAsync(["config", "gett"]);
        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Unknown subcommand 'gett'", error);
        Assert.Contains("Did you mean 'get'?", error);
        Assert.DoesNotContain("requires a subcommand", error);
        Assert.DoesNotContain("Available subcommands", error);
        Assert.Contains("Run 'extended-suggest-test config --help' for more information on specific command options.", error);
    }

    [Fact]
    public async Task Cluster_Fragment_Reports_Char_Only_Without_Hint()
    {
        var (exitCode, output, error) = await RunMainAsync(["greet", "-zx"]);
        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Unknown option: -z", error);
        Assert.DoesNotContain("-zx", error);
        Assert.DoesNotContain("Did you mean", error);
        Assert.Contains("Run 'extended-suggest-test greet --help' for more information on specific command options.", error);
    }

    [Fact]
    public async Task Single_Dash_Long_Token_Reports_Full_Token_With_Hint()
    {
        var (exitCode, output, error) = await RunMainAsync(["greet", "-verbose"]);
        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Unknown option: -verbose", error);
        Assert.Contains("Did you mean '--verbose'?", error);
        Assert.DoesNotContain("Unknown option: -e", error);
        Assert.Contains("Run 'extended-suggest-test greet --help' for more information on specific command options.", error);
    }

    [Fact]
    public async Task Distant_Option_Typo_Stays_Silent_With_Specific_Footer()
    {
        var (exitCode, output, error) = await RunMainAsync(["deploy", "prod", "--zzzzqqqq"]);
        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Unknown option: --zzzzqqqq", error);
        Assert.DoesNotContain("Did you mean", error);
        Assert.Contains("Run 'extended-suggest-test deploy --help' for more information on specific command options.", error);
    }

    [Fact]
    public async Task Isolated_Option_Typo_Stays_Silent()
    {
        var (optionExit, optionOutput, optionError) = await RunMainAsync(["deploy", "prod", "--alpah"]);
        Assert.Equal(2, optionExit);
        Assert.True(string.IsNullOrWhiteSpace(optionOutput), $"Expected empty stdout but got: {optionOutput}");
        Assert.Contains("Unknown option: --alpah", optionError);
        Assert.DoesNotContain("Did you mean", optionError);
        Assert.Contains("Run 'extended-suggest-test deploy --help' for more information on specific command options.", optionError);
    }

    [Fact]
    public async Task Isolated_Surplus_Token_Stays_Silent()
    {
        var (subExit, subOutput, subError) = await RunMainAsync(["config", "deply"]);
        Assert.Equal(2, subExit);
        Assert.True(string.IsNullOrWhiteSpace(subOutput), $"Expected empty stdout but got: {subOutput}");
        Assert.Contains("Unexpected argument 'deply'", subError);
        Assert.DoesNotContain("Did you mean", subError);
        Assert.DoesNotContain("Unknown subcommand", subError);
        Assert.Contains("Run 'extended-suggest-test config --help' for more information on specific command options.", subError);
    }

    [Fact]
    public async Task Requires_Subcommand_And_Unknown_Subcommand_Footers_Differ()
    {
        var (bareExit, bareOutput, bareError) = await RunMainAsync(["hub", "zzzzqqqq"]);
        Assert.Equal(2, bareExit);
        Assert.True(string.IsNullOrWhiteSpace(bareOutput), $"Expected empty stdout but got: {bareOutput}");
        Assert.Contains("'hub' requires a subcommand", bareError);
        Assert.Contains("to see available subcommands and options.", bareError);

        var (nearExit, nearOutput, nearError) = await RunMainAsync(["hub", "gett"]);
        Assert.Equal(2, nearExit);
        Assert.True(string.IsNullOrWhiteSpace(nearOutput), $"Expected empty stdout but got: {nearOutput}");
        Assert.Contains("Unknown subcommand 'gett'", nearError);
        Assert.Contains("Did you mean 'get'?", nearError);
        Assert.Contains("for more information on specific command options.", nearError);
        Assert.DoesNotContain("to see available subcommands and options.", nearError);
    }

    private static ApplicationBuilder CreateMainBuilder()
    {
        return ApplicationBuilder.Create()
            .SetExecutableName("extended-suggest-test")
            .SetExecutableTitle("Extended Suggest Test")
            .SetExecutableDescription("Extended suggestion verification CLI.")
            .SetExecutableVersion("9.9.9")
            .AddCommand<ExtendedDeployCommand>()
            .AddCommand<ExtendedConfigCommand>()
            .AddCommand<ExtendedConfigGetCommand>()
            .AddCommand<ExtendedConfigSetCommand>()
            .AddCommand<ExtendedGreetCommand>()
            .AddCommand<ExtendedHubGetCommand>()
            .AddCommand<ExtendedHubSetCommand>();
    }

    private static ApplicationBuilder CreateTieBuilder()
    {
        return ApplicationBuilder.Create()
            .SetExecutableName("extended-tie-test")
            .SetExecutableTitle("Extended Tie Test")
            .SetExecutableDescription("Extended tie verification CLI.")
            .SetExecutableVersion("9.9.9")
            .AddCommand<TieGetCommand>()
            .AddCommand<TieSetCommand>();
    }

    private static Task<(int ExitCode, string Output, string Error)> RunMainAsync(string[] args) =>
        RunCapturedAsync(CreateMainBuilder(), args);

    private static Task<(int ExitCode, string Output, string Error)> RunTieAsync(string[] args) =>
        RunCapturedAsync(CreateTieBuilder(), args);

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
