using ApplicationBuilderHelpers.Test.Cli.UnitTest.TestFramework;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest.HumanFlow;

/// <summary>
/// Leading bare <c>--help</c> at the concrete root (MainCommand merged at
/// root) routes a valid trailing subcommand to its help: <c>--help test</c>
/// renders target help (exit 0). A bogus bare word errors (exit 2) and
/// <c>--help --bogus</c> renders root help (exit 0).
/// Leaf help routing with a trailing value is preserved.
/// </summary>
public class RootRoutingDivergenceTests : CliTestBase
{
    [Fact]
    public async Task ConcreteRoot_Help_WithTrailingValue_Errors()
    {
        var result = await Runner.RunAsync("--help", "false");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Unexpected argument 'false'");
    }

    [Fact]
    public async Task ConcreteRoot_Help_WithTrailingCommandName_ShowsTargetHelp()
    {
        var result = await Runner.RunAsync("--help", "test");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertExitCode(result, 0);
        CliTestAssertions.AssertOutputContains(result, "USAGE:");
        CliTestAssertions.AssertOutputContains(result, "Run various test operations");
        CliTestAssertions.AssertOutputDoesNotContain(result, "COMMANDS:");
    }

    [Fact]
    public async Task ConcreteRoot_Help_WithNestedPath_ShowsDeepestHelp()
    {
        var result = await Runner.RunAsync("--help", "config", "get");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertExitCode(result, 0);
        CliTestAssertions.AssertOutputContains(result, "USAGE:");
        CliTestAssertions.AssertOutputContains(result, "Get configuration values");
        CliTestAssertions.AssertOutputDoesNotContain(result, "COMMANDS:");
    }

    [Fact]
    public async Task ConcreteRoot_Help_WithSentinel_Does_Not_Route()
    {
        var result = await Runner.RunAsync("--help", "--", "test");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertExitCode(result, 0);
        CliTestAssertions.AssertOutputContains(result, "USAGE:");
        CliTestAssertions.AssertOutputContains(result, "COMMANDS:");
        CliTestAssertions.AssertOutputContains(result, "GLOBAL OPTIONS:");
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
