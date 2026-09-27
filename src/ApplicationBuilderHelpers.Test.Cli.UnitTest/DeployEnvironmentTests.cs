using ApplicationBuilderHelpers.Test.Cli.UnitTest.TestFramework;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// Tests for deploy environment variable handling: the base --env slot,
/// the deploy-specific -e/--env-vars slot, their concatenation, and help output.
/// </summary>
public class DeployEnvironmentTests : CliTestBase
{
    [Fact]
    public async Task Deploy_With_Base_Env_Space_Form()
    {
        var result = await Runner.RunAsync("deploy", "production", "--env", "BASE_ONLY=1");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Deploying to environment: production");
        CliTestAssertions.AssertOutputContains(result, "Environment Variables: BASE_ONLY=1");
    }

    [Fact]
    public async Task Deploy_With_Base_Env_Equals_Form()
    {
        var result = await Runner.RunAsync("deploy", "production", "--env=BASE_EQUALS=1");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Environment Variables: BASE_EQUALS=1");
    }

    [Fact]
    public async Task Deploy_With_Derived_Env_Vars_Short_Form()
    {
        var result = await Runner.RunAsync("deploy", "production", "-e", "DEPLOY_SHORT=1");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Environment Variables: DEPLOY_SHORT=1");
    }

    [Fact]
    public async Task Deploy_With_Derived_Env_Vars_Long_Form()
    {
        var result = await Runner.RunAsync("deploy", "production", "--env-vars", "DEPLOY_LONG=1");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Environment Variables: DEPLOY_LONG=1");
    }

    [Fact]
    public async Task Deploy_With_Combined_Base_And_Derived_Env_Vars_Preserves_Order()
    {
        var result = await Runner.RunAsync("deploy", "production", "--env", "BASE_FIRST=1", "--env-vars", "DERIVED_SECOND=2");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Environment Variables: BASE_FIRST=1, DERIVED_SECOND=2");
    }

    [Fact]
    public async Task Deploy_Help_Lists_Both_Env_Options_With_Distinct_Descriptions()
    {
        var result = await Runner.RunAsync("deploy", "--help");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "--env-vars");
        CliTestAssertions.AssertOutputContains(result, "Environment variables to set");
        CliTestAssertions.AssertOutputContains(result, "Additional deployment-specific environment variables to set");
    }
}
