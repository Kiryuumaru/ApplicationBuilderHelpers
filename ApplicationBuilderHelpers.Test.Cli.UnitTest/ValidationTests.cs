using ApplicationBuilderHelpers.Test.Cli.UnitTest.TestFramework;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// Tests for validation of command line options including type validation and FromAmong constraints
/// </summary>
public class ValidationTests : CliTestBase
{
    [Fact]
    public async Task Valid_Output_Format_Should_Succeed()
    {
        var result = await Runner.RunAsync("test", "target", "--output-format=json", "-v");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Output Format: json");
    }

    [Fact]
    public async Task Invalid_Output_Format_Should_Fail()
    {
        var result = await Runner.RunAsync("test", "target", "--output-format=invalid");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertErrorContains(result, "Value 'invalid' is not valid for option '--output-format'");
        CliTestAssertions.AssertErrorContains(result, "Must be one of: json, xml, junit, console");
    }

    [Fact]
    public async Task Case_Insensitive_FromAmong_Validation()
    {
        var result = await Runner.RunAsync("test", "target", "--output-format=JSON", "-v");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Output Format: JSON");
    }

    [Fact]
    public async Task Valid_Framework_Should_Succeed()
    {
        var result = await Runner.RunAsync("test", "target", "--framework=net8.0", "-v");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Framework: net8.0");
    }

    [Fact]
    public async Task Invalid_Framework_Should_Fail()
    {
        var result = await Runner.RunAsync("test", "target", "--framework=netfx4.8");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertErrorContains(result, "Value 'netfx4.8' is not valid for option '--framework'");
        CliTestAssertions.AssertErrorContains(result, "Must be one of: net6.0, net7.0, net8.0, net9.0");
    }

    [Fact]
    public async Task Invalid_Integer_Value_Should_Fail()
    {
        var result = await Runner.RunAsync("test", "target", "--timeout=notanumber");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertErrorContains(result, "Invalid value 'notanumber'");
    }

    [Fact]
    public async Task Valid_Integer_Value_Should_Succeed()
    {
        var result = await Runner.RunAsync("test", "target", "--timeout=60", "-v");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Timeout: 60s");
    }

    [Fact]
    public async Task Negative_Integer_Should_Succeed()
    {
        var result = await Runner.RunAsync("test", "target", "--seed=-123", "-v");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Random Seed: -123");
    }

    [Fact]
    public async Task Invalid_Double_Value_Should_Fail()
    {
        var result = await Runner.RunAsync("test", "target", "--coverage-threshold=notadouble");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertErrorContains(result, "Invalid value 'notadouble'");
    }

    [Fact]
    public async Task Valid_Double_Value_Should_Succeed()
    {
        var result = await Runner.RunAsync("test", "target", "--coverage-threshold=85.5", "-v");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Coverage Threshold: 85.5%");
    }

    [Fact]
    public async Task Double_With_Different_Decimal_Separators()
    {
        var result = await Runner.RunAsync("test", "target", "--coverage-threshold=80.0", "-v");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Coverage Threshold: 80%");
    }

    [Fact]
    public async Task Missing_Required_Argument_Should_Be_Handled_Gracefully()
    {
        var result = await Runner.RunAsync("test");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Running test on target: ");
    }

    [Fact]
    public async Task Clear_Error_Message_For_Unknown_Option()
    {
        var result = await Runner.RunAsync("test", "target", "--unknown-option=value");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertErrorContains(result, "Unknown option");
    }

    [Fact]
    public async Task Help_Shows_Validation_Constraints()
    {
        var result = await Runner.RunAsync("test", "--help");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Possible values:");
        CliTestAssertions.AssertOutputContains(result, "json, xml, junit, console");
    }
}