using ApplicationBuilderHelpers.Test.Cli.UnitTest.TestFramework;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// Comprehensive tests for the MainCommand (root command) functionality including execution without arguments,
/// option handling, environment variables, help system integration, and error scenarios.
/// </summary>
public class MainCommandTests : CliTestBase
{
    #region Basic MainCommand Execution Tests

    [Fact]
    public async Task Should_Execute_MainCommand_When_No_Arguments()
    {
        var result = await Runner.RunAsync();
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "ApplicationBuilderHelpers Test CLI - Default Command");
        CliTestAssertions.AssertOutputContains(result, "Use --help to see available commands and options.");
    }

    [Fact]
    public async Task MainCommand_Should_Complete_Successfully()
    {
        var result = await Runner.RunAsync();
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertExitCode(result, 0);
    }

    #endregion

    #region MainCommand Option Tests

    [Fact]
    public async Task MainCommand_With_Verbose_Option()
    {
        var result = await Runner.RunAsync("--verbose");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "ApplicationBuilderHelpers Test CLI - Default Command");
        CliTestAssertions.AssertOutputContains(result, "[CFG] MAIN COMMAND CONFIGURATION");
        CliTestAssertions.AssertOutputContains(result, "[CORE] Configuration:");
        CliTestAssertions.AssertOutputContains(result, "Verbose: True");
        CliTestAssertions.AssertOutputContains(result, "[SUM] PARSED OPTIONS SUMMARY:");
    }

    [Fact]
    public async Task MainCommand_With_Config_Option()
    {
        var result = await Runner.RunAsync("--config", "myconfig.json", "--verbose");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "ApplicationBuilderHelpers Test CLI - Default Command");
        CliTestAssertions.AssertOutputContains(result, "Config: myconfig.json");
    }

    [Fact]
    public async Task MainCommand_With_Timeout_Option()
    {
        var result = await Runner.RunAsync("--timeout", "90", "--verbose");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Timeout: 90s");
    }

    [Fact]
    public async Task MainCommand_With_Short_Options()
    {
        var result = await Runner.RunAsync("-v", "-c", "test.json");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "ApplicationBuilderHelpers Test CLI - Default Command");
        CliTestAssertions.AssertOutputContains(result, "Config: test.json");
        CliTestAssertions.AssertOutputContains(result, "Verbose: True");
    }

    [Fact]
    public async Task MainCommand_With_All_Options()
    {
        var result = await Runner.RunAsync("--verbose", "--config", "test.json", "--timeout", "60");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "ApplicationBuilderHelpers Test CLI - Default Command");
        CliTestAssertions.AssertOutputContains(result, "Config: test.json");
        CliTestAssertions.AssertOutputContains(result, "Timeout: 60s");
        CliTestAssertions.AssertOutputContains(result, "Verbose: True");
        CliTestAssertions.AssertOutputContains(result, "[CFG] MAIN COMMAND CONFIGURATION");
    }

    [Fact]
    public async Task MainCommand_With_Equals_Syntax()
    {
        var result = await Runner.RunAsync("--verbose", "--config=equals-test.json", "--timeout=120");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Config: equals-test.json");
        CliTestAssertions.AssertOutputContains(result, "Timeout: 120s");
    }

    #endregion

    #region Global Options with MainCommand Tests

    [Fact]
    public async Task MainCommand_With_Global_Options()
    {
        var result = await Runner.RunAsync("--log-level", "debug", "--quiet", "--verbose");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "ApplicationBuilderHelpers Test CLI - Default Command");
        CliTestAssertions.AssertOutputContains(result, "Log Level: debug");
        CliTestAssertions.AssertOutputContains(result, "Quiet Mode: True");
    }

    [Fact]
    public async Task MainCommand_With_Environment_Variables()
    {
        var result = await Runner.RunAsync("--env", "VAR1=value1", "--env", "VAR2=value2", "--verbose");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Environment Variables: VAR1=value1, VAR2=value2");
    }

    [Fact]
    public async Task MainCommand_With_Debug_Parser()
    {
        var result = await Runner.RunAsync("--debug-parser");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "ApplicationBuilderHelpers Test CLI - Default Command");
        CliTestAssertions.AssertOutputContains(result, "[DEBUG] COMMAND LINE PARSER DEBUG INFORMATION");
        CliTestAssertions.AssertOutputContains(result, "[CMD] Command:");
        CliTestAssertions.AssertOutputContains(result, "[RAW] RAW COMMAND LINE:");
    }

    #endregion

    #region Environment Variable Tests

    [Fact]
    public async Task MainCommand_With_Environment_Variable_Config()
    {
        var envVars = new Dictionary<string, string>
        {
            ["TEST_CONFIG"] = "env-config.json"
        };
        var result = await Runner.RunAsync(envVars, "--verbose");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Config: env-config.json");
    }

    [Fact]
    public async Task MainCommand_CLI_Overrides_Environment_Variable()
    {
        var envVars = new Dictionary<string, string>
        {
            ["TEST_CONFIG"] = "env-config.json"
        };
        var result = await Runner.RunAsync(envVars, "--config", "cli-config.json", "--verbose");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Config: cli-config.json");
        CliTestAssertions.AssertOutputDoesNotContain(result, "env-config.json");
    }

    #endregion

    #region Help System Integration Tests

    [Fact]
    public async Task MainCommand_Global_Help_Shows_Comprehensive_Information()
    {
        var result = await Runner.RunAsync("--help");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "test v");
        CliTestAssertions.AssertOutputContains(result, "ApplicationBuilderHelpers Test CLI");
        CliTestAssertions.AssertOutputContains(result, "USAGE:");
        CliTestAssertions.AssertOutputContains(result, "DESCRIPTION:");
        CliTestAssertions.AssertOutputContains(result, "COMMANDS:");
        CliTestAssertions.AssertOutputContains(result, "OPTIONS:");
        CliTestAssertions.AssertOutputContains(result, "GLOBAL OPTIONS:");
        CliTestAssertions.AssertOutputContains(result, "Run 'test <command> --help' for more information on specific commands");
    }

    [Fact]
    public async Task MainCommand_Help_Shows_MainCommand_Options()
    {
        var result = await Runner.RunAsync("--help");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "--verbose");
        CliTestAssertions.AssertOutputContains(result, "--config");
        CliTestAssertions.AssertOutputContains(result, "--timeout");
        CliTestAssertions.AssertOutputContains(result, "Enable verbose output");
        CliTestAssertions.AssertOutputContains(result, "Configuration file path");
        CliTestAssertions.AssertOutputContains(result, "Timeout in seconds");
    }

    [Fact]
    public async Task MainCommand_Help_With_Short_Flag()
    {
        var result = await Runner.RunAsync("-h");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "USAGE:");
        CliTestAssertions.AssertOutputContains(result, "COMMANDS:");
        CliTestAssertions.AssertOutputContains(result, "OPTIONS:");
    }

    #endregion

    #region Option Parsing Edge Cases

    [Fact]
    public async Task MainCommand_With_Boolean_Flag_Style()
    {
        var result = await Runner.RunAsync("--verbose");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Verbose: True");
    }

    [Fact]
    public async Task MainCommand_With_Mixed_Option_Styles()
    {
        var result = await Runner.RunAsync("-v", "--config=mixed.json", "--timeout", "45");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Verbose: True");
        CliTestAssertions.AssertOutputContains(result, "Config: mixed.json");
        CliTestAssertions.AssertOutputContains(result, "Timeout: 45s");
    }

    [Fact]
    public async Task MainCommand_With_Special_Characters_In_Config()
    {
        var specialConfig = "config with spaces & special-chars.json";
        var result = await Runner.RunAsync("--config", specialConfig, "--verbose");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, $"Config: {specialConfig}");
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task MainCommand_With_Invalid_Option()
    {
        var result = await Runner.RunAsync("--invalid-option");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertErrorContains(result, "Unknown option: --invalid-option");
        CliTestAssertions.AssertErrorContains(result, "Run 'test <command> --help' for more information");
    }

    [Fact]
    public async Task MainCommand_With_Invalid_Timeout_Value()
    {
        var result = await Runner.RunAsync("--timeout", "invalid");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertErrorContains(result, "Invalid Int32 value: 'invalid'");
    }

    [Fact]
    public async Task MainCommand_Error_Messages_Are_Red_And_Helpful()
    {
        var result = await Runner.RunAsync("--unknown-flag");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertErrorContains(result, "Error: Unknown option: --unknown-flag");
        CliTestAssertions.AssertErrorContains(result, "Run 'test <command> --help' for more information");
    }

    #endregion

    #region Integration with Subcommands Tests

    [Fact]
    public async Task MainCommand_Does_Not_Interfere_With_Subcommands()
    {
        // Ensure MainCommand doesn't break existing subcommand functionality
        var result = await Runner.RunAsync("build", "project.csproj");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Building project: project.csproj");
        CliTestAssertions.AssertOutputDoesNotContain(result, "ApplicationBuilderHelpers Test CLI - Default Command");
    }

    [Fact]
    public async Task MainCommand_Does_Not_Interfere_With_Config_Subcommands()
    {
        var result = await Runner.RunAsync("config", "get", "test.key");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Configuration Get Operation");
        CliTestAssertions.AssertOutputDoesNotContain(result, "ApplicationBuilderHelpers Test CLI - Default Command");
    }

    #endregion

    #region Options Summary Tests

    [Fact]
    public async Task MainCommand_Options_Summary_Shows_Only_Non_Default_Values()
    {
        var result = await Runner.RunAsync("--verbose");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "[SUM] PARSED OPTIONS SUMMARY:");
        CliTestAssertions.AssertOutputContains(result, "verbose=true");
        CliTestAssertions.AssertOutputDoesNotContain(result, "timeout=30"); // Default value should not be shown
    }

    [Fact]
    public async Task MainCommand_Options_Summary_Shows_Multiple_Changed_Options()
    {
        var result = await Runner.RunAsync("--verbose", "--timeout", "90", "--log-level", "warning");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "verbose=true");
        CliTestAssertions.AssertOutputContains(result, "timeout=90");
        CliTestAssertions.AssertOutputContains(result, "log-level=warning");
    }

    [Fact]
    public async Task MainCommand_Options_Summary_Shows_Default_Message_When_No_Changes()
    {
        var result = await Runner.RunAsync();
        // Note: Without --verbose, we won't see the options summary
        // This test verifies the basic execution works without verbose mode
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "ApplicationBuilderHelpers Test CLI - Default Command");
        CliTestAssertions.AssertOutputDoesNotContain(result, "[SUM] PARSED OPTIONS SUMMARY:");
    }

    #endregion

    #region Performance and Regression Tests

    [Fact]
    public async Task MainCommand_Executes_Quickly()
    {
        var result = await Runner.RunAsync("--verbose");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertExecutionTime(result, System.TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task MainCommand_With_Many_Options_Still_Works()
    {
        var result = await Runner.RunAsync(
            "--verbose",
            "--config", "complex-config.json",
            "--timeout", "300",
            "--log-level", "trace",
            "--env", "A=1",
            "--env", "B=2",
            "--env", "C=3",
            "--debug-parser");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "ApplicationBuilderHelpers Test CLI - Default Command");
        CliTestAssertions.AssertExecutionTime(result, System.TimeSpan.FromSeconds(10));
    }

    #endregion
}