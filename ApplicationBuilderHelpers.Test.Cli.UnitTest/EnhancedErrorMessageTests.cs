using ApplicationBuilderHelpers.Test.Cli.UnitTest.TestFramework;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// Tests for enhanced error messages including colored output, contextual help suggestions,
/// and improved user experience in error scenarios.
/// </summary>
public class EnhancedErrorMessageTests : CliTestBase
{
    #region Error Message Formatting Tests

    [Fact]
    public async Task Error_Messages_Should_Include_Help_Footer()
    {
        var result = await Runner.RunAsync("--unknown-option");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertErrorContains(result, "Error: Unknown option: --unknown-option");
        CliTestAssertions.AssertErrorContains(result, "Run 'test <command> --help' for more information on specific command options.");
    }

    [Fact]
    public async Task Subcommand_Requirement_Error_Should_Include_Contextual_Help()
    {
        var result = await Runner.RunAsync("config");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertErrorContains(result, "Error: 'config' requires a subcommand");
        CliTestAssertions.AssertErrorContains(result, "Run 'test config --help' to see available subcommands and options.");
    }

    [Fact]
    public async Task Root_Subcommand_Requirement_Error_Should_Show_Global_Help_Suggestion()
    {
        var result = await Runner.RunAsync("invalid-command");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertErrorContains(result, "No command found");
        CliTestAssertions.AssertErrorContains(result, "more information");
    }

    [Fact]
    public async Task Missing_Required_Option_Error_Should_Include_Command_Help_Suggestion()
    {
        var result = await Runner.RunAsync("build");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertErrorContains(result, "Error: Missing required argument");
        CliTestAssertions.AssertErrorContains(result, "Run 'test <command> --help' for more information on specific command options.");
    }

    [Fact]
    public async Task Invalid_Option_Value_Error_Should_Include_Command_Help_Suggestion()
    {
        var result = await Runner.RunAsync("build", "project.csproj", "--target", "InvalidTarget");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertErrorContains(result, "Error:");
        CliTestAssertions.AssertErrorContains(result, "not valid for option '--target'");
        CliTestAssertions.AssertErrorContains(result, "Run 'test --help'");
    }

    #endregion

    #region Contextual Help Message Tests

    [Fact]
    public async Task Database_Subcommand_Error_Should_Show_Specific_Help()
    {
        var result = await Runner.RunAsync("database");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertErrorContains(result, "'database' requires a subcommand");
        CliTestAssertions.AssertErrorContains(result, "Run 'test database --help'");
    }

    [Fact]
    public async Task Remote_Subcommand_Error_Should_Show_Specific_Help()
    {
        var result = await Runner.RunAsync("remote");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertErrorContains(result, "'remote' requires a subcommand");
        CliTestAssertions.AssertErrorContains(result, "Run 'test remote --help'");
    }

    [Fact]
    public async Task Plugin_Subcommand_Error_Should_Show_Specific_Help()
    {
        var result = await Runner.RunAsync("plugin");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertErrorContains(result, "Missing required argument: action");
        CliTestAssertions.AssertErrorContains(result, "Run 'test <command> --help' for more information on specific command options.");
    }

    #endregion

    #region Error Message Content Tests

    [Fact]
    public async Task Error_Messages_Should_Start_With_Error_Prefix()
    {
        var result = await Runner.RunAsync("--invalid");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertErrorContains(result, "Error: ");
    }

    [Fact]
    public async Task Error_Messages_Should_Be_User_Friendly()
    {
        var result = await Runner.RunAsync("build", "project.csproj", "--unknown-flag");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertErrorContains(result, "Unknown option: --unknown-flag");
        // Error messages should not contain technical details
        Assert.DoesNotContain("Exception", result.StandardError);
        Assert.DoesNotContain("Stack trace", result.StandardError);
    }

    [Fact]
    public async Task Error_Messages_Should_Include_Available_Options_When_Relevant()
    {
        var result = await Runner.RunAsync("config", "get", "--invalid-option");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertErrorContains(result, "Unknown option: --invalid-option");
        CliTestAssertions.AssertErrorContains(result, "Run 'test <command> --help' for more information");
    }

    #endregion

    #region Validation Error Tests

    [Fact]
    public async Task Type_Validation_Error_Should_Be_Clear()
    {
        var result = await Runner.RunAsync("test", "target", "--timeout=invalid");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertErrorContains(result, "Invalid Int32 value: 'invalid'");
        CliTestAssertions.AssertErrorContains(result, "Run 'test --help'");
    }

    [Fact]
    public async Task FromAmong_Validation_Error_Should_Show_Valid_Options()
    {
        var result = await Runner.RunAsync("test", "target", "--output-format=invalid");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertErrorContains(result, "Value 'invalid' is not valid for option '--output-format'");
        CliTestAssertions.AssertErrorContains(result, "Must be one of:");
        CliTestAssertions.AssertErrorContains(result, "json, xml, junit, console");
    }

    [Fact]
    public async Task Framework_Validation_Error_Should_Show_Supported_Frameworks()
    {
        var result = await Runner.RunAsync("build", "project.csproj", "--framework=netfx4.8");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertErrorContains(result, "Value 'netfx4.8' is not valid for option '--framework'");
        CliTestAssertions.AssertErrorContains(result, "Must be one of:");
        CliTestAssertions.AssertErrorContains(result, "net6.0, net7.0, net8.0, net9.0");
    }

    #endregion

    #region Error Recovery and Help Integration Tests

    [Fact]
    public async Task Error_Should_Not_Prevent_Help_From_Working()
    {
        // Even if we have an error scenario, help should still work
        var helpResult = await Runner.RunAsync("--help");
        CliTestAssertions.AssertSuccess(helpResult);
        CliTestAssertions.AssertOutputContains(helpResult, "USAGE:");
        CliTestAssertions.AssertOutputContains(helpResult, "COMMANDS:");
    }

    [Fact]
    public async Task Error_Should_Not_Prevent_Version_From_Working()
    {
        // Even if we have an error scenario, version should still work
        var versionResult = await Runner.RunAsync("--version");
        CliTestAssertions.AssertSuccess(versionResult);
        CliTestAssertions.AssertOutputMatches(versionResult, @"\d+\.\d+\.\d+");
    }

    [Fact]
    public async Task Command_Specific_Help_Should_Work_After_Error()
    {
        // Test that command-specific help works even if the command itself would error
        var helpResult = await Runner.RunAsync("build", "--help");
        CliTestAssertions.AssertSuccess(helpResult);
        CliTestAssertions.AssertOutputContains(helpResult, "Build the project");
        CliTestAssertions.AssertOutputContains(helpResult, "ARGUMENTS:");
        CliTestAssertions.AssertOutputContains(helpResult, "project");
    }

    #endregion

    #region Multiple Error Scenarios Tests

    [Fact]
    public async Task Multiple_Invalid_Options_Should_Report_First_Error()
    {
        var result = await Runner.RunAsync("test", "target", "--invalid1", "--invalid2");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertErrorContains(result, "Unknown option: --invalid1");
        // Should not continue to process --invalid2 after first error
    }

    [Fact]
    public async Task Invalid_Option_Before_Valid_Command_Should_Error()
    {
        var result = await Runner.RunAsync("--invalid-global", "build", "project.csproj");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertErrorContains(result, "Unknown option: --invalid-global");
    }

    #endregion

    #region Error Message Consistency Tests

    [Fact]
    public async Task All_Error_Messages_Should_Have_Consistent_Format()
    {
        var errorScenarios = new[]
        {
            ("--unknown-option", "Unknown option"),
            ("build", "Missing required argument"),
            ("config", "requires a subcommand")
        };

        foreach (var (args, expectedError) in errorScenarios)
        {
            var result = await Runner.RunAsync(args.Split(' '));
            CliTestAssertions.AssertFailure(result);
            CliTestAssertions.AssertErrorContains(result, "Error:");
            CliTestAssertions.AssertErrorContains(result, expectedError);
            CliTestAssertions.AssertErrorContains(result, "Run 'test");
        }
    }

    [Fact]
    public async Task Error_Messages_Should_End_With_Help_Suggestion()
    {
        var result = await Runner.RunAsync("--invalid");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertErrorContains(result, "Run 'test <command> --help' for more information");
    }

    #endregion

    #region Performance Tests for Error Handling

    [Fact]
    public async Task Error_Messages_Should_Be_Generated_Quickly()
    {
        var result = await Runner.RunAsync("--unknown-option");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExecutionTime(result, System.TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task Complex_Error_Scenarios_Should_Still_Be_Fast()
    {
        var result = await Runner.RunAsync("unknown-command", "with", "many", "arguments", "--and", "--options");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExecutionTime(result, System.TimeSpan.FromSeconds(3));
    }

    #endregion
}