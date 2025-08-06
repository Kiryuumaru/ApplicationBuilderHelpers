using System.Collections.Generic;
using System.Threading.Tasks;
using ApplicationBuilderHelpers.Test.Cli.UnitTest.TestFramework;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// Tests for edge cases and boundary conditions in command line parsing
/// </summary>
public class EdgeCaseTests : CliTestBase
{
    [Fact]
    public async Task Empty_String_As_Option_Value()
    {
        var result = await Runner.RunAsync("test", "target", "--config=", "-v");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Config: ");
    }

    [Fact]
    public async Task Unicode_In_Arguments()
    {
        var result = await Runner.RunAsync("test", "unicode-target", "-v");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Running test on target: unicode-target");
    }

    [Fact]
    public async Task Special_Characters_In_Option_Values()
    {
        var result = await Runner.RunAsync("test", "target", 
            "--config=config with spaces.json",
            "--filter=name='test case'",
            "-v");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Config: config with spaces.json");
        CliTestAssertions.AssertOutputContains(result, "Filter: name='test case'");
    }

    [Fact]
    public async Task Option_With_Equals_In_Value()
    {
        var result = await Runner.RunAsync("test", "target", "--filter=key=value", "-v");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Filter: key=value");
    }

    [Fact]
    public async Task Negative_Number_As_Option_Value()
    {
        var result = await Runner.RunAsync("test", "target", "--seed=-12345", "-v");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Random Seed: -12345");
    }

    [Fact]
    public async Task Mixed_Option_Styles_In_Same_Command()
    {
        var result = await Runner.RunAsync("test", "target",
            "--verbose",           // Flag
            "--timeout=60",       // Long with equals
            "-t=unit",           // Short with equals  
            "--parallel", "true"); // Boolean with value
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Timeout: 60s");
        CliTestAssertions.AssertOutputContains(result, "Tags: unit");
        CliTestAssertions.AssertOutputContains(result, "Parallel: True");
    }

    [Fact]
    public async Task Very_Long_Option_Value()
    {
        var longValue = new string('x', 500);
        var result = await Runner.RunAsync("test", "target", $"--config={longValue}", "-v");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, $"Config: {longValue}");
    }

    [Fact]
    public async Task Many_Array_Options()
    {
        var args = new List<string> { "test", "target" };
        for (int i = 0; i < 50; i++)
        {
            args.Add("--tags");
            args.Add($"tag{i:D2}");
        }
        args.Add("-v");

        var result = await Runner.RunAsync(args.ToArray());
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "tag00, tag01");
        CliTestAssertions.AssertOutputContains(result, "tag48, tag49");
    }

    [Fact]
    public async Task Zero_Values_For_Numeric_Options()
    {
        var result = await Runner.RunAsync("test", "target", 
            "--timeout=0", 
            "--coverage-threshold=0.0", 
            "--seed=0", 
            "-v");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Timeout: 0s");
        CliTestAssertions.AssertOutputContains(result, "Coverage Threshold: 0%");
        CliTestAssertions.AssertOutputContains(result, "Random Seed: 0");
    }

    [Fact]
    public async Task Maximum_Integer_Values()
    {
        var result = await Runner.RunAsync("test", "target", 
            "--timeout=2147483647", 
            "--seed=2147483647", 
            "-v");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Timeout: 2147483647s");
        CliTestAssertions.AssertOutputContains(result, "Random Seed: 2147483647");
    }

    [Fact]
    public async Task Minimum_Integer_Values()
    {
        var result = await Runner.RunAsync("test", "target", 
            "--timeout=-2147483648", 
            "--seed=-2147483648", 
            "-v");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Timeout: -2147483648s");
        CliTestAssertions.AssertOutputContains(result, "Random Seed: -2147483648");
    }

    [Fact]
    public async Task Very_Large_Double_Values()
    {
        var result = await Runner.RunAsync("test", "target", 
            "--coverage-threshold=99.999999", 
            "-v");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Coverage Threshold: 99.999999%");
    }

    [Fact]
    public async Task Multiple_Spaces_In_Arguments()
    {
        var result = await Runner.RunAsync("test", "target with multiple spaces", "-v");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Running test on target: target with multiple spaces");
    }
}