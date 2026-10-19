using ApplicationBuilderHelpers.Test.Cli.UnitTest.TestFramework;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// Tests for consumed help/version tokens on commands compared with the
/// requests kept by grouping nodes. On a command with its own run, a required
/// valued option consumes a following <c>--help</c>/<c>-h</c>/<c>-?</c>/<c>/?</c>/<c>--version</c>/<c>-V</c>
/// token as its value, so the consumed token neither shows help nor version;
/// a grouping node beside a dangling valued option still keeps all six
/// request forms (<c>--help</c>, <c>-h</c>, <c>-?</c>, <c>/?</c>, <c>--version</c>,
/// <c>-V</c>) as requests. Flag-looking values bind explicitly through the <c>=</c>
/// form, a consumed help token leaves deferred conversion errors unforgiven,
/// and an empty <c>=</c> form on a string option still satisfies
/// <c>Required</c>.
/// </summary>
public class ConsumedHelpVersionTokenTests : CliTestBase
{
    [Theory]
    [InlineData("--help")]
    [InlineData("-h")]
    [InlineData("-?")]
    [InlineData("/?")]
    [InlineData("--version")]
    [InlineData("-V")]
    public async Task RunNode_RequiredValuedOption_Consumes_HelpVersionToken_AsValue(string helpVersionToken)
    {
        var result = await Runner.RunAsync("required-test", "mytarget", "--name", helpVersionToken, "--email", "e@x.com");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertExitCode(result, 0);
        CliTestAssertions.AssertNoError(result);
        CliTestAssertions.AssertOutputContains(result, "Required Test Command Executed");
        CliTestAssertions.AssertOutputContains(result, $"Name: {helpVersionToken}");
        CliTestAssertions.AssertOutputDoesNotContain(result, "USAGE:");
        Assert.DoesNotMatch(@"(?m)^\d+\.\d+\.\d+", result.StandardOutput);
    }

    [Theory]
    [InlineData("--help")]
    [InlineData("-h")]
    [InlineData("-?")]
    [InlineData("/?")]
    public async Task GroupingNode_HelpToken_BesideDanglingValuedOption_ShowsHelp(string helpToken)
    {
        var result = await Runner.RunAsync("config", "--format", helpToken);
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertExitCode(result, 0);
        CliTestAssertions.AssertNoError(result);
        CliTestAssertions.AssertOutputContains(result, "Configuration values");
        CliTestAssertions.AssertOutputContains(result, "OPTIONS (command):");
        CliTestAssertions.AssertOutputDoesNotContain(result, "requires a subcommand");
        Assert.DoesNotMatch(@"(?m)^\d+\.\d+\.\d+", result.StandardOutput);
    }

    [Theory]
    [InlineData("--version")]
    [InlineData("-V")]
    public async Task GroupingNode_VersionToken_BesideDanglingValuedOption_ShowsVersion(string versionToken)
    {
        var result = await Runner.RunAsync("config", "--format", versionToken);
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertExitCode(result, 0);
        CliTestAssertions.AssertNoError(result);
        CliTestAssertions.AssertOutputMatches(result, @"\d+\.\d+\.\d+");
        CliTestAssertions.AssertOutputDoesNotContain(result, "USAGE:");
    }

    [Fact]
    public async Task RequiredStringOption_EqualsForm_BindsHelpToken_AsValue()
    {
        var result = await Runner.RunAsync("required-test", "mytarget", "--name=--help", "--email", "e@x.com");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertExitCode(result, 0);
        CliTestAssertions.AssertNoError(result);
        CliTestAssertions.AssertOutputContains(result, "Name: --help");
        CliTestAssertions.AssertOutputDoesNotContain(result, "USAGE:");
        Assert.DoesNotMatch(@"(?m)^\d+\.\d+\.\d+", result.StandardOutput);
    }

    [Fact]
    public async Task OptionalStringOption_EqualsForm_BindsHelpToken_AsValue()
    {
        var result = await Runner.RunAsync("test", "mytarget", "--config=--help");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertExitCode(result, 0);
        CliTestAssertions.AssertNoError(result);
        CliTestAssertions.AssertOutputContains(result, "config=\"--help\"");
        CliTestAssertions.AssertOutputDoesNotContain(result, "USAGE:");
    }

    [Fact]
    public async Task Deferred_Conversion_Error_With_Consumed_Help_Stays_Error()
    {
        var result = await Runner.RunAsync("required-test", "mytarget", "--name", "--help", "--email", "e@x.com", "--age=abc");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Invalid value 'abc' for option '--age'");
        CliTestAssertions.AssertErrorContains(result, "Invalid Int32 value: 'abc'");
        CliTestAssertions.AssertOutputDoesNotContain(result, "USAGE:");
        CliTestAssertions.AssertOutputDoesNotContain(result, "Required Test Command Executed");
    }

    [Fact]
    public async Task Deferred_Conversion_Error_With_Unconsumed_Help_Shows_Help()
    {
        var result = await Runner.RunAsync("required-test", "mytarget", "--name", "John", "--email", "e@x.com", "--age=abc", "--help");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertExitCode(result, 0);
        CliTestAssertions.AssertNoError(result);
        CliTestAssertions.AssertOutputContains(result, "USAGE:");
        CliTestAssertions.AssertOutputDoesNotContain(result, "Required Test Command Executed");
        Assert.DoesNotMatch(@"(?m)^\d+\.\d+\.\d+", result.StandardOutput);
    }

    [Fact]
    public async Task Empty_Equals_String_Option_BindsEmpty_And_SatisfiesRequired()
    {
        var result = await Runner.RunAsync("required-test", "mytarget", "--name=", "--email", "e@x.com");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertExitCode(result, 0);
        CliTestAssertions.AssertNoError(result);
        CliTestAssertions.AssertOutputContains(result, "Required Test Command Executed");
        Assert.Contains(result.OutputLines, line => line.TrimEnd() == "Name:");
    }
}
