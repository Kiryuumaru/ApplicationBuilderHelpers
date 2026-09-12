using ApplicationBuilderHelpers.Test.Cli.UnitTest.TestFramework;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// Tests for help/version flag precedence: flags are resolved after the
/// command path is walked and option values are consumed, so a flag-looking
/// token in value position never fires, and help never masks path errors.
/// </summary>
public class HelpVersionPrecedenceTests : CliTestBase
{
    [Fact]
    public async Task Version_Flag_As_Option_Value_Is_Consumed_As_Value()
    {
        var result = await Runner.RunAsync("test", "mytarget", "--config", "--version");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Running test on target: mytarget");
        CliTestAssertions.AssertOutputContains(result, "config=\"--version\"");
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
    public async Task Help_Does_Not_Skip_Required_Option_Validation()
    {
        var result = await Runner.RunAsync("required-test", "mytarget", "--help");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertErrorContains(result, "Missing required option");
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
