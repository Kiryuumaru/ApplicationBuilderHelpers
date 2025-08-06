using System.Threading.Tasks;
using ApplicationBuilderHelpers.Test.Cli.UnitTest.TestFramework;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// Tests for all CLI commands including build, serve, deploy, test, config, database, remote, and plugin commands
/// </summary>
public class AllCommandsTests : CliTestBase
{
    #region Root Command Tests

    [Fact]
    public async Task Should_Show_Help_When_No_Arguments()
    {
        var result = await Runner.RunAsync();
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Run 'test <command> --help' for more information on specific commands");
    }

    [Fact]
    public async Task Should_Show_Version()
    {
        var result = await Runner.RunAsync("--version");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputMatches(result, @"\d+\.\d+\.\d+");
    }

    [Fact]
    public async Task Should_Show_Short_Version()
    {
        var result = await Runner.RunAsync("-V");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputMatches(result, @"\d+\.\d+\.\d+");
    }

    #endregion

    #region Build Command Tests

    [Fact]
    public async Task Basic_Build_Command()
    {
        var result = await Runner.RunAsync("build", "MyProject.csproj");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Building project: MyProject.csproj");
        CliTestAssertions.AssertOutputContains(result, "Target: Debug");
        CliTestAssertions.AssertOutputContains(result, "Build completed successfully!");
    }

    [Fact]
    public async Task Build_With_Release_Flag()
    {
        var result = await Runner.RunAsync("build", "MyProject.csproj", "--release");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Building project: MyProject.csproj");
        CliTestAssertions.AssertOutputContains(result, "Release Mode: True");
        CliTestAssertions.AssertOutputContains(result, "Build completed successfully!");
    }

    [Fact]
    public async Task Build_With_Custom_Output()
    {
        var result = await Runner.RunAsync("build", "MyProject.csproj", "--output", "CustomOutput");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Building project: MyProject.csproj");
        CliTestAssertions.AssertOutputContains(result, "Output: CustomOutput");
    }

    [Fact]
    public async Task Build_With_Target_And_Framework()
    {
        var result = await Runner.RunAsync("build", "MyProject.csproj", "--target=Release", "--framework=net8.0");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Target: Release");
        CliTestAssertions.AssertOutputContains(result, "Framework: net8.0");
    }

    [Fact]
    public async Task Build_With_Defines()
    {
        var result = await Runner.RunAsync("build", "MyProject.csproj", "--define", "DEBUG", "--define", "TRACE");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Defines: DEBUG, TRACE");
    }

    [Fact]
    public async Task Build_With_All_Options()
    {
        var result = await Runner.RunAsync("build", "MyProject.csproj",
            "--release",
            "--output", "bin/Release",
            "--target=Release",
            "--framework=net9.0",
            "--arch=x64",
            "--restore",
            "--verbosity=detailed");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Release Mode: True");
        CliTestAssertions.AssertOutputContains(result, "Output: bin/Release");
        CliTestAssertions.AssertOutputContains(result, "Target: Release");
        CliTestAssertions.AssertOutputContains(result, "Framework: net9.0");
        CliTestAssertions.AssertOutputContains(result, "Architecture: x64");
        CliTestAssertions.AssertOutputContains(result, "Restore: True");
        CliTestAssertions.AssertOutputContains(result, "Verbosity: detailed");
    }

    [Fact]
    public async Task Build_Help()
    {
        var result = await Runner.RunAsync("build", "--help");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Build the project");
    }

    #endregion

    #region Test Command Tests

    [Fact]
    public async Task Test_Basic()
    {
        var result = await Runner.RunAsync("test", "MyTarget");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Running test on target: MyTarget");
        CliTestAssertions.AssertOutputContains(result, "[SUM] PARSED OPTIONS SUMMARY:");
    }

    [Fact]
    public async Task Test_With_Verbose_Output()
    {
        var result = await Runner.RunAsync("test", "MyTarget", "--verbose");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Running test on target: MyTarget");
        CliTestAssertions.AssertOutputContains(result, "[CORE] Core Configuration:");
        CliTestAssertions.AssertOutputContains(result, "[SUM] PARSED OPTIONS SUMMARY:");
    }

    [Fact]
    public async Task Test_With_Comprehensive_Options()
    {
        var result = await Runner.RunAsync("test", "ComprehensiveTarget",
            "--verbose",
            "--config", "test.config",
            "--timeout=180",
            "--parallel",
            "--coverage",
            "--coverage-threshold=90",
            "--output-format=xml",
            "--framework=net8.0",
            "--tags", "unit",
            "--tags", "integration");

        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Running test on target: ComprehensiveTarget");
        CliTestAssertions.AssertOutputContains(result, "Config: test.config");
        CliTestAssertions.AssertOutputContains(result, "Timeout: 180s");
        CliTestAssertions.AssertOutputContains(result, "Parallel: True");
        CliTestAssertions.AssertOutputContains(result, "Coverage Enabled: True");
        CliTestAssertions.AssertOutputContains(result, "Coverage Threshold: 90%");
        CliTestAssertions.AssertOutputContains(result, "Output Format: xml");
        CliTestAssertions.AssertOutputContains(result, "Framework: net8.0");
        CliTestAssertions.AssertOutputContains(result, "Tags: unit, integration");
    }

    #endregion

    #region Config Command Tests

    [Fact]
    public async Task Config_Requires_Subcommand()
    {
        var result = await Runner.RunAsync("config");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertErrorContains(result, "'config' requires a subcommand");
    }

    [Fact]
    public async Task Config_Get_With_Key()
    {
        var result = await Runner.RunAsync("config", "get", "database.connection");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Getting configuration for key: database.connection");
    }

    [Fact]
    public async Task Config_Get_All()
    {
        var result = await Runner.RunAsync("config", "get", "--all");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Configuration Get Operation");
    }

    [Fact]
    public async Task Config_Get_With_Filters()
    {
        // Use --section instead of --filter which doesn't exist
        var result = await Runner.RunAsync("config", "get", "--section", "database");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Configuration Get Operation");
    }

    [Fact]
    public async Task Config_Set_Basic()
    {
        var result = await Runner.RunAsync("config", "set", "api.timeout", "30");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Setting config api.timeout = 30");
    }

    [Fact]
    public async Task Config_Set_With_Options()
    {
        var result = await Runner.RunAsync("config", "set", "debug.enabled", "true", "--force", "--format=json");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Setting config debug.enabled = true");
        CliTestAssertions.AssertOutputContains(result, "Force: True");
        CliTestAssertions.AssertOutputContains(result, "Output Format: json");
    }

    [Fact]
    public async Task Config_Help()
    {
        var result = await Runner.RunAsync("config", "--help");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Configuration values");
    }

    #endregion

    #region Global Options Tests

    [Fact]
    public async Task Debug_Parser_Option()
    {
        var result = await Runner.RunAsync("test", "MyTarget", "--debug-parser");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "[DEBUG] COMMAND LINE PARSER DEBUG INFORMATION");
        CliTestAssertions.AssertOutputContains(result, "[CMD] Command:");
        CliTestAssertions.AssertOutputContains(result, "[RAW] RAW COMMAND LINE:");
    }

    [Fact]
    public async Task Quiet_Mode()
    {
        var result = await Runner.RunAsync("build", "MyProject.csproj", "--quiet");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Building project: MyProject.csproj");
        CliTestAssertions.AssertOutputDoesNotContain(result, "Build completed successfully!");
    }

    [Fact]
    public async Task Log_Level()
    {
        var result = await Runner.RunAsync("test", "MyTarget", "--log-level", "debug", "--debug-parser");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Running test on target: MyTarget");
        CliTestAssertions.AssertOutputContains(result, "[DEBUG] COMMAND LINE PARSER DEBUG INFORMATION");
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task Invalid_Command()
    {
        var result = await Runner.RunAsync("invalid-command");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertErrorContains(result, "No command found");
    }

    [Fact]
    public async Task Missing_Required_Argument()
    {
        var result = await Runner.RunAsync("build");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertErrorContains(result, "Missing required argument");
    }

    [Fact]
    public async Task Invalid_Option_Value()
    {
        var result = await Runner.RunAsync("build", "MyProject.csproj", "--target", "InvalidTarget");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertErrorContains(result, "not valid for option '--target'");
        CliTestAssertions.AssertErrorContains(result, "Must be one of:");
    }

    [Fact]
    public async Task Unknown_Option()
    {
        var result = await Runner.RunAsync("build", "MyProject.csproj", "--unknown-option");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertErrorContains(result, "Unknown option: --unknown-option");
    }

    #endregion
}