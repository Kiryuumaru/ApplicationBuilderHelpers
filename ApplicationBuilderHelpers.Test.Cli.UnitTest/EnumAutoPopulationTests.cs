using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.Test.Cli.UnitTest.TestFramework;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// Tests for automatic enum value population when FromAmong is not specified
/// </summary>
public class EnumAutoPopulationTests : CliTestBase
{
    [Fact]
    public async Task Valid_Enum_Value_Should_Succeed()
    {
        var result = await Runner.RunAsync("test", "target", "--log-level=Debug", "-v");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Log Level: debug");
    }

    [Fact]
    public async Task Case_Insensitive_Enum_Value_Should_Succeed()
    {
        var result = await Runner.RunAsync("test", "target", "--log-level=warning", "-v");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Log Level: warning");
    }

    [Fact]
    public async Task Invalid_Enum_Value_Should_Fail_With_Auto_Generated_FromAmong()
    {
        var result = await Runner.RunAsync("test", "target", "--log-level=InvalidValue");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertErrorContains(result, "Value 'InvalidValue' is not valid for option '--log-level'");
        CliTestAssertions.AssertErrorContains(result, "Must be one of:");
        // Should contain all the enum values specified in FromAmong
        CliTestAssertions.AssertErrorContains(result, "trace");
        CliTestAssertions.AssertErrorContains(result, "debug");
        CliTestAssertions.AssertErrorContains(result, "information");
        CliTestAssertions.AssertErrorContains(result, "warning");
        CliTestAssertions.AssertErrorContains(result, "error");
        CliTestAssertions.AssertErrorContains(result, "critical");
        CliTestAssertions.AssertErrorContains(result, "none");
    }

    [Fact]
    public async Task Default_Enum_Value_Should_Work()
    {
        var result = await Runner.RunAsync("test", "target", "-v");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Log Level: information");
    }

    [Fact]
    public async Task Help_Should_Show_Enum_Values()
    {
        var result = await Runner.RunAsync("test", "--help");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "--log-level");
        CliTestAssertions.AssertOutputContains(result, "Set the logging level");
    }

    [Fact]
    public async Task Short_Option_Should_Work_With_Enum()
    {
        var result = await Runner.RunAsync("test", "target", "-l", "error", "-v");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Log Level: error");
    }
}