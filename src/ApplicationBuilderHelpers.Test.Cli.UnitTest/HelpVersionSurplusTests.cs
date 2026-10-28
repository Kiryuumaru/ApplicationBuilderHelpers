using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.Extensions;
using ApplicationBuilderHelpers.Test.Cli.UnitTest.TestFramework;
using Microsoft.Extensions.Hosting;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// Tests that a surplus bare word after a resolved help path stays an error
/// when a version flag sits beside help in either order. Help chooses the path
/// and the miss gates run before version precedence, so each error matches the
/// help-only control in both text and kind. Covers long and short help
/// aliases, near misses, hit-plus-extra, typo-before-help parity,
/// version-before-path leaf positionals, and the version-only and separator
/// controls.
/// </summary>
public class HelpVersionSurplusTests : CliTestBase
{
    private const string GroupRequiresSubcommand =
        "'config' requires a subcommand. Available subcommands: get, set";

    private const string GroupNearMiss =
        "Unknown subcommand 'sett'. Did you mean 'set'?";

    private const string GroupHitPlusExtra =
        "Unexpected argument 'extra'";

    [Fact]
    public async Task Group_Help_BeforeSurplus_RequiresSubcommand()
    {
        var result = await Runner.RunAsync("config", "--help", "bogus");

        Assert.Equal(2, result.ExitCode);
        Assert.True(string.IsNullOrWhiteSpace(result.StandardOutput),
            $"Expected empty stdout but got: {result.StandardOutput}");
        Assert.Contains(GroupRequiresSubcommand, result.StandardError);
        Assert.Contains("Run 'test --version' to show version information.", result.StandardError);
    }

    [Fact]
    public async Task Group_Help_Version_BeforeSurplus_MatchesHelpOnly()
    {
        var control = await Runner.RunAsync("config", "--help", "bogus");
        var result = await Runner.RunAsync("config", "--help", "--version", "bogus");

        AssertMatchedFailure(control, result, GroupRequiresSubcommand);
    }

    [Fact]
    public async Task Group_Version_Help_BeforeSurplus_MatchesHelpOnly()
    {
        var control = await Runner.RunAsync("config", "--help", "bogus");
        var result = await Runner.RunAsync("config", "--version", "--help", "bogus");

        AssertMatchedFailure(control, result, GroupRequiresSubcommand);
    }

    [Fact]
    public async Task Group_Help_Version_ShowsGroupHelp()
    {
        var control = await Runner.RunAsync("config", "--help");
        var result = await Runner.RunAsync("config", "--help", "--version");

        Assert.Equal(0, control.ExitCode);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal(control.StandardOutput, result.StandardOutput);
        AssertGroupHelp(result);
    }

    [Fact]
    public async Task Group_Version_Help_ShowsGroupHelp()
    {
        var control = await Runner.RunAsync("config", "--help");
        var result = await Runner.RunAsync("config", "--version", "--help");

        Assert.Equal(0, control.ExitCode);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal(control.StandardOutput, result.StandardOutput);
        AssertGroupHelp(result);
    }

    [Fact]
    public async Task Group_Help_BeforeSubcommand_ShowsLeafHelp()
    {
        var result = await Runner.RunAsync("config", "--help", "get");

        Assert.Equal(0, result.ExitCode);
        AssertLeafHelp(result);
    }

    [Fact]
    public async Task Group_Help_Version_BeforeSubcommand_ShowsLeafHelp()
    {
        var control = await Runner.RunAsync("config", "--help", "get");
        var result = await Runner.RunAsync("config", "--help", "--version", "get");

        Assert.Equal(0, control.ExitCode);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal(control.StandardOutput, result.StandardOutput);
        AssertLeafHelp(result);
    }

    [Fact]
    public async Task Group_Version_Help_BeforeSubcommand_ShowsLeafHelp()
    {
        var control = await Runner.RunAsync("config", "--help", "get");
        var result = await Runner.RunAsync("config", "--version", "--help", "get");

        Assert.Equal(0, control.ExitCode);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal(control.StandardOutput, result.StandardOutput);
        AssertLeafHelp(result);
    }

    [Fact]
    public async Task Group_Help_BeforeNearMiss_ReportsUnknownSubcommand()
    {
        var result = await Runner.RunAsync("config", "--help", "sett");

        Assert.Equal(2, result.ExitCode);
        Assert.True(string.IsNullOrWhiteSpace(result.StandardOutput),
            $"Expected empty stdout but got: {result.StandardOutput}");
        Assert.Contains(GroupNearMiss, result.StandardError);
        Assert.Contains("Run 'test --version' to show version information.", result.StandardError);
    }

    [Fact]
    public async Task Group_Help_Version_BeforeNearMiss_MatchesHelpOnly()
    {
        var control = await Runner.RunAsync("config", "--help", "sett");
        var result = await Runner.RunAsync("config", "--help", "--version", "sett");

        AssertMatchedFailure(control, result, GroupNearMiss);
    }

    [Fact]
    public async Task Group_Version_Help_BeforeNearMiss_MatchesHelpOnly()
    {
        var control = await Runner.RunAsync("config", "--help", "sett");
        var result = await Runner.RunAsync("config", "--version", "--help", "sett");

        AssertMatchedFailure(control, result, GroupNearMiss);
    }

    [Fact]
    public async Task Group_TypoBeforeHelp_RequiresSubcommandWithPointer()
    {
        var result = await Runner.RunAsync("config", "sett", "--help");

        Assert.Equal(2, result.ExitCode);
        Assert.True(string.IsNullOrWhiteSpace(result.StandardOutput),
            $"Expected empty stdout but got: {result.StandardOutput}");
        Assert.Contains(GroupRequiresSubcommand, result.StandardError);
        Assert.Contains("Did you mean 'set'?", result.StandardError);
        Assert.Contains("Run 'test --version' to show version information.", result.StandardError);
    }

    [Theory]
    [InlineData("--version")]
    [InlineData("-V")]
    public async Task Group_TypoBeforeHelp_WithVersionPrefix_MatchesHelpOnly(string versionToken)
    {
        var control = await Runner.RunAsync("config", "sett", "--help");
        var result = await Runner.RunAsync("config", versionToken, "sett", "--help");

        AssertMatchedFailure(control, result, "Did you mean 'set'?");
        Assert.Contains(GroupRequiresSubcommand, control.StandardError);
    }

    [Fact]
    public async Task Group_Help_BeforeHitPlusExtra_ReportsUnexpectedArgument()
    {
        var result = await Runner.RunAsync("config", "--help", "get", "extra");

        Assert.Equal(2, result.ExitCode);
        Assert.True(string.IsNullOrWhiteSpace(result.StandardOutput),
            $"Expected empty stdout but got: {result.StandardOutput}");
        Assert.Contains(GroupHitPlusExtra, result.StandardError);
        Assert.Contains("Run 'test --version' to show version information.", result.StandardError);
    }

    [Fact]
    public async Task Group_Help_Version_BeforeHitPlusExtra_MatchesHelpOnly()
    {
        var control = await Runner.RunAsync("config", "--help", "get", "extra");
        var result = await Runner.RunAsync("config", "--help", "--version", "get", "extra");

        AssertMatchedFailure(control, result, GroupHitPlusExtra);
    }

    [Fact]
    public async Task Group_Version_Help_BeforeHitPlusExtra_MatchesHelpOnly()
    {
        var control = await Runner.RunAsync("config", "--help", "get", "extra");
        var result = await Runner.RunAsync("config", "--version", "--help", "get", "extra");

        AssertMatchedFailure(control, result, GroupHitPlusExtra);
    }

    [Theory]
    [InlineData("--version")]
    [InlineData("-V")]
    public async Task Group_Help_Get_Extra_WithVersionAfterLeaf_MatchesHelpOnly(string versionToken)
    {
        var control = await Runner.RunAsync("config", "--help", "get", "extra");
        var result = await Runner.RunAsync("config", "--help", "get", versionToken, "extra");

        AssertMatchedFailure(control, result, GroupHitPlusExtra);
        Assert.Equal(control.StandardOutput, result.StandardOutput);
    }

    [Theory]
    [InlineData("--version")]
    [InlineData("-V")]
    public async Task Group_Help_Get_WithVersionAfterLeaf_ShowsLeafHelp(string versionToken)
    {
        var control = await Runner.RunAsync("config", "--help", "get");
        var result = await Runner.RunAsync("config", "--help", "get", versionToken);

        AssertMatchedHelp(control, result);
        AssertLeafHelp(control);
    }

    [Theory]
    [InlineData("-h")]
    [InlineData("-?")]
    [InlineData("/?")]
    public async Task Group_HelpAlias_WithVersion_BeforeSurplus_MatchesHelpOnly(string helpAlias)
    {
        var control = await Runner.RunAsync("config", helpAlias, "bogus");
        var helpFirst = await Runner.RunAsync("config", helpAlias, "-V", "bogus");
        var versionFirst = await Runner.RunAsync("config", "-V", helpAlias, "bogus");

        AssertMatchedFailure(control, helpFirst, GroupRequiresSubcommand);
        AssertMatchedFailure(control, versionFirst, GroupRequiresSubcommand);
    }

    [Fact]
    public async Task Group_Version_BeforeSurplus_ShowsVersion()
    {
        var result = await Runner.RunAsync("config", "--version", "bogus");

        Assert.Equal(0, result.ExitCode);
        Assert.Matches(@"^\d+\.\d+\.\d+", result.StandardOutput);
        Assert.DoesNotContain("USAGE:", result.StandardOutput);
        Assert.True(string.IsNullOrWhiteSpace(result.StandardError),
            $"Expected empty stderr but got: {result.StandardError}");
    }

    [Fact]
    public async Task Leaf_Help_Version_BeforePositional_ShowsLeafHelp()
    {
        var control = await Runner.RunAsync("config", "get", "--help", "bogus");
        var result = await Runner.RunAsync("config", "get", "--help", "--version", "bogus");

        Assert.Equal(0, control.ExitCode);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal(control.StandardOutput, result.StandardOutput);
        AssertLeafHelp(result);
    }

    [Theory]
    [InlineData("--version")]
    [InlineData("-V")]
    public async Task Leaf_GetPositional_Help_WithVersionBeforePath_MatchesHelpOnly(string versionToken)
    {
        var control = await Runner.RunAsync("config", "get", "mykey", "--help");
        var result = await Runner.RunAsync("config", versionToken, "get", "mykey", "--help");

        AssertMatchedHelp(control, result);
        AssertLeafHelp(control);
    }

    [Fact]
    public async Task Leaf_SetPositionals_Help_WithVersionBeforePath_MatchesHelpOnly()
    {
        var control = await Runner.RunAsync("config", "set", "mykey", "myval", "--help");
        var result = await Runner.RunAsync("config", "--version", "set", "mykey", "myval", "--help");

        AssertMatchedHelp(control, result);
        AssertSetLeafHelp(control);
    }

    [Fact]
    public async Task Leaf_GetExtraPositional_Help_WithVersionBeforePath_MatchesHelpOnly()
    {
        var control = await Runner.RunAsync("config", "get", "extra", "--help");
        var result = await Runner.RunAsync("config", "--version", "get", "extra", "--help");

        AssertMatchedHelp(control, result);
        AssertLeafHelp(control);
    }

    [Fact]
    public async Task Leaf_DanglingOption_Help_ReportsMissingValue()
    {
        var result = await Runner.RunAsync("config", "get", "--format", "--help");

        Assert.Equal(2, result.ExitCode);
        Assert.True(string.IsNullOrWhiteSpace(result.StandardOutput),
            $"Expected empty stdout but got: {result.StandardOutput}");
        Assert.Contains("Missing value for option: --format", result.StandardError);
        Assert.Contains("Run 'test --version' to show version information.", result.StandardError);
    }

    [Theory]
    [InlineData("--version")]
    [InlineData("-V")]
    public async Task Leaf_DanglingOption_Help_WithVersionBeforePath_ShowsLeafHelp(string versionToken)
    {
        var helpOnly = await Runner.RunAsync("config", "get", "--help");
        var result = await Runner.RunAsync("config", versionToken, "get", "--format", "--help");

        Assert.Equal(0, helpOnly.ExitCode);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal(helpOnly.StandardOutput, result.StandardOutput);
        Assert.True(string.IsNullOrWhiteSpace(result.StandardError),
            $"Expected empty stderr but got: {result.StandardError}");
        AssertLeafHelp(result);
    }

    [Fact]
    public async Task HelpEquals_Value_ReportsInvalidValue()
    {
        var result = await Runner.RunAsync("--help=x");

        Assert.Equal(2, result.ExitCode);
        Assert.True(string.IsNullOrWhiteSpace(result.StandardOutput),
            $"Expected empty stdout but got: {result.StandardOutput}");
        Assert.Contains("Option '--help' does not accept a value 'x'. Use bare '--help'.", result.StandardError);
        Assert.Contains("Run 'test --help' for more information on available commands and options.", result.StandardError);
        Assert.Contains("Run 'test --version' to show version information.", result.StandardError);
    }

    [Fact]
    public async Task VersionEquals_Value_ReportsInvalidValue()
    {
        var result = await Runner.RunAsync("--version=x");

        Assert.Equal(2, result.ExitCode);
        Assert.True(string.IsNullOrWhiteSpace(result.StandardOutput),
            $"Expected empty stdout but got: {result.StandardOutput}");
        Assert.Contains("Option '--version' does not accept a value 'x'. Use bare '--version'.", result.StandardError);
        Assert.Contains("Run 'test --help' for more information on available commands and options.", result.StandardError);
        Assert.Contains("Run 'test --version' to show version information.", result.StandardError);
    }

    [Fact]
    public async Task Group_Version_WithSeparatorBeforeHelp_ShowsVersion()
    {
        var result = await Runner.RunAsync("config", "--version", "--", "--help");

        Assert.Equal(0, result.ExitCode);
        Assert.Matches(@"^\d+\.\d+\.\d+", result.StandardOutput);
        Assert.DoesNotContain("USAGE:", result.StandardOutput);
        Assert.True(string.IsNullOrWhiteSpace(result.StandardError),
            $"Expected empty stderr but got: {result.StandardError}");
    }

    [Fact]
    public async Task ConcreteRoot_Help_Version_BeforeUnknown_Errors()
    {
        var result = await Runner.RunAsync("--help", "--version", "bogus");

        Assert.Equal(2, result.ExitCode);
        Assert.True(string.IsNullOrWhiteSpace(result.StandardOutput),
            $"Expected empty stdout but got: {result.StandardOutput}");
        Assert.Contains("Unexpected argument 'bogus'", result.StandardError);
        Assert.DoesNotContain("USAGE:", result.StandardOutput);
    }

    [Fact]
    public async Task Group_Help_Version_BeforeUnknownOption_ReportsUnknownOption()
    {
        var control = await Runner.RunAsync("config", "--help", "--bogus");
        var result = await Runner.RunAsync("config", "--help", "--version", "--bogus");

        AssertMatchedFailure(control, result, "Unknown option: --bogus");
    }

    private static void AssertMatchedFailure(CliTestResult control, CliTestResult result, string expectedError)
    {
        Assert.Equal(2, control.ExitCode);
        Assert.Equal(2, result.ExitCode);
        Assert.True(string.IsNullOrWhiteSpace(control.StandardOutput),
            $"Expected empty stdout but got: {control.StandardOutput}");
        Assert.True(string.IsNullOrWhiteSpace(result.StandardOutput),
            $"Expected empty stdout but got: {result.StandardOutput}");
        Assert.Contains(expectedError, control.StandardError);
        Assert.Contains("Run 'test --version' to show version information.", control.StandardError);
        Assert.Equal(control.StandardError, result.StandardError);
    }

    private static void AssertMatchedHelp(CliTestResult control, CliTestResult result)
    {
        Assert.Equal(0, control.ExitCode);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal(control.StandardOutput, result.StandardOutput);
        Assert.Equal(control.StandardError, result.StandardError);
        Assert.True(string.IsNullOrWhiteSpace(control.StandardError),
            $"Expected empty stderr but got: {control.StandardError}");
        Assert.True(string.IsNullOrWhiteSpace(result.StandardError),
            $"Expected empty stderr but got: {result.StandardError}");
    }

    private static void AssertGroupHelp(CliTestResult result)
    {
        Assert.Contains("USAGE:", result.StandardOutput);
        Assert.Contains("Configuration values", result.StandardOutput);
        Assert.Contains("COMMANDS:", result.StandardOutput);
        Assert.Contains("Get configuration values", result.StandardOutput);
        Assert.Contains("Set configuration values", result.StandardOutput);
        Assert.DoesNotMatch(@"(?m)^\d+\.\d+\.\d+", result.StandardOutput);
        Assert.True(string.IsNullOrWhiteSpace(result.StandardError),
            $"Expected empty stderr but got: {result.StandardError}");
    }

    private static void AssertLeafHelp(CliTestResult result)
    {
        Assert.Contains("USAGE:", result.StandardOutput);
        Assert.Contains("test config get", result.StandardOutput);
        Assert.Contains("Get configuration values", result.StandardOutput);
        Assert.Contains("ARGUMENTS:", result.StandardOutput);
        Assert.DoesNotContain("COMMANDS:", result.StandardOutput);
        Assert.DoesNotContain("Getting configuration for key", result.StandardOutput);
        Assert.True(string.IsNullOrWhiteSpace(result.StandardError),
            $"Expected empty stderr but got: {result.StandardError}");
    }

    private static void AssertSetLeafHelp(CliTestResult result)
    {
        Assert.Contains("USAGE:", result.StandardOutput);
        Assert.Contains("test config set", result.StandardOutput);
        Assert.Contains("Set configuration values", result.StandardOutput);
        Assert.Contains("ARGUMENTS:", result.StandardOutput);
        Assert.DoesNotContain("COMMANDS:", result.StandardOutput);
        Assert.True(string.IsNullOrWhiteSpace(result.StandardError),
            $"Expected empty stderr but got: {result.StandardError}");
    }
}

/// <summary>
/// Tests help-plus-version precedence on the implementation-less root branch,
/// including surplus classification, version-before-path leaf positionals,
/// typo-before-help parity, and the nested named grouping parent's near-miss
/// pointer and leaf routing. Runs in the non-parallel
/// <c>ConsoleDecoupling</c> collection.
/// </summary>
[Collection("ConsoleDecoupling")]
public sealed class HelpVersionSurplusAbstractRootTests
{
    private static readonly SemaphoreSlim ConsoleGate = new(1, 1);

    [Command("greet", "Greets the specified name.")]
    public sealed class RootlessGreetCommand : Command
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine("Hello!");
            return ValueTask.CompletedTask;
        }
    }

    [Command("config", "Configuration values")]
    public abstract class RootlessConfigCommand : Command
    {
    }

    [Command("config get", "Get configuration values")]
    public sealed class RootlessConfigGetCommand : RootlessConfigCommand
    {
        [CommandArgument("key", Description = "Configuration key", Position = 0, Required = false)]
        public string? Key { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine("config get");
            return ValueTask.CompletedTask;
        }
    }

    [Command("config set", "Set configuration values")]
    public sealed class RootlessConfigSetCommand : RootlessConfigCommand
    {
        [CommandArgument("key", Description = "Configuration key", Position = 0, Required = true)]
        public required string Key { get; set; }

        [CommandArgument("value", Description = "Configuration value", Position = 1, Required = true)]
        public required string Value { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine("config set");
            return ValueTask.CompletedTask;
        }
    }

    [Fact]
    public async Task AbstractRoot_Help_BeforeSurplus_RequiresSubcommand()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["--help", "bogus"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("'<root>' requires a subcommand", error);
        Assert.Contains("Available subcommands: config, greet", error);
    }

    [Fact]
    public async Task AbstractRoot_Help_Version_BeforeSurplus_MatchesHelpOnly()
    {
        var control = await RunCapturedAsync(CreateBuilder, ["--help", "bogus"]);
        var helpFirst = await RunCapturedAsync(CreateBuilder, ["--help", "--version", "bogus"]);
        var versionFirst = await RunCapturedAsync(CreateBuilder, ["--version", "--help", "bogus"]);

        AssertMatchedFailure(control, helpFirst, "'<root>' requires a subcommand");
        AssertMatchedFailure(control, versionFirst, "'<root>' requires a subcommand");
    }

    [Fact]
    public async Task AbstractRoot_Help_Version_BeforeHit_ShowsLeafHelp()
    {
        var control = await RunCapturedAsync(CreateBuilder, ["--help", "greet"]);
        var helpFirst = await RunCapturedAsync(CreateBuilder, ["--help", "--version", "greet"]);
        var versionFirst = await RunCapturedAsync(CreateBuilder, ["--version", "--help", "greet"]);

        Assert.Equal(0, control.ExitCode);
        Assert.Equal(0, helpFirst.ExitCode);
        Assert.Equal(0, versionFirst.ExitCode);
        Assert.Contains("Greets the specified name.", control.Output);
        Assert.Equal(control.Output, helpFirst.Output);
        Assert.Equal(control.Output, versionFirst.Output);
        Assert.True(string.IsNullOrWhiteSpace(helpFirst.Error), $"Expected empty stderr but got: {helpFirst.Error}");
        Assert.True(string.IsNullOrWhiteSpace(versionFirst.Error), $"Expected empty stderr but got: {versionFirst.Error}");
    }

    [Fact]
    public async Task AbstractRoot_NamedGroup_Help_Version_BeforeNearMiss_MatchesHelpOnly()
    {
        var control = await RunCapturedAsync(CreateBuilder, ["--help", "config", "sett"]);
        var helpFirst = await RunCapturedAsync(CreateBuilder, ["--help", "--version", "config", "sett"]);
        var versionFirst = await RunCapturedAsync(CreateBuilder, ["--version", "--help", "config", "sett"]);

        AssertMatchedFailure(control, helpFirst, "Unknown subcommand 'sett'");
        AssertMatchedFailure(control, versionFirst, "Unknown subcommand 'sett'");
        Assert.Contains("Did you mean 'set'?", control.Error);
    }

    [Fact]
    public async Task AbstractRoot_NamedGroup_Help_Version_BeforeHit_ShowsLeafHelp()
    {
        var control = await RunCapturedAsync(CreateBuilder, ["--help", "config", "get"]);
        var helpFirst = await RunCapturedAsync(CreateBuilder, ["--help", "--version", "config", "get"]);
        var versionFirst = await RunCapturedAsync(CreateBuilder, ["--version", "--help", "config", "get"]);

        Assert.Equal(0, control.ExitCode);
        Assert.Equal(0, helpFirst.ExitCode);
        Assert.Equal(0, versionFirst.ExitCode);
        Assert.Contains("Get configuration values", control.Output);
        Assert.Equal(control.Output, helpFirst.Output);
        Assert.Equal(control.Output, versionFirst.Output);
        Assert.True(string.IsNullOrWhiteSpace(helpFirst.Error), $"Expected empty stderr but got: {helpFirst.Error}");
        Assert.True(string.IsNullOrWhiteSpace(versionFirst.Error), $"Expected empty stderr but got: {versionFirst.Error}");
    }

    [Fact]
    public async Task AbstractRoot_LeafPositional_Help_WithVersionBeforePath_MatchesHelpOnly()
    {
        var control = await RunCapturedAsync(CreateBuilder, ["config", "get", "mykey", "--help"]);
        var pathVersion = await RunCapturedAsync(CreateBuilder, ["config", "--version", "get", "mykey", "--help"]);
        var pathShortVersion = await RunCapturedAsync(CreateBuilder, ["config", "-V", "get", "mykey", "--help"]);
        var rootVersion = await RunCapturedAsync(CreateBuilder, ["--version", "config", "get", "mykey", "--help"]);

        AssertMatchedHelp(control, pathVersion);
        AssertMatchedHelp(control, pathShortVersion);
        AssertMatchedHelp(control, rootVersion);
        Assert.Contains("config get", control.Output);
        Assert.Contains("ARGUMENTS:", control.Output);
        Assert.DoesNotContain("COMMANDS:", control.Output);
    }

    [Fact]
    public async Task AbstractRoot_LeafRequiredPositionals_Help_WithVersionBeforePath_MatchesHelpOnly()
    {
        var control = await RunCapturedAsync(CreateBuilder, ["config", "set", "mykey", "myval", "--help"]);
        var pathVersion = await RunCapturedAsync(CreateBuilder, ["config", "--version", "set", "mykey", "myval", "--help"]);
        var rootVersion = await RunCapturedAsync(CreateBuilder, ["--version", "config", "set", "mykey", "myval", "--help"]);

        AssertMatchedHelp(control, pathVersion);
        AssertMatchedHelp(control, rootVersion);
        Assert.Contains("config set", control.Output);
        Assert.Contains("ARGUMENTS:", control.Output);
        Assert.DoesNotContain("COMMANDS:", control.Output);
    }

    [Fact]
    public async Task AbstractRoot_LeafExtraPositional_Help_WithVersionBeforePath_MatchesHelpOnly()
    {
        var control = await RunCapturedAsync(CreateBuilder, ["config", "get", "extra", "--help"]);
        var pathVersion = await RunCapturedAsync(CreateBuilder, ["config", "--version", "get", "extra", "--help"]);
        var rootVersion = await RunCapturedAsync(CreateBuilder, ["--version", "config", "get", "extra", "--help"]);

        AssertMatchedHelp(control, pathVersion);
        AssertMatchedHelp(control, rootVersion);
        Assert.Contains("Get configuration values", control.Output);
        Assert.DoesNotContain("COMMANDS:", control.Output);
    }

    [Fact]
    public async Task AbstractRoot_TypoBeforeHelp_RequiresSubcommandWithPointer()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["config", "sett", "--help"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("'config' requires a subcommand", error);
        Assert.Contains("Available subcommands: get, set", error);
        Assert.Contains("Did you mean 'set'?", error);
    }

    [Theory]
    [InlineData("--version")]
    [InlineData("-V")]
    public async Task AbstractRoot_TypoBeforeHelp_WithVersionPrefix_MatchesHelpOnly(string versionToken)
    {
        var control = await RunCapturedAsync(CreateBuilder, ["config", "sett", "--help"]);
        var pathVersion = await RunCapturedAsync(CreateBuilder, ["config", versionToken, "sett", "--help"]);
        var rootVersion = await RunCapturedAsync(CreateBuilder, [versionToken, "config", "sett", "--help"]);

        AssertMatchedFailure(control, pathVersion, "Did you mean 'set'?");
        AssertMatchedFailure(control, rootVersion, "Did you mean 'set'?");
        Assert.Contains("'config' requires a subcommand", control.Error);
    }

    [Theory]
    [InlineData("--version")]
    [InlineData("-V")]
    public async Task AbstractRoot_VersionPrefix_BeforeUnknown_MatchesHelpOnly(string versionToken)
    {
        var control = await RunCapturedAsync(CreateBuilder, ["bogus", "--help"]);
        var result = await RunCapturedAsync(CreateBuilder, [versionToken, "bogus", "--help"]);

        AssertMatchedFailure(control, result, "No command found for 'bogus'");
    }

    private static ApplicationBuilder CreateBuilder()
    {
        return ApplicationBuilder.Create()
            .SetExecutableName("help-version-surplus-abstract")
            .SetExecutableTitle("Help Version Surplus Abstract")
            .SetExecutableDescription("Abstract root verification CLI.")
            .SetExecutableVersion("9.9.9")
            .AddCommand<RootlessGreetCommand>()
            .AddCommand<RootlessConfigGetCommand>()
            .AddCommand<RootlessConfigSetCommand>();
    }

    private static void AssertMatchedHelp(
        (int ExitCode, string Output, string Error) control,
        (int ExitCode, string Output, string Error) result)
    {
        Assert.Equal(0, control.ExitCode);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal(control.Output, result.Output);
        Assert.Equal(control.Error, result.Error);
        Assert.True(string.IsNullOrWhiteSpace(control.Error), $"Expected empty stderr but got: {control.Error}");
        Assert.True(string.IsNullOrWhiteSpace(result.Error), $"Expected empty stderr but got: {result.Error}");
    }

    private static void AssertMatchedFailure(
        (int ExitCode, string Output, string Error) control,
        (int ExitCode, string Output, string Error) result,
        string expectedError)
    {
        Assert.Equal(2, control.ExitCode);
        Assert.Equal(2, result.ExitCode);
        Assert.True(string.IsNullOrWhiteSpace(control.Output), $"Expected empty stdout but got: {control.Output}");
        Assert.True(string.IsNullOrWhiteSpace(result.Output), $"Expected empty stdout but got: {result.Output}");
        Assert.Contains(expectedError, control.Error);
        Assert.Equal(control.Error, result.Error);
    }

    private static async Task<(int ExitCode, string Output, string Error)> RunCapturedAsync(
        Func<ApplicationBuilder> create,
        string[] args)
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
                var exitCode = await create().RunAsync(args);
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
