using System.Linq;
using System.Threading.Tasks;
using ApplicationBuilderHelpers.Test.Cli.UnitTest.TestFramework;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// Tests for the help system including command help, option descriptions, and formatting
/// </summary>
public class HelpSystemTests : CliTestBase
{
    [Fact]
    public async Task Root_Help_Shows_Usage()
    {
        var result = await Runner.RunAsync("--help");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "USAGE:");
        CliTestAssertions.AssertOutputContains(result, "GLOBAL OPTIONS:");
        CliTestAssertions.AssertOutputContains(result, "Run 'test <command> --help' for more information");
    }

    [Fact]
    public async Task Command_Help_Shows_Options()
    {
        var result = await Runner.RunAsync("test", "--help");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Run various test operations");
        CliTestAssertions.AssertOutputContains(result, "OPTIONS:");
        CliTestAssertions.AssertOutputContains(result, "--verbose");
        CliTestAssertions.AssertOutputContains(result, "--config");
        CliTestAssertions.AssertOutputContains(result, "--timeout");
    }

    [Fact]
    public async Task Build_Command_Help()
    {
        var result = await Runner.RunAsync("build", "--help");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Build the project");
        CliTestAssertions.AssertOutputContains(result, "ARGUMENTS:");
        CliTestAssertions.AssertOutputContains(result, "project");
    }

    [Fact]
    public async Task Config_Command_Help()
    {
        var result = await Runner.RunAsync("config", "--help");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Configuration values");
        // The config command shows OPTIONS instead of COMMANDS
        CliTestAssertions.AssertOutputContains(result, "OPTIONS:");
        CliTestAssertions.AssertOutputContains(result, "--format");
    }

    [Fact]
    public async Task Config_Get_Help()
    {
        var result = await Runner.RunAsync("config", "get", "--help");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Get configuration values");
        CliTestAssertions.AssertOutputContains(result, "OPTIONS:");
        CliTestAssertions.AssertOutputContains(result, "--all");
    }

    [Fact]
    public async Task Config_Set_Help()
    {
        var result = await Runner.RunAsync("config", "set", "--help");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Set configuration values");
        CliTestAssertions.AssertOutputContains(result, "ARGUMENTS:");
        CliTestAssertions.AssertOutputContains(result, "key");
        CliTestAssertions.AssertOutputContains(result, "value");
    }

    [Fact]
    public async Task Help_Shows_Default_Values()
    {
        var result = await Runner.RunAsync("test", "--help");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Default:");
        CliTestAssertions.AssertOutputContains(result, "False"); // For boolean options
    }

    [Fact]
    public async Task Help_Shows_Possible_Values()
    {
        var result = await Runner.RunAsync("test", "--help");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Possible values:");
        CliTestAssertions.AssertOutputContains(result, "json, xml, junit, console");
    }

    [Fact]
    public async Task Help_Shows_Option_Descriptions()
    {
        var result = await Runner.RunAsync("test", "--help");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Enable verbose output");
        CliTestAssertions.AssertOutputContains(result, "Configuration file path");
        CliTestAssertions.AssertOutputContains(result, "Timeout in seconds");
    }

    [Fact]
    public async Task Help_Shows_Short_Options()
    {
        var result = await Runner.RunAsync("test", "--help");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "-v, --verbose");
        CliTestAssertions.AssertOutputContains(result, "-c, --config");
        CliTestAssertions.AssertOutputContains(result, "-t, --tags");
    }

    [Fact]
    public async Task Global_Options_In_Command_Help()
    {
        var result = await Runner.RunAsync("test", "--help");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "GLOBAL OPTIONS:");
        CliTestAssertions.AssertOutputContains(result, "--log-level");
        CliTestAssertions.AssertOutputContains(result, "--quiet");
        CliTestAssertions.AssertOutputContains(result, "--debug-parser");
    }

    [Fact]
    public async Task Help_Formatting_Is_Consistent()
    {
        var commands = new[] { "test", "build", "config get", "config set" };
        
        foreach (var command in commands)
        {
            var args = command.Split(' ').Append("--help").ToArray();
            var result = await Runner.RunAsync(args);
            CliTestAssertions.AssertSuccess(result, $"Help should work for command: {command}");
            CliTestAssertions.AssertOutputContains(result, "test v", $"Version should be shown for: {command}");
        }
    }

    [Fact]
    public async Task Help_For_Unknown_Subcommand()
    {
        // Unknown subcommands just show the parent command help, so this should succeed
        var result = await Runner.RunAsync("config", "unknown", "--help");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Configuration values");
    }

    [Fact]
    public async Task Short_Help_Flag()
    {
        var result = await Runner.RunAsync("test", "-h");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Run various test operations");
        CliTestAssertions.AssertOutputContains(result, "OPTIONS:");
    }

    [Fact]
    public async Task Help_Option_Priority()
    {
        // Help should be shown even with other options
        var result = await Runner.RunAsync("test", "target", "--verbose", "--help");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Run various test operations");
        CliTestAssertions.AssertOutputDoesNotContain(result, "Running test on target");
    }
}