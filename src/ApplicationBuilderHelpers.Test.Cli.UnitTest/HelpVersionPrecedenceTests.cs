using ApplicationBuilderHelpers.Test.Cli.UnitTest.TestFramework;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// Tests for help/version flag precedence: a bare valued option never consumes
/// a flag-looking neighbor (#469, reject-by-default), so the neighbor binds or
/// errors on its own merits while the valued option falls back to the
/// trailing-bare missing sentinel (env fallback or MissingRequired), and help
/// never masks path errors.
/// </summary>
public class HelpVersionPrecedenceTests : CliTestBase
{
    [Fact]
    public async Task Version_Flag_As_Option_Value_Is_Not_Consumed()
    {
        // #469: --version is flag-looking, so --config leaves it alone and the
        // post-parse version check fires on the leftover token.
        var result = await Runner.RunAsync("test", "mytarget", "--config", "--version");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputMatches(result, @"\d+\.\d+\.\d+");
        CliTestAssertions.AssertOutputDoesNotContain(result, "Running test on target");
    }

    [Fact]
    public async Task Known_Flag_Neighbor_Reports_Missing_Value()
    {
        var result = await Runner.RunAsync("test", "mytarget", "--config", "--verbose");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Missing value for option: -c, --config");
        CliTestAssertions.AssertOutputDoesNotContain(result, "config=\"--verbose\"");
    }

    [Fact]
    public async Task Unknown_Flag_Neighbor_Errors_On_Merits()
    {
        var result = await Runner.RunAsync("test", "mytarget", "--config", "--nope");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Unknown option: --nope");
    }

    [Fact]
    public async Task Required_Valued_Option_With_Flag_Neighbor_Reports_Missing()
    {
        var result = await Runner.RunAsync("required-test", "mytarget", "--name", "--force", "--email", "e@x.com");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Missing required option: -n, --name");
    }

    [Fact]
    public async Task Command_Help_Takes_Command_Path()
    {
        var result = await Runner.RunAsync("deploy", "--help");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Deploy applications to various environments");
    }

    [Fact]
    public async Task Help_Does_Not_Mask_Unknown_Option_Error()
    {
        var result = await Runner.RunAsync("build", "MyProject.csproj", "--unknown-option", "--help");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertErrorContains(result, "Unknown option: --unknown-option");
    }

    [Fact]
    public async Task Help_Does_Not_Mask_Unknown_Command_Error()
    {
        var result = await Runner.RunAsync("deply", "--help");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertErrorContains(result, "No command found");
    }

    [Fact]
    public async Task Help_Skips_Required_Validation()
    {
        // #509: help always wins over missing required. Missing required
        // options/arguments no longer block help-with-values (exit 0 + USAGE).
        var result = await Runner.RunAsync("required-test", "mytarget", "--help");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertExitCode(result, 0);
        CliTestAssertions.AssertOutputContains(result, "USAGE:");
    }

    [Fact]
    public async Task Help_Skips_Required_Argument_Validation()
    {
        // #509: missing required argument also yields to help (exit 0 + USAGE).
        var result = await Runner.RunAsync("required-test", "--name", "John", "--email", "john@example.com", "--help");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertExitCode(result, 0);
        CliTestAssertions.AssertOutputContains(result, "USAGE:");
    }

    [Fact]
    public async Task Help_Skips_All_Required_Validation()
    {
        // #509: nothing provided at all beyond the command — still help (exit 0).
        var result = await Runner.RunAsync("required-test", "--help");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertExitCode(result, 0);
        CliTestAssertions.AssertOutputContains(result, "USAGE:");
    }

    [Fact]
    public async Task Short_Help_Skips_Required_Validation()
    {
        // #509 short-flag variant: -h wins over missing required too (exit 0 + USAGE).
        var result = await Runner.RunAsync("required-test", "mytarget", "-h");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertExitCode(result, 0);
        CliTestAssertions.AssertOutputContains(result, "USAGE:");
    }

    [Fact]
    public async Task Version_Skips_Required_Validation()
    {
        // #509 version pin: missing required yields to the Step 4b version
        // guard, which fires before validation (exit 0, no USAGE).
        var result = await Runner.RunAsync("required-test", "mytarget", "--version");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertExitCode(result, 0);
        CliTestAssertions.AssertOutputMatches(result, @"\d+\.\d+\.\d+");
        CliTestAssertions.AssertOutputDoesNotContain(result, "USAGE:");
    }

    [Fact]
    public async Task Version_Takes_Precedence_Over_Help()
    {
        var result = await Runner.RunAsync("--version", "--help");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputMatches(result, @"\d+\.\d+\.\d+");
        CliTestAssertions.AssertOutputDoesNotContain(result, "USAGE:");
    }
}
