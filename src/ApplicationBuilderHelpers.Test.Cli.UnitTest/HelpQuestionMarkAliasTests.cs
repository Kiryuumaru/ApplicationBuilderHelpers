using ApplicationBuilderHelpers.Test.Cli.UnitTest.TestFramework;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// Tests for the <c>-?</c> and <c>/?</c> help aliases: bare tokens behave like
/// <c>--help</c>/<c>-h</c> on every OS, while <c>?</c> never expands in a
/// cluster, <c>/?</c> never resolves as a path/subcommand, and
/// <c>-?=x</c>/<c>/?=x</c> are usage errors (exit 2), never help.
/// </summary>
public class HelpQuestionMarkAliasTests : CliTestBase
{
    [Fact]
    public async Task BareDashQuestion_ShowsGlobalHelp()
    {
        var result = await Runner.RunAsync("-?");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertExitCode(result, 0);
        CliTestAssertions.AssertOutputContains(result, "USAGE:");
        CliTestAssertions.AssertOutputContains(result, "GLOBAL OPTIONS:");
        CliTestAssertions.AssertOutputContains(result, "Run 'test <command> --help'");
    }

    [Fact]
    public async Task BareSlashQuestion_ShowsGlobalHelp()
    {
        var result = await Runner.RunAsync("/?");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertExitCode(result, 0);
        CliTestAssertions.AssertOutputContains(result, "USAGE:");
        CliTestAssertions.AssertOutputContains(result, "GLOBAL OPTIONS:");
    }

    [Fact]
    public async Task Subcommand_DashQuestion_ShowsCommandHelp()
    {
        var result = await Runner.RunAsync("test", "-?");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Run various test operations");
        CliTestAssertions.AssertOutputContains(result, "OPTIONS:");
        CliTestAssertions.AssertOutputDoesNotContain(result, "Running test on target");
    }

    [Fact]
    public async Task Subcommand_SlashQuestion_ShowsCommandHelp()
    {
        var result = await Runner.RunAsync("test", "/?");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Run various test operations");
        CliTestAssertions.AssertOutputContains(result, "OPTIONS:");
        CliTestAssertions.AssertOutputDoesNotContain(result, "Running test on target");
    }

    [Fact]
    public async Task NestedSubcommand_DashQuestion_ShowsLeafHelp()
    {
        var result = await Runner.RunAsync("config", "get", "-?");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Get configuration values");
        CliTestAssertions.AssertOutputDoesNotContain(result, "Running test on target");
    }

    [Fact]
    public async Task NestedSubcommand_SlashQuestion_ShowsLeafHelp()
    {
        var result = await Runner.RunAsync("config", "get", "/?");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Get configuration values");
        CliTestAssertions.AssertOutputDoesNotContain(result, "Running test on target");
    }

    [Fact]
    public async Task Separator_DashQuestion_StaysPositional()
    {
        var result = await Runner.RunAsync("test", "--", "-?");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Running test on target: -?");
    }

    [Fact]
    public async Task Separator_SlashQuestion_StaysPositional()
    {
        var result = await Runner.RunAsync("test", "--", "/?");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Running test on target: /?");
    }

    [Fact]
    public async Task Alias_WithVersion_ShowsHelp()
    {
        var result = await Runner.RunAsync("test", "mytarget", "-?", "--version");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertExitCode(result, 0);
        CliTestAssertions.AssertOutputContains(result, "USAGE:");
        CliTestAssertions.AssertOutputDoesNotContain(result, "Running test on target");
    }

    [Fact]
    public async Task Alias_WithBadValue_ErrorsOnValue()
    {
        var result = await Runner.RunAsync("test", "mytarget", "--timeout=banana", "-?");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Invalid value 'banana' for option '--timeout'");
    }

    [Fact]
    public async Task DashQuestionEquals_IsInvalidValue()
    {
        var result = await Runner.RunAsync("test", "mytarget", "-?=x");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Invalid Boolean value 'x' for option '-?'");
    }

    [Fact]
    public async Task SlashQuestionEquals_IsInvalidValue()
    {
        var result = await Runner.RunAsync("test", "mytarget", "/?=x");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Invalid Boolean value 'x' for option '/?'");
    }

    [Fact]
    public async Task AbstractCommand_DashQuestionEquals_IsInvalidValue()
    {
        var result = await Runner.RunAsync("config", "-?=x");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Invalid Boolean value 'x' for option '-?'");
    }

    [Fact]
    public async Task AbstractCommand_SlashQuestionEquals_IsInvalidValue()
    {
        var result = await Runner.RunAsync("config", "/?=x");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Invalid Boolean value 'x' for option '/?'");
    }

    [Fact]
    public async Task Question_NeverExpandsInCluster()
    {
        var result = await Runner.RunAsync("test", "mytarget", "-v?");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Unknown option: -?");
    }

    [Fact]
    public async Task SlashQuestion_LeadsHelpTargetRouting()
    {
        var result = await Runner.RunAsync("/?", "test");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Run various test operations");
        CliTestAssertions.AssertOutputDoesNotContain(result, "Unknown");
    }

    [Fact]
    public async Task UnknownCommand_WithAlias_StillErrors()
    {
        var result = await Runner.RunAsync("bogus", "-?");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "No command found");
    }
}
