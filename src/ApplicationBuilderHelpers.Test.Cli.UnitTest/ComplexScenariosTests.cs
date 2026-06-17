using ApplicationBuilderHelpers.Test.Cli.UnitTest.TestFramework;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// Tests for complex command line scenarios including mixed option formats, multiple options, and advanced use cases
/// </summary>
public class ComplexScenariosTests : CliTestBase
{
    [Fact]
    public async Task Multiple_Options_With_Mixed_Formats()
    {
        var result = await Runner.RunAsync("test", "MyTarget",
            "--verbose",
            "--config", "test.json",
            "--timeout=120",
            "-t", "unit",
            "-t", "integration",
            "--parallel=true",
            "--coverage",
            "--output-format=junit");
        
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Running test on target: MyTarget");
        CliTestAssertions.AssertOutputContains(result, "Config: test.json");
        CliTestAssertions.AssertOutputContains(result, "Timeout: 120s");
        CliTestAssertions.AssertOutputContains(result, "Tags: unit, integration");
        CliTestAssertions.AssertOutputContains(result, "Parallel: True");
        CliTestAssertions.AssertOutputContains(result, "Coverage Enabled: True");
        CliTestAssertions.AssertOutputContains(result, "Output Format: junit");
    }

    [Theory]
    [InlineData("--diag", "True")]
    [InlineData("--diag=true", "True")]
    [InlineData("--diag true", "True")]
    [InlineData("--diag=false", "False")]
    [InlineData("--diag false", "False")]
    [InlineData("--diag=yes", "True")]
    [InlineData("--diag=no", "False")]
    [InlineData("--diag=1", "True")]
    [InlineData("--diag=0", "False")]
    public async Task All_Boolean_Value_Formats(string args, string expected)
    {
        var argArray = args.Split(' ');
        var fullArgs = new[] { "test", "target", "-v" }.Concat(argArray).ToArray();
        
        var result = await Runner.RunAsync(fullArgs);
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, $"Diagnostic Mode: {expected}");
    }

    [Fact]
    public async Task Global_Options_With_Commands()
    {
        // Global options alone now execute MainCommand successfully
        var result = await Runner.RunAsync("--log-level=debug");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "ApplicationBuilderHelpers Test CLI - Default Command");
    }

    [Fact]
    public async Task Mixed_Global_And_Command_Options()
    {
        var result = await Runner.RunAsync("test", "target", "--log-level=warning", "--timeout=90", "-v");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Timeout: 90s");
    }

    [Fact]
    public async Task Performance_Test_Many_Options()
    {
        var args = new List<string> { "test", "target" };
        
        // Add many options
        for (int i = 0; i < 10; i++)
        {
            args.Add("--tags");
            args.Add($"tag{i}");
        }
        args.Add("-v");

        var result = await Runner.RunAsync(args.ToArray());
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertExecutionTime(result, TimeSpan.FromSeconds(5));
        
        // Verify all tags are included
        var expectedTags = string.Join(", ", Enumerable.Range(0, 10).Select(i => $"tag{i}"));
        CliTestAssertions.AssertOutputContains(result, $"Tags: {expectedTags}");
    }

    [Fact]
    public async Task Command_Sequence_Validation()
    {
        var commands = new[]
        {
            new[] { "--version" },
            new[] { "--help" },
            new[] { "test", "--help" }
        };

        var results = new List<CliTestResult>();
        
        // Execute commands individually to ensure proper isolation
        foreach (var commandArgs in commands)
        {
            var result = await Runner.RunAsync(commandArgs);
            results.Add(result);
            
            // Each command should succeed
            CliTestAssertions.AssertSuccess(result);
        }
        
        // Check specific outputs
        CliTestAssertions.AssertOutputMatches(results[0], @"\d+\.\d+\.\d+");
        CliTestAssertions.AssertOutputContains(results[1], "USAGE:");
        CliTestAssertions.AssertOutputContains(results[2], "Run various test operations");
    }

    [Fact]
    public async Task Environment_Variables_With_Complex_Values()
    {
        var envVars = new Dictionary<string, string>
        {
            ["TEST_CONFIG"] = "path/with spaces/config.json"
        };
        
        var result = await Runner.RunAsync(envVars, "test", "target", "-v");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Config: path/with spaces/config.json");
    }

    [Fact]
    public async Task Mixed_Option_And_Argument_Order()
    {
        // Options before and after the target argument
        var result = await Runner.RunAsync("test", "--timeout=45", "mytarget", "--parallel", "-v");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Running test on target: mytarget");
        CliTestAssertions.AssertOutputContains(result, "Timeout: 45s");
        CliTestAssertions.AssertOutputContains(result, "Parallel: True");
    }

    [Fact]
    public async Task Build_Command_With_Complex_Options()
    {
        var result = await Runner.RunAsync("build", "--help");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Build the project");
    }
}