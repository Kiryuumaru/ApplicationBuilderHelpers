using System.Collections.Generic;
using System.Threading.Tasks;
using ApplicationBuilderHelpers.Test.Cli.UnitTest.TestFramework;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// Tests for basic command line parsing functionality including options, arguments, and help system
/// </summary>
public class CommandLineParsingTests : CliTestBase
{
    [Fact]
    public async Task Should_Show_Help_When_No_Arguments_Provided()
    {
        var result = await Runner.RunAsync();
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Run 'test <command> --help' for more information on specific commands");
    }

    [Fact]
    public async Task Should_Show_Version_With_Version_Flag()
    {
        var result = await Runner.RunAsync("--version");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputMatches(result, @"\d+\.\d+\.\d+");
    }

    [Fact]
    public async Task Should_Show_Version_With_V_Flag()
    {
        var result = await Runner.RunAsync("-V");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputMatches(result, @"\d+\.\d+\.\d+");
    }

    [Fact]
    public async Task Should_Show_Help_With_Help_Flag()
    {
        var result = await Runner.RunAsync("--help");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "USAGE:");
        CliTestAssertions.AssertOutputContains(result, "GLOBAL OPTIONS:");
    }

    [Fact]
    public async Task Should_Show_Help_With_H_Flag()
    {
        var result = await Runner.RunAsync("-h");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "USAGE:");
    }

    [Fact]
    public async Task Should_Show_Command_Specific_Help()
    {
        var result = await Runner.RunAsync("test", "--help");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Run various test operations");
        CliTestAssertions.AssertOutputContains(result, "OPTIONS:");
        CliTestAssertions.AssertOutputContains(result, "--verbose");
    }

    [Fact]
    public async Task Should_Handle_Unknown_Command()
    {
        var result = await Runner.RunAsync("unknowncommand");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertErrorContains(result, "No command found");
    }

    [Fact]
    public async Task Long_Option_With_Equals()
    {
        var result = await Runner.RunAsync("test", "target", "--config=test.json", "-v");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Config: test.json");
    }

    [Fact]
    public async Task Long_Option_With_Space()
    {
        var result = await Runner.RunAsync("test", "target", "--config", "test.json", "-v");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Config: test.json");
    }

    [Fact]
    public async Task Short_Option_With_Equals()
    {
        var result = await Runner.RunAsync("test", "target", "-c=test.json", "-v");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Config: test.json");
    }

    [Fact]
    public async Task Short_Option_With_Space()
    {
        var result = await Runner.RunAsync("test", "target", "-c", "test.json", "-v");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Config: test.json");
    }

    [Fact]
    public async Task Boolean_Flag_Mode()
    {
        var result = await Runner.RunAsync("test", "target", "--diag", "-v");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Diagnostic Mode: True");
    }

    [Fact]
    public async Task Boolean_With_True_Value()
    {
        var result = await Runner.RunAsync("test", "target", "--diag", "true", "-v");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Diagnostic Mode: True");
    }

    [Fact]
    public async Task Boolean_With_False_Value()
    {
        var result = await Runner.RunAsync("test", "target", "--diag", "false", "-v");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Diagnostic Mode: False");
    }

    [Fact]
    public async Task Boolean_With_Equals_True()
    {
        var result = await Runner.RunAsync("test", "target", "--diag=true", "-v");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Diagnostic Mode: True");
    }

    [Fact]
    public async Task Boolean_With_Equals_False()
    {
        var result = await Runner.RunAsync("test", "target", "--diag=false", "-v");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Diagnostic Mode: False");
    }

    [Fact]
    public async Task Boolean_With_Yes_Value()
    {
        var result = await Runner.RunAsync("test", "target", "--diag=yes", "-v");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Diagnostic Mode: True");
    }

    [Fact]
    public async Task Boolean_With_No_Value()
    {
        var result = await Runner.RunAsync("test", "target", "--diag=no", "-v");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Diagnostic Mode: False");
    }

    [Fact]
    public async Task Multiple_Boolean_Flags()
    {
        var result = await Runner.RunAsync("test", "target", "--verbose", "--parallel", "--coverage");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Parallel: True");
        CliTestAssertions.AssertOutputContains(result, "Coverage Enabled: True");
    }

    [Fact]
    public async Task Option_From_Environment_Variable()
    {
        var envVars = new Dictionary<string, string>
        {
            ["TEST_CONFIG"] = "env-config.json"
        };
        var result = await Runner.RunAsync(envVars, "test", "target", "-v");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Config: env-config.json");
    }

    [Fact]
    public async Task Command_Line_Overrides_Environment_Variable()
    {
        var envVars = new Dictionary<string, string>
        {
            ["TEST_CONFIG"] = "env-config.json"
        };
        var result = await Runner.RunAsync(envVars, "test", "target", "--config", "cli-config.json", "-v");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Config: cli-config.json");
    }

    [Fact]
    public async Task Multiple_Array_Values()
    {
        var result = await Runner.RunAsync("test", "target", 
            "--tags", "unit", 
            "--tags", "integration", 
            "--tags", "fast", 
            "-v");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Tags: unit, integration, fast");
    }

    [Fact]
    public async Task Array_With_Equals_Syntax()
    {
        var result = await Runner.RunAsync("test", "target", 
            "--tags=unit", 
            "--tags=integration", 
            "-v");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Tags: unit, integration");
    }

    [Fact]
    public async Task Should_Show_Subcommand_Help()
    {
        var result = await Runner.RunAsync("remote", "add", "--help");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Add a remote repository");
        CliTestAssertions.AssertOutputContains(result, "ARGUMENTS:");
    }
}