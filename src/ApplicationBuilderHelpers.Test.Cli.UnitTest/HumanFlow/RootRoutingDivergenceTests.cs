using ApplicationBuilderHelpers.Test.Cli.UnitTest.TestFramework;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest.HumanFlow;

/// <summary>
/// Leading bare <c>--help</c> at the concrete root (MainCommand merged at
/// root) wins over trailing tokens: <c>--help false</c>, <c>--help test</c>,
/// and <c>--help --bogus</c> render root help (exit 0) instead of reporting
/// exit 2. Leaf help routing with a trailing value is preserved.
/// </summary>
public class RootRoutingDivergenceTests : CliTestBase
{
    [Fact]
    public async Task ConcreteRoot_Help_WithTrailingValue_ShowsRootHelp()
    {
        var result = await Runner.RunAsync("--help", "false");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertExitCode(result, 0);
        CliTestAssertions.AssertOutputContains(result, "USAGE:");
        CliTestAssertions.AssertOutputContains(result, "COMMANDS:");
        CliTestAssertions.AssertOutputContains(result, "GLOBAL OPTIONS:");
        CliTestAssertions.AssertOutputDoesNotContain(result, "ApplicationBuilderHelpers Test CLI - Default Command");
    }

    [Fact]
    public async Task ConcreteRoot_Help_WithTrailingCommandName_ShowsRootHelp()
    {
        var result = await Runner.RunAsync("--help", "test");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertExitCode(result, 0);
        CliTestAssertions.AssertOutputContains(result, "USAGE:");
        CliTestAssertions.AssertOutputContains(result, "test [OPTIONS] <COMMAND> [ARGS...]");
        CliTestAssertions.AssertOutputContains(result, "COMMANDS:");
        CliTestAssertions.AssertOutputContains(result, "GLOBAL OPTIONS:");
        CliTestAssertions.AssertOutputDoesNotContain(result, "OPTIONS (command):");
        CliTestAssertions.AssertOutputDoesNotContain(result, "ARGUMENTS:");
        CliTestAssertions.AssertOutputDoesNotContain(result, "ApplicationBuilderHelpers Test CLI - Default Command");
    }

    [Fact]
    public async Task ConcreteRoot_Help_WithTrailingUnknownOption_ShowsRootHelp()
    {
        var result = await Runner.RunAsync("--help", "--bogus");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertExitCode(result, 0);
        CliTestAssertions.AssertOutputContains(result, "USAGE:");
        CliTestAssertions.AssertOutputContains(result, "COMMANDS:");
        CliTestAssertions.AssertOutputContains(result, "GLOBAL OPTIONS:");
        CliTestAssertions.AssertOutputDoesNotContain(result, "ApplicationBuilderHelpers Test CLI - Default Command");
    }

    [Fact]
    public async Task Leaf_Help_WithTrailingValue_ShowsLeafHelp()
    {
        var result = await Runner.RunAsync("test", "--help", "false");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertExitCode(result, 0);
        CliTestAssertions.AssertOutputContains(result, "USAGE:");
        CliTestAssertions.AssertOutputContains(result, "Run various test operations");
        CliTestAssertions.AssertOutputDoesNotContain(result, "COMMANDS:");
    }
}
