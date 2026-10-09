using ApplicationBuilderHelpers.Test.Cli.UnitTest.TestFramework;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// Tests that unknown-option errors beat bare <c>--help</c> regardless of
/// argument order or command path: concrete root, abstract grouping, and
/// leaf commands all report the unknown option (exit 2) instead of showing
/// help. A bare word after a resolved help path errors (exit 2) only when
/// the resolved node offers no bindable positional slot; otherwise it is
/// forgiven as positional context and leaf help shows (exit 0).
/// Misused <c>--help</c> value forms stay invalid values (exit 2) and
/// the <c>--</c> separator keeps blocking help.
/// </summary>
public class HelpUnknownOrderInvarianceTests : CliTestBase
{
    [Fact]
    public async Task ConcreteRoot_Help_Before_Unknown_Reports_Unknown()
    {
        var result = await Runner.RunAsync("--help", "--bogus");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Unknown option: --bogus");
        CliTestAssertions.AssertOutputDoesNotContain(result, "USAGE:");
        CliTestAssertions.AssertOutputDoesNotContain(result, "COMMANDS:");
    }

    [Fact]
    public async Task ConcreteRoot_Unknown_Before_Help_Reports_Unknown()
    {
        var result = await Runner.RunAsync("--bogus", "--help");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Unknown option: --bogus");
        CliTestAssertions.AssertOutputDoesNotContain(result, "USAGE:");
        CliTestAssertions.AssertOutputDoesNotContain(result, "COMMANDS:");
    }

    [Fact]
    public async Task AbstractGrouping_Help_Before_Unknown_Reports_Unknown()
    {
        var result = await Runner.RunAsync("config", "--help", "--bogus");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Unknown option: --bogus");
        CliTestAssertions.AssertOutputDoesNotContain(result, "USAGE:");
        CliTestAssertions.AssertOutputDoesNotContain(result, "COMMANDS:");
    }

    [Fact]
    public async Task AbstractGrouping_Help_WithHitPlusExtra_ShowsLeafHelp()
    {
        // Leaf 'config get' offers a [key] positional slot, so the trailing
        // bare word is forgiven as positional context and leaf help shows.
        var result = await Runner.RunAsync("config", "--help", "get", "extra");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertExitCode(result, 0);
        CliTestAssertions.AssertOutputContains(result, "USAGE:");
        CliTestAssertions.AssertOutputContains(result, "Get configuration values");
        CliTestAssertions.AssertOutputDoesNotContain(result, "COMMANDS:");
    }

    [Fact]
    public async Task AbstractGrouping_Unknown_Before_Help_Reports_Unknown()
    {
        var result = await Runner.RunAsync("config", "--bogus", "--help");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Unknown option: --bogus");
        CliTestAssertions.AssertOutputDoesNotContain(result, "USAGE:");
        CliTestAssertions.AssertOutputDoesNotContain(result, "COMMANDS:");
    }

    [Fact]
    public async Task Leaf_Help_Before_Unknown_Reports_Unknown()
    {
        var result = await Runner.RunAsync("build", "MyProject.csproj", "--help", "--bogus");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Unknown option: --bogus");
        CliTestAssertions.AssertOutputDoesNotContain(result, "USAGE:");
        CliTestAssertions.AssertOutputDoesNotContain(result, "Building project:");
    }

    [Fact]
    public async Task Leaf_Unknown_Before_Help_Reports_Unknown()
    {
        var result = await Runner.RunAsync("build", "MyProject.csproj", "--bogus", "--help");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Unknown option: --bogus");
        CliTestAssertions.AssertOutputDoesNotContain(result, "USAGE:");
        CliTestAssertions.AssertOutputDoesNotContain(result, "Building project:");
    }

    [Fact]
    public async Task HelpEquals_Value_Reports_Invalid()
    {
        var result = await Runner.RunAsync("test", "mytarget", "--help=true");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Option '--help' does not accept a value 'true'. Use bare '--help'.");
        CliTestAssertions.AssertOutputDoesNotContain(result, "USAGE:");
        CliTestAssertions.AssertOutputDoesNotContain(result, "Running test on target");
    }

    [Fact]
    public async Task Sentinel_Blocks_Help_Control()
    {
        var result = await Runner.RunAsync("--", "--help");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Unexpected argument '--help'");
        CliTestAssertions.AssertOutputDoesNotContain(result, "USAGE:");
        CliTestAssertions.AssertOutputDoesNotContain(result, "COMMANDS:");
    }

    [Fact]
    public async Task Sentinel_Before_Help_Keeps_Requires_Subcommand()
    {
        var result = await Runner.RunAsync("config", "--", "--help");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "'config' requires a subcommand");
        CliTestAssertions.AssertErrorContains(result, "Available subcommands: get, set");
        CliTestAssertions.AssertOutputDoesNotContain(result, "USAGE:");
    }
}
