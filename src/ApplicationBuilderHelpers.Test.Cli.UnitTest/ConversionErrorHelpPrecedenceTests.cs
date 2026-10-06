using ApplicationBuilderHelpers.Test.Cli.UnitTest.TestFramework;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// Tests for the help/version validation gate: conversion failures stay forgiven
/// beside <c>--help</c>/<c>--version</c> kind-blind, so allowed-value
/// (<c>FromAmong</c>) violations show help or version (exit 0) when help or
/// version is present, while valid values keep help and the bare-help,
/// bare-valued-option, version, and missing-required carve-outs are preserved.
/// Per-element collection conversion failures are pinned in-process by
/// <c>TypePipelineScalarMatrixTests</c>; this class pins the
/// collection-valid-with-help preservation through the help-with-values path.
/// </summary>
public class ConversionErrorHelpPrecedenceTests : CliTestBase
{
    [Fact]
    public async Task Scalar_Invalid_Value_With_Trailing_Help_Shows_Help()
    {
        var result = await Runner.RunAsync("test", "target", "--timeout=notanumber", "--help");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertExitCode(result, 0);
        CliTestAssertions.AssertNoError(result);
        CliTestAssertions.AssertOutputContains(result, "USAGE:");
        CliTestAssertions.AssertOutputDoesNotContain(result, "Running test on target");
    }

    [Fact]
    public async Task Scalar_Invalid_Value_With_Leading_Help_Shows_Help()
    {
        var result = await Runner.RunAsync("test", "target", "--help", "--timeout=notanumber");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertExitCode(result, 0);
        CliTestAssertions.AssertNoError(result);
        CliTestAssertions.AssertOutputContains(result, "USAGE:");
        CliTestAssertions.AssertOutputDoesNotContain(result, "Running test on target");
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
    public async Task Constrained_Option_Invalid_Value_With_Trailing_Help_Shows_Help()
    {
        var result = await Runner.RunAsync("test", "target", "--output-format=yaml", "--help");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertExitCode(result, 0);
        CliTestAssertions.AssertNoError(result);
        CliTestAssertions.AssertOutputContains(result, "USAGE:");
        CliTestAssertions.AssertOutputDoesNotContain(result, "Running test on target");
    }

    [Fact]
    public async Task Constrained_Option_Invalid_Value_With_Leading_Help_Shows_Help()
    {
        var result = await Runner.RunAsync("test", "target", "--help", "--output-format=yaml");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertExitCode(result, 0);
        CliTestAssertions.AssertNoError(result);
        CliTestAssertions.AssertOutputContains(result, "USAGE:");
        CliTestAssertions.AssertOutputDoesNotContain(result, "Running test on target");
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
    public async Task Constrained_Argument_Invalid_Value_With_Trailing_Help_Shows_Help()
    {
        var result = await Runner.RunAsync("plugin", "bogus", "--help");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertExitCode(result, 0);
        CliTestAssertions.AssertNoError(result);
        CliTestAssertions.AssertOutputContains(result, "USAGE:");
        CliTestAssertions.AssertOutputDoesNotContain(result, "Listing installed plugins");
    }

    [Fact]
    public async Task Constrained_Argument_Invalid_Value_With_Leading_Help_Shows_Help()
    {
        var result = await Runner.RunAsync("plugin", "--help", "bogus");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertExitCode(result, 0);
        CliTestAssertions.AssertNoError(result);
        CliTestAssertions.AssertOutputContains(result, "USAGE:");
        CliTestAssertions.AssertOutputDoesNotContain(result, "Listing installed plugins");
    }

    [Fact]
    public async Task Constrained_Option_Invalid_Value_With_Trailing_Version_Shows_Version()
    {
        var result = await Runner.RunAsync("test", "target", "--output-format=yaml", "--version");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertExitCode(result, 0);
        CliTestAssertions.AssertNoError(result);
        CliTestAssertions.AssertOutputMatches(result, @"\d+\.\d+\.\d+");
        CliTestAssertions.AssertOutputDoesNotContain(result, "Running test on target");
    }

    [Fact]
    public async Task Constrained_Option_Invalid_Value_With_Leading_Version_Shows_Version()
    {
        var result = await Runner.RunAsync("test", "target", "--version", "--output-format=yaml");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertExitCode(result, 0);
        CliTestAssertions.AssertNoError(result);
        CliTestAssertions.AssertOutputMatches(result, @"\d+\.\d+\.\d+");
        CliTestAssertions.AssertOutputDoesNotContain(result, "Running test on target");
    }

    [Fact]
    public async Task Constrained_Argument_Invalid_Value_With_Trailing_Version_Shows_Version()
    {
        var result = await Runner.RunAsync("plugin", "bogus", "--version");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertExitCode(result, 0);
        CliTestAssertions.AssertNoError(result);
        CliTestAssertions.AssertOutputMatches(result, @"\d+\.\d+\.\d+");
        CliTestAssertions.AssertOutputDoesNotContain(result, "Listing installed plugins");
    }

    [Fact]
    public async Task Constrained_Argument_Invalid_Value_With_Leading_Version_Shows_Version()
    {
        var result = await Runner.RunAsync("plugin", "--version", "bogus");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertExitCode(result, 0);
        CliTestAssertions.AssertNoError(result);
        CliTestAssertions.AssertOutputMatches(result, @"\d+\.\d+\.\d+");
        CliTestAssertions.AssertOutputDoesNotContain(result, "Listing installed plugins");
    }

    [Fact]
    public async Task Constrained_Option_Invalid_Value_With_Trailing_Short_Help_Shows_Help()
    {
        var result = await Runner.RunAsync("test", "target", "--output-format=yaml", "-h");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertExitCode(result, 0);
        CliTestAssertions.AssertNoError(result);
        CliTestAssertions.AssertOutputContains(result, "USAGE:");
        CliTestAssertions.AssertOutputDoesNotContain(result, "Running test on target");
    }

    [Fact]
    public async Task Constrained_Option_Invalid_Value_With_Leading_Short_Help_Shows_Help()
    {
        var result = await Runner.RunAsync("test", "target", "-h", "--output-format=yaml");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertExitCode(result, 0);
        CliTestAssertions.AssertNoError(result);
        CliTestAssertions.AssertOutputContains(result, "USAGE:");
        CliTestAssertions.AssertOutputDoesNotContain(result, "Running test on target");
    }

    [Fact]
    public async Task Constrained_Option_Invalid_Value_With_Trailing_Short_Version_Shows_Version()
    {
        var result = await Runner.RunAsync("test", "target", "--output-format=yaml", "-V");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertExitCode(result, 0);
        CliTestAssertions.AssertNoError(result);
        CliTestAssertions.AssertOutputMatches(result, @"\d+\.\d+\.\d+");
        CliTestAssertions.AssertOutputDoesNotContain(result, "Running test on target");
    }

    [Fact]
    public async Task Constrained_Option_Invalid_Value_With_Leading_Short_Version_Shows_Version()
    {
        var result = await Runner.RunAsync("test", "target", "-V", "--output-format=yaml");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertExitCode(result, 0);
        CliTestAssertions.AssertNoError(result);
        CliTestAssertions.AssertOutputMatches(result, @"\d+\.\d+\.\d+");
        CliTestAssertions.AssertOutputDoesNotContain(result, "Running test on target");
    }

    [Fact]
    public async Task Constrained_Option_Invalid_Value_With_Help_And_Version_Shows_Help()
    {
        var result = await Runner.RunAsync("test", "target", "--output-format=yaml", "--help", "--version");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertExitCode(result, 0);
        CliTestAssertions.AssertNoError(result);
        CliTestAssertions.AssertOutputContains(result, "USAGE:");
        Assert.DoesNotMatch(@"(?m)^\d+\.\d+\.\d+", result.StandardOutput);
        CliTestAssertions.AssertOutputDoesNotContain(result, "Running test on target");
    }

    [Fact]
    public async Task Constrained_Option_Invalid_Value_With_Version_And_Help_Shows_Help()
    {
        var result = await Runner.RunAsync("test", "target", "--output-format=yaml", "--version", "--help");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertExitCode(result, 0);
        CliTestAssertions.AssertNoError(result);
        CliTestAssertions.AssertOutputContains(result, "USAGE:");
        Assert.DoesNotMatch(@"(?m)^\d+\.\d+\.\d+", result.StandardOutput);
        CliTestAssertions.AssertOutputDoesNotContain(result, "Running test on target");
    }

    [Fact]
    public async Task Constrained_Option_Repeated_Last_Invalid_Value_With_Help_Shows_Help()
    {
        var result = await Runner.RunAsync("test", "target", "--output-format=json", "--output-format=yaml", "--help");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertExitCode(result, 0);
        CliTestAssertions.AssertNoError(result);
        CliTestAssertions.AssertOutputContains(result, "USAGE:");
        CliTestAssertions.AssertOutputDoesNotContain(result, "Running test on target");
    }

    [Fact]
    public async Task Constrained_Option_Repeated_Last_Valid_Value_With_Help_Shows_Help()
    {
        var result = await Runner.RunAsync("test", "target", "--output-format=yaml", "--output-format=json", "--help");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertExitCode(result, 0);
        CliTestAssertions.AssertNoError(result);
        CliTestAssertions.AssertOutputContains(result, "USAGE:");
        CliTestAssertions.AssertOutputDoesNotContain(result, "Running test on target");
    }

    [Fact]
    public async Task Constrained_Option_Valid_Value_With_Version_Shows_Version()
    {
        var result = await Runner.RunAsync("test", "target", "--output-format=json", "--version");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertExitCode(result, 0);
        CliTestAssertions.AssertNoError(result);
        CliTestAssertions.AssertOutputMatches(result, @"\d+\.\d+\.\d+");
        CliTestAssertions.AssertOutputDoesNotContain(result, "Running test on target");
    }

    [Fact]
    public async Task Constrained_Argument_Valid_Value_With_Version_Shows_Version()
    {
        var result = await Runner.RunAsync("plugin", "list", "--version");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertExitCode(result, 0);
        CliTestAssertions.AssertNoError(result);
        CliTestAssertions.AssertOutputMatches(result, @"\d+\.\d+\.\d+");
        CliTestAssertions.AssertOutputDoesNotContain(result, "Listing installed plugins");
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
    public async Task Empty_Equals_Int_With_Help_Reports_Error()
    {
        var result = await Runner.RunAsync("test", "target", "--timeout=", "--help");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Invalid value '' for option '--timeout'");
        CliTestAssertions.AssertErrorContains(result, "Expected a whole number between -2147483648 and 2147483647.");
        CliTestAssertions.AssertErrorContains(result, "Run 'test --version' to show version information.");
    }

    [Fact]
    public async Task Empty_Equals_String_With_Help_Shows_Help()
    {
        var result = await Runner.RunAsync("test", "target", "--config=", "--help");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertExitCode(result, 0);
        CliTestAssertions.AssertOutputContains(result, "USAGE:");
    }

    [Fact]
    public async Task Double_Invalid_Value_With_Help_Shows_Help()
    {
        var result = await Runner.RunAsync("test", "target", "--coverage-threshold=notadouble", "--help");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertExitCode(result, 0);
        CliTestAssertions.AssertNoError(result);
        CliTestAssertions.AssertOutputContains(result, "USAGE:");
        CliTestAssertions.AssertOutputDoesNotContain(result, "Running test on target");
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
    public async Task Bare_Valued_Option_With_Help_Reports_Missing()
    {
        var result = await Runner.RunAsync("test", "mytarget", "--config", "--help");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Missing value for option: -c, --config");
    }

    [Fact]
    public async Task Bare_Valued_Option_With_Help_And_Environment_Reports_Missing()
    {
        var envVars = new Dictionary<string, string>
        {
            ["TEST_CONFIG"] = "env-config.json"
        };
        var result = await Runner.RunAsync(envVars, "test", "target", "--config", "--help");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Missing value for option: -c, --config");
    }

    [Fact]
    public async Task Deferred_Conversion_Error_With_Help_Shows_Help()
    {
        var result = await Runner.RunAsync("required-test", "mytarget", "--age", "abc", "--help");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertExitCode(result, 0);
        CliTestAssertions.AssertNoError(result);
        CliTestAssertions.AssertOutputContains(result, "USAGE:");
        CliTestAssertions.AssertOutputDoesNotContain(result, "Required Test Command Executed");
    }

    [Fact]
    public async Task Invalid_Value_With_Version_Shows_Version()
    {
        var result = await Runner.RunAsync("test", "target", "--timeout=notanumber", "--version");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertExitCode(result, 0);
        CliTestAssertions.AssertNoError(result);
        CliTestAssertions.AssertOutputMatches(result, @"\d+\.\d+\.\d+");
        CliTestAssertions.AssertOutputDoesNotContain(result, "Running test on target");
    }

    [Fact]
    public async Task Deferred_Invalid_Value_With_Leading_Version_Shows_Version()
    {
        var result = await Runner.RunAsync("test", "target", "--version", "--timeout=notanumber");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertExitCode(result, 0);
        CliTestAssertions.AssertNoError(result);
        CliTestAssertions.AssertOutputMatches(result, @"\d+\.\d+\.\d+");
        CliTestAssertions.AssertOutputDoesNotContain(result, "Running test on target");
    }

    [Fact]
    public async Task Flag_Equals_Invalid_Literal_With_VersionCluster_Shows_Version()
    {
        var result = await Runner.RunAsync("test", "target", "--verbose=maybe", "-vV");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertExitCode(result, 0);
        CliTestAssertions.AssertNoError(result);
        CliTestAssertions.AssertOutputMatches(result, @"\d+\.\d+\.\d+");
        CliTestAssertions.AssertOutputDoesNotContain(result, "Running test on target");
    }

    [Fact]
    public async Task Flag_Equals_Invalid_Literal_With_Trailing_Version_Shows_Version()
    {
        var result = await Runner.RunAsync("test", "target", "--verbose=maybe", "--version");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertExitCode(result, 0);
        CliTestAssertions.AssertNoError(result);
        CliTestAssertions.AssertOutputMatches(result, @"\d+\.\d+\.\d+");
        CliTestAssertions.AssertOutputDoesNotContain(result, "Running test on target");
    }

    [Fact]
    public async Task Flag_Equals_Invalid_Literal_With_Leading_Version_Shows_Version()
    {
        var result = await Runner.RunAsync("test", "target", "--version", "--verbose=maybe");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertExitCode(result, 0);
        CliTestAssertions.AssertNoError(result);
        CliTestAssertions.AssertOutputMatches(result, @"\d+\.\d+\.\d+");
        CliTestAssertions.AssertOutputDoesNotContain(result, "Running test on target");
    }

    [Fact]
    public async Task Flag_Equals_Invalid_Literal_Before_Separator_With_Version_After_Stays_Error()
    {
        var result = await Runner.RunAsync("test", "target", "--verbose=maybe", "--", "--version");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Option '--verbose' does not accept a value 'maybe'");
        Assert.DoesNotMatch(@"(?m)^\d+\.\d+\.\d+", result.StandardOutput);
    }

    [Fact]
    public async Task Flag_Equals_Invalid_Literal_Without_Version_Reports_Invalid_Value()
    {
        var result = await Runner.RunAsync("test", "target", "--verbose=maybe");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Option '--verbose' does not accept a value 'maybe'");
        Assert.DoesNotMatch(@"(?m)^\d+\.\d+\.\d+", result.StandardOutput);
    }

    [Fact]
    public async Task Flag_Equals_Invalid_Literal_With_Version_And_Help_Shows_Help()
    {
        var result = await Runner.RunAsync("test", "target", "--verbose=maybe", "--version", "--help");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertExitCode(result, 0);
        CliTestAssertions.AssertNoError(result);
        CliTestAssertions.AssertOutputContains(result, "USAGE:");
        Assert.DoesNotMatch(@"(?m)^\d+\.\d+\.\d+", result.StandardOutput);
        CliTestAssertions.AssertOutputDoesNotContain(result, "Running test on target");
    }

    [Fact]
    public async Task No_Value_Rejected_With_Trailing_Version_Shows_Version()
    {
        var result = await Runner.RunAsync("test", "target", "--no-verbose=x", "--version");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertExitCode(result, 0);
        CliTestAssertions.AssertNoError(result);
        CliTestAssertions.AssertOutputMatches(result, @"\d+\.\d+\.\d+");
        CliTestAssertions.AssertOutputDoesNotContain(result, "Running test on target");
    }

    [Fact]
    public async Task No_Value_Rejected_With_Leading_Version_Shows_Version()
    {
        var result = await Runner.RunAsync("test", "target", "--version", "--no-verbose=x");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertExitCode(result, 0);
        CliTestAssertions.AssertNoError(result);
        CliTestAssertions.AssertOutputMatches(result, @"\d+\.\d+\.\d+");
        CliTestAssertions.AssertOutputDoesNotContain(result, "Running test on target");
    }

    [Fact]
    public async Task No_Value_Rejected_Without_Version_Reports_Invalid_Value()
    {
        var result = await Runner.RunAsync("test", "target", "--no-verbose=x");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "does not accept a value");
        Assert.DoesNotMatch(@"(?m)^\d+\.\d+\.\d+", result.StandardOutput);
    }

    [Fact]
    public async Task Unknown_Option_With_Version_Stays_Error()
    {
        var result = await Runner.RunAsync("test", "target", "--bogus", "--version");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Unknown option: --bogus");
        Assert.DoesNotMatch(@"(?m)^\d+\.\d+\.\d+", result.StandardOutput);
    }

    [Fact]
    public async Task Version_With_Trailing_Unknown_Option_Stays_Error()
    {
        var result = await Runner.RunAsync("test", "target", "--version", "--bogus");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Unknown option: --bogus");
        Assert.DoesNotMatch(@"(?m)^\d+\.\d+\.\d+", result.StandardOutput);
    }

    [Fact]
    public async Task Version_With_Space_Separated_Unknown_Word_Stays_Error()
    {
        var result = await Runner.RunAsync("test", "target", "--version", "bogus");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        Assert.DoesNotMatch(@"(?m)^\d+\.\d+\.\d+", result.StandardOutput);
    }

    [Fact]
    public async Task Unknown_Command_With_Version_Stays_Error()
    {
        var result = await Runner.RunAsync("bogus", "--version");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "No command found");
        Assert.DoesNotMatch(@"(?m)^\d+\.\d+\.\d+", result.StandardOutput);
    }

    [Fact]
    public async Task SingleDash_Help_Reports_Unknown_With_Suggestion_And_Footer_Hints()
    {
        var result = await Runner.RunAsync("test", "target", "-help");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Unknown option: -help");
        CliTestAssertions.AssertErrorContains(result, "Did you mean '--help'?");
        CliTestAssertions.AssertErrorContains(result, "Run 'test test --help'");
        CliTestAssertions.AssertErrorContains(result, "Run 'test --version'");
    }

    [Fact]
    public async Task SingleDash_Version_Reports_Unknown_With_Suggestion_And_Footer_Hints()
    {
        var result = await Runner.RunAsync("test", "target", "-version");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Unknown option: -version");
        CliTestAssertions.AssertErrorContains(result, "Did you mean '--version'?");
        CliTestAssertions.AssertErrorContains(result, "Run 'test test --help'");
        CliTestAssertions.AssertErrorContains(result, "Run 'test --version'");
    }

    [Fact]
    public async Task Empty_Equals_Int_With_Version_Shows_Version()
    {
        var result = await Runner.RunAsync("test", "target", "--timeout=", "--version");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertExitCode(result, 0);
        CliTestAssertions.AssertOutputMatches(result, @"\d+\.\d+\.\d+");
        CliTestAssertions.AssertOutputDoesNotContain(result, "Running test on target");
    }

    [Fact]
    public async Task Bare_Valued_Option_With_Version_Shows_Version()
    {
        var result = await Runner.RunAsync("test", "target", "--config", "--version");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertExitCode(result, 0);
        CliTestAssertions.AssertOutputMatches(result, @"\d+\.\d+\.\d+");
        CliTestAssertions.AssertOutputDoesNotContain(result, "Running test on target");
    }

    [Fact]
    public async Task Global_SingleDash_Help_Reports_Unknown_With_Suggestion_And_Footer_Hints()
    {
        var result = await Runner.RunAsync("-help");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Unknown option: -help");
        CliTestAssertions.AssertErrorContains(result, "Did you mean '--help'?");
        CliTestAssertions.AssertErrorContains(result, "Run 'test --help'");
        CliTestAssertions.AssertErrorContains(result, "Run 'test --version'");
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
