using ApplicationBuilderHelpers.Test.Cli.UnitTest.TestFramework;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// Tests for help/version flag precedence: a bare valued option never consumes
/// a flag-looking neighbor, so the neighbor binds or
/// errors on its own merits while the valued option falls back to the
/// trailing-bare missing sentinel (env fallback or MissingRequired), and help
/// never masks path errors.
/// </summary>
public class HelpVersionPrecedenceTests : CliTestBase
{
    [Fact]
    public async Task Version_Flag_As_Option_Value_Is_Not_Consumed()
    {
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
        var result = await Runner.RunAsync("required-test", "mytarget", "--help");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertExitCode(result, 0);
        CliTestAssertions.AssertOutputContains(result, "USAGE:");
    }

    [Fact]
    public async Task Help_Skips_Required_Argument_Validation()
    {
        var result = await Runner.RunAsync("required-test", "--name", "John", "--email", "john@example.com", "--help");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertExitCode(result, 0);
        CliTestAssertions.AssertOutputContains(result, "USAGE:");
    }

    [Fact]
    public async Task Help_Skips_All_Required_Validation()
    {
        var result = await Runner.RunAsync("required-test", "--help");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertExitCode(result, 0);
        CliTestAssertions.AssertOutputContains(result, "USAGE:");
    }

    [Fact]
    public async Task Short_Help_Skips_Required_Validation()
    {
        var result = await Runner.RunAsync("required-test", "mytarget", "-h");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertExitCode(result, 0);
        CliTestAssertions.AssertOutputContains(result, "USAGE:");
    }

    [Fact]
    public async Task Version_Skips_Required_Validation()
    {
        var result = await Runner.RunAsync("required-test", "mytarget", "--version");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertExitCode(result, 0);
        CliTestAssertions.AssertOutputMatches(result, @"\d+\.\d+\.\d+");
        CliTestAssertions.AssertOutputDoesNotContain(result, "USAGE:");
    }

    [Fact]
    public async Task Help_Takes_Precedence_Over_Version()
    {
        var result = await Runner.RunAsync("--version", "--help");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "USAGE:");
        Assert.DoesNotMatch(@"^\d+\.\d+\.\d+", result.StandardOutput.TrimStart());
    }

    [Fact]
    public async Task Command_Help_Takes_Precedence_Over_Version()
    {
        var result = await Runner.RunAsync("test", "mytarget", "--version", "--help");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertExitCode(result, 0);
        CliTestAssertions.AssertOutputContains(result, "USAGE:");
        Assert.DoesNotMatch(@"(?m)^\d+\.\d+\.\d+", result.StandardOutput);
        CliTestAssertions.AssertOutputDoesNotContain(result, "Running test on target");
    }

    [Fact]
    public async Task Command_Help_Before_Version_Shows_Help()
    {
        var result = await Runner.RunAsync("test", "mytarget", "--help", "--version");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertExitCode(result, 0);
        CliTestAssertions.AssertOutputContains(result, "USAGE:");
        Assert.DoesNotMatch(@"(?m)^\d+\.\d+\.\d+", result.StandardOutput);
        CliTestAssertions.AssertOutputDoesNotContain(result, "Running test on target");
    }

    [Fact]
    public async Task Command_Help_Version_Cluster_Shows_Help()
    {
        var result = await Runner.RunAsync("test", "mytarget", "-hV");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertExitCode(result, 0);
        CliTestAssertions.AssertOutputContains(result, "USAGE:");
        Assert.DoesNotMatch(@"(?m)^\d+\.\d+\.\d+", result.StandardOutput);
        CliTestAssertions.AssertOutputDoesNotContain(result, "Running test on target");
    }

    [Fact]
    public async Task Group_Unknown_Option_Before_Version_Reports_Unknown()
    {
        var result = await Runner.RunAsync("config", "--bogus", "--version");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Unknown option: --bogus");
        Assert.DoesNotMatch(@"(?m)^\d+\.\d+\.\d+", result.StandardOutput);
    }

    [Fact]
    public async Task Group_Version_Before_Unknown_Option_Reports_Unknown()
    {
        var result = await Runner.RunAsync("config", "--version", "--bogus");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Unknown option: --bogus");
        Assert.DoesNotMatch(@"(?m)^\d+\.\d+\.\d+", result.StandardOutput);
    }

    [Fact]
    public async Task Command_Unknown_Option_Before_Version_Reports_Unknown()
    {
        var result = await Runner.RunAsync("test", "mytarget", "--bogus", "--version");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Unknown option: --bogus");
        Assert.DoesNotMatch(@"(?m)^\d+\.\d+\.\d+", result.StandardOutput);
        CliTestAssertions.AssertOutputDoesNotContain(result, "Running test on target");
    }

    [Fact]
    public async Task Command_Version_Before_Unknown_Option_Reports_Unknown()
    {
        var result = await Runner.RunAsync("test", "mytarget", "--version", "--bogus");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Unknown option: --bogus");
        Assert.DoesNotMatch(@"(?m)^\d+\.\d+\.\d+", result.StandardOutput);
        CliTestAssertions.AssertOutputDoesNotContain(result, "Running test on target");
    }

    [Fact]
    public async Task Group_Separator_Before_Unknown_With_Version_Keeps_Requires_Subcommand()
    {
        var result = await Runner.RunAsync("config", "--", "--bogus", "--version");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "'config' requires a subcommand");
        CliTestAssertions.AssertErrorContains(result, "Available subcommands: get, set");
        Assert.DoesNotMatch(@"(?m)^\d+\.\d+\.\d+", result.StandardOutput);
    }

    [Fact]
    public async Task Group_Unknown_Before_Separator_With_Version_Reports_Unknown()
    {
        var result = await Runner.RunAsync("config", "--bogus", "--", "--version");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Unknown option: --bogus");
        Assert.DoesNotMatch(@"(?m)^\d+\.\d+\.\d+", result.StandardOutput);
    }

    [Fact]
    public async Task Group_Version_Before_Separator_With_Unknown_Shows_Version()
    {
        var result = await Runner.RunAsync("config", "--version", "--", "--bogus");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertExitCode(result, 0);
        CliTestAssertions.AssertOutputMatches(result, @"\d+\.\d+\.\d+");
    }

    [Fact]
    public async Task Group_Version_Shows_Version()
    {
        var result = await Runner.RunAsync("config", "--version");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertExitCode(result, 0);
        CliTestAssertions.AssertOutputMatches(result, @"\d+\.\d+\.\d+");
    }

    [Fact]
    public async Task Command_Version_Shows_Version()
    {
        var result = await Runner.RunAsync("test", "mytarget", "--version");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertExitCode(result, 0);
        CliTestAssertions.AssertOutputMatches(result, @"\d+\.\d+\.\d+");
        CliTestAssertions.AssertOutputDoesNotContain(result, "Running test on target");
    }

    [Fact]
    public async Task Group_Invalid_Flag_Literal_Before_Version_Reports_Invalid()
    {
        var result = await Runner.RunAsync("config", "--quiet=banana", "--version");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Invalid Boolean value 'banana' for option '--quiet'");
        Assert.DoesNotMatch(@"(?m)^\d+\.\d+\.\d+", result.StandardOutput);
    }

    [Fact]
    public async Task Group_Version_Before_Invalid_Flag_Literal_Reports_Invalid()
    {
        var result = await Runner.RunAsync("config", "--version", "--quiet=banana");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Invalid Boolean value 'banana' for option '--quiet'");
        Assert.DoesNotMatch(@"(?m)^\d+\.\d+\.\d+", result.StandardOutput);
    }
}
