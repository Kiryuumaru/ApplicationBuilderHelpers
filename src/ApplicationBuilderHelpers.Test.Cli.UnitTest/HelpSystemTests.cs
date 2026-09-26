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
        CliTestAssertions.AssertOutputContains(result, "-V, --version");
        CliTestAssertions.AssertOutputContains(result, "Show version information");
        CliTestAssertions.AssertOutputContains(result, "Run 'test <command> --help' for more information on specific commands");
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
        CliTestAssertions.AssertOutputContains(result, "False");
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
        CliTestAssertions.AssertOutputContains(result, "-V, --version");
        CliTestAssertions.AssertOutputContains(result, "Show version information");
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
        var result = await Runner.RunAsync("config", "unknown", "--help");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "'config' requires a subcommand");
        CliTestAssertions.AssertErrorContains(result, "Available subcommands: get, set");
        CliTestAssertions.AssertErrorContains(result, "Run 'test config --version' to show version information.");
    }

    [Fact]
    public async Task Near_Miss_Database_With_Help_Errors_Requires_Subcommand()
    {
        var result = await Runner.RunAsync("database", "migrat", "--help");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "'database' requires a subcommand");
        CliTestAssertions.AssertErrorContains(result, "Available subcommands: migrate");
        CliTestAssertions.AssertErrorContains(result, "Did you mean 'migrate'?");
        CliTestAssertions.AssertErrorContains(result, "Run 'test database --version' to show version information.");
        CliTestAssertions.AssertOutputDoesNotContain(result, "USAGE:");
        Assert.DoesNotContain("Run 'test database --help'", result.StandardError);
    }

    [Fact]
    public async Task Near_Miss_Remote_With_Help_Errors_Requires_Subcommand()
    {
        var result = await Runner.RunAsync("remote", "ad", "--help");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "'remote' requires a subcommand");
        CliTestAssertions.AssertErrorContains(result, "Available subcommands: add");
        CliTestAssertions.AssertErrorContains(result, "Did you mean 'add'?");
        CliTestAssertions.AssertErrorContains(result, "Run 'test remote --version' to show version information.");
        CliTestAssertions.AssertOutputDoesNotContain(result, "USAGE:");
        Assert.DoesNotContain("Run 'test remote --help'", result.StandardError);
    }

    [Fact]
    public async Task Near_Miss_With_Short_Help_Errors_Requires_Subcommand()
    {
        var result = await Runner.RunAsync("config", "sett", "-h");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "'config' requires a subcommand");
        CliTestAssertions.AssertErrorContains(result, "Available subcommands: get, set");
        CliTestAssertions.AssertErrorContains(result, "Did you mean 'set'?");
        CliTestAssertions.AssertErrorContains(result, "Run 'test config --version' to show version information.");
        CliTestAssertions.AssertOutputDoesNotContain(result, "USAGE:");
        Assert.DoesNotContain("Run 'test config --help'", result.StandardError);
    }

    [Fact]
    public async Task Separator_Before_Surplus_With_Help_Keeps_Requires_Subcommand()
    {
        var result = await Runner.RunAsync("config", "--", "gett", "--help");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "'config' requires a subcommand");
        CliTestAssertions.AssertErrorContains(result, "Available subcommands: get, set");
        CliTestAssertions.AssertErrorContains(result, "Run 'test config --help' to see available subcommands and options.");
        CliTestAssertions.AssertErrorContains(result, "Run 'test config --version' to show version information.");
        CliTestAssertions.AssertOutputDoesNotContain(result, "USAGE:");
        Assert.DoesNotContain("Did you mean", result.StandardError);
    }

    [Fact]
    public async Task Surplus_Before_Separator_With_Help_Keeps_Requires_Subcommand()
    {
        var result = await Runner.RunAsync("config", "gett", "--", "--help");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "'config' requires a subcommand");
        CliTestAssertions.AssertErrorContains(result, "Available subcommands: get, set");
        CliTestAssertions.AssertErrorContains(result, "Did you mean 'get'?");
        CliTestAssertions.AssertErrorContains(result, "Run 'test config --help' to see available subcommands and options.");
        CliTestAssertions.AssertErrorContains(result, "Run 'test config --version' to show version information.");
        CliTestAssertions.AssertOutputDoesNotContain(result, "USAGE:");
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
        var result = await Runner.RunAsync("test", "target", "--verbose", "--help");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Run various test operations");
        CliTestAssertions.AssertOutputDoesNotContain(result, "Running test on target");
    }
}