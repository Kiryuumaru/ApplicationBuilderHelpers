using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.Extensions;
using Microsoft.Extensions.Hosting;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// Guards the help-token fallback boundary: a bare <c>/?</c> is never consumed
/// as the fallback value of a dangling optional valued option, so it behaves
/// exactly like <c>--help</c> — help wins beside a path-walk option, an
/// optional option fails closed with a missing-value error (also inside a
/// combined short cluster, after a positional, and before a separator), and a
/// grouping node shows help instead of requiring a subcommand, even when a
/// version token follows. A required option still consumes <c>/?</c> as its
/// value, the <c>=</c>-form still binds it, and lookalike values
/// (<c>/?x</c>, <c>/??</c>, <c>/</c>, <c>?</c>, <c>/tmp/x</c>) still bind as
/// either a required or an optional value. A consumed optional value followed
/// by a typo keeps a live help request on the subcommand guard in either order,
/// with or without an accompanying version token, while a version-only tail
/// still shows the version. A required valued option taking a cluster-looking
/// value likewise counts the full pre-<c>--</c> help window, so a trailing
/// version token stays a typed subcommand error while the version-only and help
/// tails keep their outcomes. Runs in the non-parallel
/// <c>ConsoleDecoupling</c> collection.
/// </summary>
[Collection("ConsoleDecoupling")]
public sealed class HelpTokenFallbackConsumptionTests
{
    private static readonly SemaphoreSlim ConsoleGate = new(1, 1);

    [Command("run", "Runs with a required name and an optional fast flag.")]
    public sealed class RunFallbackProbeCommand : Command
    {
        [CommandOption('n', "name", Description = "Required name.", Required = true)]
        public string Name { get; set; } = "";

        [CommandOption('c', "config", Description = "Optional config path.")]
        public string? Config { get; set; }

        [CommandOption('f', "fast", Description = "Fast mode flag.")]
        public bool Fast { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"RUN name=[{Name}] config=[{Config ?? "<null>"}] fast={Fast}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("opt", "Runs with a single optional valued option.")]
    public sealed class OptionalFallbackProbeCommand : Command
    {
        [CommandOption('c', "config", Description = "Optional config path.")]
        public string? Config { get; set; }

        [CommandOption('v', "verbose", Description = "Verbose flag.")]
        public bool Verbose { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"OPT config=[{Config ?? "<null>"}] verbose={Verbose}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("pos", "Runs with a positional word and an optional valued option.")]
    public sealed class PositionalFallbackProbeCommand : Command
    {
        [CommandArgument("who", Description = "Positional word.", Position = 0, Required = false)]
        public string? Who { get; set; }

        [CommandOption('t', "tag", Description = "Optional tag.")]
        public string? Tag { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"POS who=[{Who ?? "<null>"}] tag=[{Tag ?? "<null>"}]");
            return ValueTask.CompletedTask;
        }
    }

    [Command("grp", "Grouping node.")]
    public abstract class GroupingFallbackProbeCommand : Command
    {
        [CommandOption('f', "fmt", Description = "Optional format.")]
        public string? Fmt { get; set; }

        [CommandOption('n', "name", Description = "Optional name.")]
        public string? Name { get; set; }
    }

    [Command("grp child", "Child of the grouping node.")]
    public sealed class GroupingChildFallbackProbeCommand : GroupingFallbackProbeCommand
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"GRP-CHILD name=[{Name ?? "<null>"}] fmt=[{Fmt ?? "<null>"}]");
            return ValueTask.CompletedTask;
        }
    }

    [Command("grp2", "Grouping node with a required option.")]
    public abstract class RequiredGroupingFallbackProbeCommand : Command
    {
        [CommandOption('n', "name", Description = "Required name.", Required = true)]
        public string Name { get; set; } = "";
    }

    [Command("grp2 child", "Child of the required-option grouping node.")]
    public sealed class RequiredGroupingChildFallbackProbeCommand : RequiredGroupingFallbackProbeCommand
    {
        [CommandArgument("target", Description = "Target item.", Position = 0, Required = false)]
        public string? Target { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"GRP2-CHILD name=[{Name}]");
            Console.WriteLine($"Ran target={Target} name={Name}");
            return ValueTask.CompletedTask;
        }
    }

    [Fact]
    public async Task PathWalk_RequiredValuedOption_HelpToken_ForgivesLaterInvalidValue()
    {
        var slash = await RunCapturedAsync(["run", "--name", "/?", "--fast=banana"]);
        var dash = await RunCapturedAsync(["run", "--name", "--help", "--fast=banana"]);

        Assert.Equal(0, slash.ExitCode);
        Assert.Contains("USAGE:", slash.Output);
        Assert.Contains("Runs with a required name", slash.Output);
        Assert.DoesNotContain("Invalid Boolean value", slash.Error);
        Assert.True(string.IsNullOrWhiteSpace(slash.Error), $"Expected empty stderr but got: {slash.Error}");

        Assert.Equal(dash.ExitCode, slash.ExitCode);
        Assert.Equal(dash.Output, slash.Output);
        Assert.Equal(dash.Error, slash.Error);
    }

    [Fact]
    public async Task PathWalk_OptionalValuedOption_HelpToken_ReportsMissingValue()
    {
        var slash = await RunCapturedAsync(["opt", "--config", "/?"]);
        var dash = await RunCapturedAsync(["opt", "--config", "--help"]);

        Assert.Equal(2, slash.ExitCode);
        Assert.True(string.IsNullOrWhiteSpace(slash.Output), $"Expected empty stdout but got: {slash.Output}");
        Assert.Contains("Missing value for option: -c, --config", slash.Error);
        Assert.DoesNotContain("OPT config=[/?]", slash.Output);

        Assert.Equal(dash.ExitCode, slash.ExitCode);
        Assert.Equal(dash.Output, slash.Output);
        Assert.Equal(dash.Error, slash.Error);
    }

    [Fact]
    public async Task CombinedShortCluster_ValuedShort_HelpToken_ReportsMissingValue()
    {
        var slash = await RunCapturedAsync(["opt", "-vc", "/?"]);
        var dash = await RunCapturedAsync(["opt", "-vc", "--help"]);

        Assert.Equal(2, slash.ExitCode);
        Assert.True(string.IsNullOrWhiteSpace(slash.Output), $"Expected empty stdout but got: {slash.Output}");
        Assert.Contains("Missing value for option: -c, --config", slash.Error);
        Assert.DoesNotContain("verbose=True", slash.Output);

        Assert.Equal(dash.ExitCode, slash.ExitCode);
        Assert.Equal(dash.Output, slash.Output);
        Assert.Equal(dash.Error, slash.Error);
    }

    [Fact]
    public async Task OptionsAndArguments_OptionalValuedOption_HelpToken_ReportsMissingValue()
    {
        var slash = await RunCapturedAsync(["pos", "who", "--tag", "/?"]);
        var dash = await RunCapturedAsync(["pos", "who", "--tag", "--help"]);

        Assert.Equal(2, slash.ExitCode);
        Assert.True(string.IsNullOrWhiteSpace(slash.Output), $"Expected empty stdout but got: {slash.Output}");
        Assert.Contains("Missing value for option: -t, --tag", slash.Error);

        Assert.Equal(dash.ExitCode, slash.ExitCode);
        Assert.Equal(dash.Output, slash.Output);
        Assert.Equal(dash.Error, slash.Error);
    }

    [Fact]
    public async Task GroupingNode_OptionalValuedOption_HelpToken_ShowsHelp()
    {
        var slash = await RunCapturedAsync(["grp", "--fmt", "/?"]);
        var dash = await RunCapturedAsync(["grp", "--fmt", "--help"]);

        Assert.Equal(0, slash.ExitCode);
        Assert.Contains("USAGE:", slash.Output);
        Assert.Contains("Grouping node", slash.Output);
        Assert.DoesNotContain("requires a subcommand", slash.Error);
        Assert.True(string.IsNullOrWhiteSpace(slash.Error), $"Expected empty stderr but got: {slash.Error}");

        Assert.Equal(dash.ExitCode, slash.ExitCode);
        Assert.Equal(dash.Output, slash.Output);
        Assert.Equal(dash.Error, slash.Error);
    }

    [Fact]
    public async Task SubcommandOnlyNode_RequiredValuedOption_HelpToken_ShowsHelp()
    {
        var slash = await RunCapturedAsync(["grp2", "--name", "/?", "child"]);
        var dash = await RunCapturedAsync(["grp2", "--name", "--help", "child"]);

        Assert.Equal(0, slash.ExitCode);
        Assert.Contains("USAGE:", slash.Output);
        Assert.Contains("Grouping node with a required option", slash.Output);
        Assert.DoesNotContain("GRP2-CHILD", slash.Output);
        Assert.True(string.IsNullOrWhiteSpace(slash.Error), $"Expected empty stderr but got: {slash.Error}");

        Assert.Equal(dash.ExitCode, slash.ExitCode);
        Assert.Equal(dash.Output, slash.Output);
        Assert.Equal(dash.Error, slash.Error);
    }

    [Fact]
    public async Task GroupingLeaf_RequiredValuedOption_ConsumesHelpToken_Executes()
    {
        var result = await RunCapturedAsync(["grp2", "child", "target", "--name", "--help"]);

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Ran target=target name=--help", result.Output);
        Assert.DoesNotContain("USAGE:", result.Output);
        Assert.True(string.IsNullOrWhiteSpace(result.Error), $"Expected empty stderr but got: {result.Error}");
    }

    [Fact]
    public async Task RequiredGroupingOption_HelpVersionClusterValue_RequiresSubcommand()
    {
        var result = await RunCapturedAsync(["grp2", "--name", "-hV"]);

        Assert.Equal(2, result.ExitCode);
        Assert.True(string.IsNullOrWhiteSpace(result.Output), $"Expected empty stdout but got: {result.Output}");
        Assert.Contains("'grp2' requires a subcommand", result.Error);
        Assert.Contains("Available subcommands: child", result.Error);
        AssertNoRuntimeFault(result.Error);
    }

    [Fact]
    public async Task RequiredGroupingOption_HelpVersionClusterValue_WithVersion_RequiresSubcommand()
    {
        var control = await RunCapturedAsync(["grp2", "--name", "-hV"]);
        var result = await RunCapturedAsync(["grp2", "--name", "-hV", "--version"]);

        Assert.Equal(2, result.ExitCode);
        AssertSameResult(control, result);
        Assert.True(string.IsNullOrWhiteSpace(result.Output), $"Expected empty stdout but got: {result.Output}");
        Assert.Contains("'grp2' requires a subcommand", result.Error);
        Assert.Contains("Available subcommands: child", result.Error);
        AssertNoRuntimeFault(result.Error);
    }

    [Fact]
    public async Task RequiredGroupingOption_AttachedShortValue_WithVersion_RequiresSubcommand()
    {
        var control = await RunCapturedAsync(["grp2", "-nhello"]);
        var result = await RunCapturedAsync(["grp2", "-nhello", "--version"]);

        Assert.Equal(2, control.ExitCode);
        Assert.Equal(2, result.ExitCode);
        AssertSameResult(control, result);
        Assert.True(string.IsNullOrWhiteSpace(result.Output), $"Expected empty stdout but got: {result.Output}");
        AssertNoRuntimeFault(result.Error);
    }

    [Fact]
    public async Task RequiredGroupingOption_HelpClusterValue_WithVersion_RequiresSubcommand()
    {
        var control = await RunCapturedAsync(["grp2", "--name", "-hx"]);
        var result = await RunCapturedAsync(["grp2", "--name", "-hx", "--version"]);

        Assert.Equal(2, control.ExitCode);
        Assert.Equal(2, result.ExitCode);
        AssertSameResult(control, result);
        Assert.True(string.IsNullOrWhiteSpace(result.Output), $"Expected empty stdout but got: {result.Output}");
        AssertNoRuntimeFault(result.Error);
    }

    [Fact]
    public async Task RequiredGroupingOption_HelpClusterLookalikeValue_WithVersion_RequiresSubcommand()
    {
        var control = await RunCapturedAsync(["grp2", "--name", "-vhello"]);
        var result = await RunCapturedAsync(["grp2", "--name", "-vhello", "--version"]);

        Assert.Equal(2, control.ExitCode);
        Assert.Equal(2, result.ExitCode);
        AssertSameResult(control, result);
        Assert.True(string.IsNullOrWhiteSpace(result.Output), $"Expected empty stdout but got: {result.Output}");
        AssertNoRuntimeFault(result.Error);
    }

    [Fact]
    public async Task RequiredGroupingOption_HelpVersionClusterValue_BeforeSeparator_WithVersion_RequiresSubcommand()
    {
        var control = await RunCapturedAsync(["grp2", "--name", "-hV", "--", "--help"]);
        var result = await RunCapturedAsync(["grp2", "--name", "-hV", "--version", "--", "--help"]);

        Assert.Equal(2, control.ExitCode);
        Assert.Equal(2, result.ExitCode);
        AssertSameResult(control, result);
        Assert.True(string.IsNullOrWhiteSpace(result.Output), $"Expected empty stdout but got: {result.Output}");
        AssertNoRuntimeFault(result.Error);
    }

    [Fact]
    public async Task RequiredGroupingOption_AttachedShortValue_WithVersion_ShowsVersion()
    {
        var expected = await RunCapturedAsync(["grp2", "--version"]);
        var result = await RunCapturedAsync(["grp2", "--name", "-nValue", "--version"]);

        Assert.Equal(0, result.ExitCode);
        AssertSameResult(expected, result);
        Assert.Contains("9.9.9", result.Output);
        Assert.DoesNotContain("USAGE:", result.Output);
        Assert.True(string.IsNullOrWhiteSpace(result.Error), $"Expected empty stderr but got: {result.Error}");
    }

    [Theory]
    [InlineData("--version")]
    [InlineData("-V")]
    public async Task GroupingLeaf_VersionBeforePath_ConsumedHelp_ShowsVersion(string versionToken)
    {
        var control = await RunCapturedAsync(["grp2", "--version"]);
        var result = await RunCapturedAsync(["grp2", versionToken, "child", "target", "--name", "--help"]);

        Assert.Equal(0, control.ExitCode);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal(control.Output, result.Output);
        Assert.Equal(control.Error, result.Error);
        Assert.Contains("9.9.9", result.Output);
        Assert.DoesNotContain("Ran target=", result.Output);
    }

    [Fact]
    public async Task RequiredGroupingOption_EqualsFormHelpClusterValue_WithVersion_ShowsVersion()
    {
        var expected = await RunCapturedAsync(["grp2", "--version"]);
        var result = await RunCapturedAsync(["grp2", "--name=-hV", "--version"]);

        Assert.Equal(0, result.ExitCode);
        AssertSameResult(expected, result);
        Assert.Contains("9.9.9", result.Output);
        Assert.DoesNotContain("USAGE:", result.Output);
        Assert.True(string.IsNullOrWhiteSpace(result.Error), $"Expected empty stderr but got: {result.Error}");
    }

    [Fact]
    public async Task RequiredGroupingOption_VersionClusterValue_WithVersion_ShowsVersion()
    {
        var expected = await RunCapturedAsync(["grp2", "--version"]);
        var result = await RunCapturedAsync(["grp2", "--name", "-V", "--version"]);

        Assert.Equal(0, result.ExitCode);
        AssertSameResult(expected, result);
        Assert.Contains("9.9.9", result.Output);
        Assert.DoesNotContain("USAGE:", result.Output);
        Assert.True(string.IsNullOrWhiteSpace(result.Error), $"Expected empty stderr but got: {result.Error}");
    }

    [Fact]
    public async Task RequiredGroupingOption_BareHelpToken_WithVersion_ShowsHelp()
    {
        var result = await RunCapturedAsync(["grp2", "--name", "--help", "--version"]);

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("USAGE:", result.Output);
        Assert.Contains("Grouping node with a required option", result.Output);
        Assert.DoesNotContain("GRP2-CHILD", result.Output);
        Assert.DoesNotContain("requires a subcommand", result.Error);
        Assert.True(string.IsNullOrWhiteSpace(result.Error), $"Expected empty stderr but got: {result.Error}");
    }

    [Fact]
    public async Task RequiredGroupingOption_HelpVersionClusterValue_WithVersionAndHelp_ShowsHelp()
    {
        var result = await RunCapturedAsync(["grp2", "--name", "-hV", "--version", "--help"]);

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("USAGE:", result.Output);
        Assert.Contains("Grouping node with a required option", result.Output);
        Assert.DoesNotContain("GRP2-CHILD", result.Output);
        Assert.DoesNotContain("requires a subcommand", result.Error);
        Assert.True(string.IsNullOrWhiteSpace(result.Error), $"Expected empty stderr but got: {result.Error}");
    }

    [Fact]
    public async Task RequiredValuedOption_BareHelpToken_StillBindsAsValue()
    {
        var result = await RunCapturedAsync(["run", "--name", "/?"]);

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("RUN name=[/?] config=[<null>] fast=False", result.Output);
        Assert.DoesNotContain("USAGE:", result.Output);
        Assert.True(string.IsNullOrWhiteSpace(result.Error), $"Expected empty stderr but got: {result.Error}");
    }

    [Fact]
    public async Task RequiredValuedOption_EqualsForm_StillBindsHelpToken()
    {
        var result = await RunCapturedAsync(["run", "--name=/?", "--fast"]);

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("RUN name=[/?] config=[<null>] fast=True", result.Output);
        Assert.DoesNotContain("USAGE:", result.Output);
        Assert.True(string.IsNullOrWhiteSpace(result.Error), $"Expected empty stderr but got: {result.Error}");
    }

    [Fact]
    public async Task RequiredValuedOption_EqualsForm_DoesNotRequestHelp()
    {
        var result = await RunCapturedAsync(["run", "--name=/?", "--fast=banana"]);

        Assert.Equal(2, result.ExitCode);
        Assert.Contains("Invalid Boolean value 'banana' for option '--fast'", result.Error);
        Assert.DoesNotContain("USAGE:", result.Output);
    }

    [Fact]
    public async Task OptionalValuedOption_EqualsForm_StillBindsHelpToken()
    {
        var result = await RunCapturedAsync(["opt", "--config=/?", "--verbose"]);

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("OPT config=[/?] verbose=True", result.Output);
        Assert.DoesNotContain("USAGE:", result.Output);
        Assert.True(string.IsNullOrWhiteSpace(result.Error), $"Expected empty stderr but got: {result.Error}");
    }

    [Fact]
    public async Task BareHelpToken_ShowsGlobalHelp()
    {
        var result = await RunCapturedAsync(["/?"]);

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("USAGE:", result.Output);
        Assert.Contains("COMMANDS:", result.Output);
        Assert.True(string.IsNullOrWhiteSpace(result.Error), $"Expected empty stderr but got: {result.Error}");
    }

    [Theory]
    [InlineData("/?x")]
    [InlineData("/??")]
    [InlineData("/")]
    [InlineData("?")]
    [InlineData("/tmp/x")]
    [InlineData("plain")]
    public async Task LookalikeValues_StillBindAsRequiredValue(string value)
    {
        var result = await RunCapturedAsync(["run", "--name", value]);

        Assert.Equal(0, result.ExitCode);
        Assert.Contains($"RUN name=[{value}] config=[<null>] fast=False", result.Output);
        Assert.DoesNotContain("USAGE:", result.Output);
        Assert.True(string.IsNullOrWhiteSpace(result.Error), $"Expected empty stderr but got: {result.Error}");
    }

    [Theory]
    [InlineData("/?x")]
    [InlineData("/??")]
    [InlineData("/")]
    [InlineData("?")]
    [InlineData("/tmp/x")]
    public async Task LookalikeValues_StillBindAsOptionalValue(string value)
    {
        var result = await RunCapturedAsync(["opt", "--config", value]);

        Assert.Equal(0, result.ExitCode);
        Assert.Contains($"OPT config=[{value}] verbose=False", result.Output);
        Assert.DoesNotContain("USAGE:", result.Output);
        Assert.DoesNotContain("Missing value", result.Error);
        Assert.True(string.IsNullOrWhiteSpace(result.Error), $"Expected empty stderr but got: {result.Error}");
    }

    [Theory]
    [InlineData("--version")]
    [InlineData("-V")]
    public async Task GroupingNode_OptionalValuedOption_HelpToken_BeforeVersionToken_ShowsHelp(string versionToken)
    {
        var slash = await RunCapturedAsync(["grp", "--fmt", "/?", versionToken]);
        var dash = await RunCapturedAsync(["grp", "--fmt", "--help", versionToken]);

        Assert.Equal(0, slash.ExitCode);
        Assert.Contains("USAGE:", slash.Output);
        Assert.Contains("Grouping node", slash.Output);
        Assert.DoesNotContain("Non-static method requires a target", slash.Error);
        Assert.DoesNotContain("requires a subcommand", slash.Error);
        Assert.True(string.IsNullOrWhiteSpace(slash.Error), $"Expected empty stderr but got: {slash.Error}");

        Assert.Equal(dash.ExitCode, slash.ExitCode);
        Assert.Equal(dash.Output, slash.Output);
        Assert.Equal(dash.Error, slash.Error);
    }

    [Fact]
    public async Task GroupingNode_OptionalValuedOption_BeforeTypo_HelpAndVersion_RequiresSubcommand()
    {
        var expected = await RunCapturedAsync(["grp", "--fmt", "json", "bogus", "--help"]);
        var result = await RunCapturedAsync(["grp", "--fmt", "json", "bogus", "--help", "--version"]);

        Assert.Equal(2, expected.ExitCode);
        Assert.Equal(expected.ExitCode, result.ExitCode);
        Assert.Equal(expected.Output, result.Output);
        Assert.Equal(expected.Error, result.Error);
        Assert.True(string.IsNullOrWhiteSpace(result.Output), $"Expected empty stdout but got: {result.Output}");
        Assert.Contains("requires a subcommand", result.Error);
        Assert.Contains("Available subcommands: child", result.Error);
        Assert.DoesNotContain("Object reference not set", result.Error);
        Assert.DoesNotContain("Non-static method requires a target", result.Error);
    }

    [Fact]
    public async Task GroupingNode_OptionalValuedOption_BeforeHelpAndTypo_Version_RequiresSubcommand()
    {
        var expected = await RunCapturedAsync(["grp", "--fmt", "json", "--help", "bogus"]);
        var result = await RunCapturedAsync(["grp", "--fmt", "json", "--help", "bogus", "--version"]);

        Assert.Equal(2, expected.ExitCode);
        Assert.Equal(expected.ExitCode, result.ExitCode);
        Assert.Equal(expected.Output, result.Output);
        Assert.Equal(expected.Error, result.Error);
        Assert.True(string.IsNullOrWhiteSpace(result.Output), $"Expected empty stdout but got: {result.Output}");
        Assert.Contains("requires a subcommand", result.Error);
        Assert.Contains("Available subcommands: child", result.Error);
        Assert.DoesNotContain("Object reference not set", result.Error);
        Assert.DoesNotContain("Non-static method requires a target", result.Error);
    }

    [Fact]
    public async Task GroupingNode_OptionalValuedOption_BeforeTypo_WithVersion_ShowsVersion()
    {
        var expected = await RunCapturedAsync(["grp", "--version"]);
        var result = await RunCapturedAsync(["grp", "--fmt", "json", "bogus", "--version"]);

        Assert.Equal(0, expected.ExitCode);
        Assert.Equal(expected.ExitCode, result.ExitCode);
        Assert.Equal(expected.Output, result.Output);
        Assert.Equal(expected.Error, result.Error);
        Assert.Contains("9.9.9", result.Output);
        Assert.DoesNotContain("USAGE:", result.Output);
        Assert.DoesNotContain("requires a subcommand", result.Error);
        Assert.DoesNotContain("Object reference not set", result.Error);
        Assert.DoesNotContain("Non-static method requires a target", result.Error);
        Assert.True(string.IsNullOrWhiteSpace(result.Error), $"Expected empty stderr but got: {result.Error}");
    }

    [Fact]
    public async Task OptionalValuedOption_HelpTokenBeforeSeparator_ReportsMissingValue()
    {
        var slash = await RunCapturedAsync(["opt", "--config", "/?", "--"]);
        var dash = await RunCapturedAsync(["opt", "--config", "--help", "--"]);

        Assert.Equal(2, slash.ExitCode);
        Assert.True(string.IsNullOrWhiteSpace(slash.Output), $"Expected empty stdout but got: {slash.Output}");
        Assert.Contains("Missing value for option: -c, --config", slash.Error);
        Assert.DoesNotContain("OPT config=[/?]", slash.Output);

        Assert.Equal(dash.ExitCode, slash.ExitCode);
        Assert.Equal(dash.Output, slash.Output);
        Assert.Equal(dash.Error, slash.Error);
    }

    private static void AssertSameResult(
        (int ExitCode, string Output, string Error) expected,
        (int ExitCode, string Output, string Error) result)
    {
        Assert.Equal(expected.ExitCode, result.ExitCode);
        Assert.Equal(expected.Output, result.Output);
        Assert.Equal(expected.Error, result.Error);
    }

    private static void AssertNoRuntimeFault(string error)
    {
        Assert.DoesNotContain("Non-static method requires a target", error);
        Assert.DoesNotContain("TargetException", error);
        Assert.DoesNotContain("Object reference not set", error);
        Assert.DoesNotContain(
            "Run 'help-token-fallback-test --help' for more information on available commands and options.",
            error);
    }

    private static ApplicationBuilder CreateBuilder()
    {
        return ApplicationBuilder.Create()
            .SetExecutableName("help-token-fallback-test")
            .SetExecutableTitle("Help Token Fallback Test")
            .SetExecutableDescription("Help-token fallback verification CLI.")
            .SetExecutableVersion("9.9.9")
            .AddCommand<RunFallbackProbeCommand>()
            .AddCommand<OptionalFallbackProbeCommand>()
            .AddCommand<PositionalFallbackProbeCommand>()
            .AddCommand<GroupingChildFallbackProbeCommand>()
            .AddCommand<RequiredGroupingChildFallbackProbeCommand>();
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
