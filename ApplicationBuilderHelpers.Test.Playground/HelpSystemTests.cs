using System;
using System.Threading.Tasks;
using ApplicationBuilderHelpers.Test.Playground.TestFramework;

namespace ApplicationBuilderHelpers.Test.Playground;

public class HelpSystemTests : TestSuiteBase
{
    public HelpSystemTests(CliTestRunner runner) : base(runner, "Help System") { }

    protected override void DefineTests()
    {
        AddTestGroup("Help Display", () =>
        {
            Test("Global help shows proper formatting", async () =>
            {
                var result = await Runner.RunAsync("--help");
                CliTestAssertions.AssertSuccess(result);
                CliTestAssertions.AssertOutputContains(result, "USAGE:");
                CliTestAssertions.AssertOutputContains(result, "GLOBAL OPTIONS:");
                CliTestAssertions.AssertOutputContains(result, "COMMANDS:");
                
                // Check for proper two-column formatting
                CliTestAssertions.AssertOutputContains(result, "--log-level");
                CliTestAssertions.AssertOutputContains(result, "Set the logging level");
            });

            Test("Command help shows command-specific options", async () =>
            {
                var result = await Runner.RunAsync("test", "--help");
                CliTestAssertions.AssertSuccess(result);
                CliTestAssertions.AssertOutputContains(result, "Run various test operations");
                CliTestAssertions.AssertOutputContains(result, "OPTIONS:");
                CliTestAssertions.AssertOutputContains(result, "--output-format");
                CliTestAssertions.AssertOutputContains(result, "Possible values:");
                CliTestAssertions.AssertOutputContains(result, "json, xml, junit, console");
            });

            Test("Help shows default values", async () =>
            {
                var result = await Runner.RunAsync("test", "--help");
                CliTestAssertions.AssertSuccess(result);
                CliTestAssertions.AssertOutputContains(result, "Default:");
                CliTestAssertions.AssertOutputContains(result, "console"); // Default output format
            });

            Test("Help shows environment variables", async () =>
            {
                var result = await Runner.RunAsync("test", "--help");
                CliTestAssertions.AssertSuccess(result);
                CliTestAssertions.AssertOutputContains(result, "Environment variable:");
                CliTestAssertions.AssertOutputContains(result, "TEST_CONFIG");
            });
        });

        AddTestGroup("Subcommand Help", () =>
        {
            Test("Build command help", async () =>
            {
                var result = await Runner.RunAsync("build", "--help");
                CliTestAssertions.AssertSuccess(result);
                CliTestAssertions.AssertOutputContains(result, "Build the project");
            });

            Test("Remote add subcommand help", async () =>
            {
                var result = await Runner.RunAsync("remote", "add", "--help");
                CliTestAssertions.AssertSuccess(result);
                CliTestAssertions.AssertOutputContains(result, "Add a new remote repository");
                CliTestAssertions.AssertOutputContains(result, "ARGUMENTS:");
            });

            Test("Config commands help", async () =>
            {
                var result = await Runner.RunAsync("config", "--help");
                CliTestAssertions.AssertSuccess(result);
                // Should show available config subcommands
            });
        });

        AddTestGroup("Version Information", () =>
        {
            Test("Version shows proper format", async () =>
            {
                var result = await Runner.RunAsync("--version");
                CliTestAssertions.AssertSuccess(result);
                CliTestAssertions.AssertOutputMatches(result, @"\d+\.\d+\.\d+");
            });

            Test("Short version flag works", async () =>
            {
                var result = await Runner.RunAsync("-V");
                CliTestAssertions.AssertSuccess(result);
                CliTestAssertions.AssertOutputMatches(result, @"\d+\.\d+\.\d+");
            });
        });

        AddTestGroup("Error Messages", () =>
        {
            Test("Unknown command shows helpful error", async () =>
            {
                var result = await Runner.RunAsync("nonexistent-command");
                CliTestAssertions.AssertFailure(result);
                CliTestAssertions.AssertErrorContains(result, "No command found");
            });

            Test("Invalid option value shows constraint info", async () =>
            {
                var result = await Runner.RunAsync("test", "target", "--output-format=invalid");
                CliTestAssertions.AssertFailure(result);
                CliTestAssertions.AssertErrorContains(result, "Must be one of:");
                CliTestAssertions.AssertErrorContains(result, "json, xml, junit, console");
            });
        });
    }
}