using ApplicationBuilderHelpers.Test.Cli.UnitTest.TestFramework;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// Tests for conversion-error precedence over help-with-values: a collected
/// value that fails type conversion or allowed-value validation reports exit 2
/// even when <c>--help</c> is present, while valid values keep help and the
/// bare-help, bare-valued-option, version, and missing-required carve-outs are
/// preserved. Per-element collection conversion failures are pinned in-process
/// by <c>TypePipelineScalarMatrixTests</c>; this class pins the
/// collection-valid-with-help preservation through the help-with-values path.
/// </summary>
public class ConversionErrorHelpPrecedenceTests : CliTestBase
{
    [Fact]
    public async Task Scalar_Invalid_Value_With_Trailing_Help_Reports_Conversion_Error()
    {
        var result = await Runner.RunAsync("test", "target", "--timeout=notanumber", "--help");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Invalid Int32 value: 'notanumber'");
    }

    [Fact]
    public async Task Scalar_Invalid_Value_With_Leading_Help_Reports_Conversion_Error()
    {
        var result = await Runner.RunAsync("test", "target", "--help", "--timeout=notanumber");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Invalid Int32 value: 'notanumber'");
    }

    [Fact]
    public async Task Scalar_Invalid_Value_Without_Help_Reports_Conversion_Error()
    {
        var result = await Runner.RunAsync("test", "target", "--timeout=notanumber");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Invalid Int32 value: 'notanumber'");
    }

    [Fact]
    public async Task Scalar_Valid_Value_With_Help_Shows_Help()
    {
        var result = await Runner.RunAsync("test", "target", "--timeout=60", "--help");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "USAGE:");
        CliTestAssertions.AssertOutputDoesNotContain(result, "Running test on target");
    }

    [Fact]
    public async Task Constrained_Option_Invalid_Value_With_Trailing_Help_Reports_Allowed_Values()
    {
        var result = await Runner.RunAsync("test", "target", "--output-format=yaml", "--help");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Value 'yaml' is not valid for option '--output-format'");
        CliTestAssertions.AssertErrorContains(result, "Must be one of:");
    }

    [Fact]
    public async Task Constrained_Option_Invalid_Value_With_Leading_Help_Reports_Allowed_Values()
    {
        var result = await Runner.RunAsync("test", "target", "--help", "--output-format=yaml");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Value 'yaml' is not valid for option '--output-format'");
        CliTestAssertions.AssertErrorContains(result, "Must be one of:");
    }

    [Fact]
    public async Task Constrained_Option_Invalid_Value_Without_Help_Reports_Allowed_Values()
    {
        var result = await Runner.RunAsync("test", "target", "--output-format=yaml");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Value 'yaml' is not valid for option '--output-format'");
        CliTestAssertions.AssertErrorContains(result, "Must be one of:");
    }

    [Fact]
    public async Task Constrained_Option_Valid_Value_With_Help_Shows_Help()
    {
        var result = await Runner.RunAsync("test", "target", "--output-format=json", "--help");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "USAGE:");
        CliTestAssertions.AssertOutputDoesNotContain(result, "Running test on target");
    }

    [Fact]
    public async Task Constrained_Argument_Invalid_Value_With_Trailing_Help_Reports_Allowed_Values()
    {
        var result = await Runner.RunAsync("plugin", "bogus", "--help");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Value 'bogus' is not valid for argument 'action'");
        CliTestAssertions.AssertErrorContains(result, "Must be one of:");
    }

    [Fact]
    public async Task Constrained_Argument_Invalid_Value_With_Leading_Help_Reports_Allowed_Values()
    {
        var result = await Runner.RunAsync("plugin", "--help", "bogus");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Value 'bogus' is not valid for argument 'action'");
        CliTestAssertions.AssertErrorContains(result, "Must be one of:");
    }

    [Fact]
    public async Task Constrained_Argument_Valid_Value_With_Help_Shows_Help()
    {
        var result = await Runner.RunAsync("plugin", "list", "--help");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Manage plugins and extensions");
        CliTestAssertions.AssertOutputDoesNotContain(result, "Listing installed plugins");
    }

    [Fact]
    public async Task Double_Invalid_Value_With_Help_Reports_Conversion_Error()
    {
        var result = await Runner.RunAsync("test", "target", "--coverage-threshold=notadouble", "--help");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Invalid Double value: 'notadouble'");
    }

    [Fact]
    public async Task Collection_Valid_Values_With_Help_Shows_Help()
    {
        var result = await Runner.RunAsync("test", "target", "--tags", "unit", "--tags", "integration", "--help");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "USAGE:");
        CliTestAssertions.AssertOutputDoesNotContain(result, "Running test on target");
    }

    [Fact]
    public async Task Bare_Valued_Option_With_Help_Shows_Help()
    {
        var result = await Runner.RunAsync("test", "mytarget", "--config", "--help");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "USAGE:");
    }

    [Fact]
    public async Task Bare_Valued_Option_With_Help_And_Environment_Shows_Help()
    {
        var envVars = new Dictionary<string, string>
        {
            ["TEST_CONFIG"] = "env-config.json"
        };
        var result = await Runner.RunAsync(envVars, "test", "target", "--config", "--help");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "USAGE:");
    }

    [Fact]
    public async Task Missing_Required_With_Help_Beats_Conversion_Error()
    {
        var result = await Runner.RunAsync("required-test", "mytarget", "--age", "abc", "--help");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Missing required option");
    }

    [Fact]
    public async Task Invalid_Value_With_Version_Shows_Version()
    {
        var result = await Runner.RunAsync("test", "target", "--timeout=notanumber", "--version");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputMatches(result, @"\d+\.\d+\.\d+");
        CliTestAssertions.AssertOutputDoesNotContain(result, "Running test on target");
    }

    [Fact]
    public async Task Bare_Command_Help_Shows_Help()
    {
        var result = await Runner.RunAsync("test", "--help");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "USAGE:");
    }

    [Fact]
    public async Task Global_Help_Shows_Help()
    {
        var result = await Runner.RunAsync("--help");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "USAGE:");
        CliTestAssertions.AssertOutputContains(result, "GLOBAL OPTIONS:");
    }

    [Fact]
    public async Task Config_Command_Help_Shows_Help()
    {
        var result = await Runner.RunAsync("config", "--help");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Configuration values");
    }
}
