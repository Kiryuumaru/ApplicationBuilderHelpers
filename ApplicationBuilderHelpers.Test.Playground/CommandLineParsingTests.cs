using System;
using System.Linq;
using System.Threading.Tasks;
using ApplicationBuilderHelpers.Test.Playground.TestFramework;

namespace ApplicationBuilderHelpers.Test.Playground;

public class CommandLineParsingTests : TestSuiteBase
{
    public CommandLineParsingTests(CliTestRunner runner) : base(runner, "Command Line Parsing") { }

    protected override void DefineTests()
    {
        Test("Should show help when no arguments provided", async () =>
        {
            var result = await Runner.RunAsync();
            CliTestAssertions.AssertSuccess(result);
            CliTestAssertions.AssertOutputContains(result, "Use --help to see available commands");
        });

        Test("Should show version with --version flag", async () =>
        {
            var result = await Runner.RunAsync("--version");
            CliTestAssertions.AssertSuccess(result);
            CliTestAssertions.AssertOutputMatches(result, @"\d+\.\d+\.\d+");
        });

        Test("Should show version with -V flag", async () =>
        {
            var result = await Runner.RunAsync("-V");
            CliTestAssertions.AssertSuccess(result);
            CliTestAssertions.AssertOutputMatches(result, @"\d+\.\d+\.\d+");
        });

        Test("Should show help with --help flag", async () =>
        {
            var result = await Runner.RunAsync("--help");
            CliTestAssertions.AssertSuccess(result);
            CliTestAssertions.AssertOutputContains(result, "USAGE:");
            CliTestAssertions.AssertOutputContains(result, "GLOBAL OPTIONS:");
        });

        Test("Should show help with -h flag", async () =>
        {
            var result = await Runner.RunAsync("-h");
            CliTestAssertions.AssertSuccess(result);
            CliTestAssertions.AssertOutputContains(result, "USAGE:");
        });

        Test("Should show command-specific help", async () =>
        {
            var result = await Runner.RunAsync("test", "--help");
            CliTestAssertions.AssertSuccess(result);
            CliTestAssertions.AssertOutputContains(result, "Run various test operations");
            CliTestAssertions.AssertOutputContains(result, "OPTIONS:");
            CliTestAssertions.AssertOutputContains(result, "--verbose");
        });

        Test("Should handle unknown command", async () =>
        {
            var result = await Runner.RunAsync("unknowncommand");
            CliTestAssertions.AssertFailure(result);
            CliTestAssertions.AssertErrorContains(result, "No command found");
        });

        // Test all option formats
        AddTestGroup("Option Formats", () =>
        {
            Test("Long option with equals (--config=test.json)", async () =>
            {
                var result = await Runner.RunAsync("test", "target", "--config=test.json", "-v");
                CliTestAssertions.AssertSuccess(result);
                CliTestAssertions.AssertOutputContains(result, "Config: test.json");
            });

            Test("Long option with space (--config test.json)", async () =>
            {
                var result = await Runner.RunAsync("test", "target", "--config", "test.json", "-v");
                CliTestAssertions.AssertSuccess(result);
                CliTestAssertions.AssertOutputContains(result, "Config: test.json");
            });

            Test("Short option with equals (-c=test.json)", async () =>
            {
                var result = await Runner.RunAsync("test", "target", "-c=test.json", "-v");
                CliTestAssertions.AssertSuccess(result);
                CliTestAssertions.AssertOutputContains(result, "Config: test.json");
            });

            Test("Short option with space (-c test.json)", async () =>
            {
                var result = await Runner.RunAsync("test", "target", "-c", "test.json", "-v");
                CliTestAssertions.AssertSuccess(result);
                CliTestAssertions.AssertOutputContains(result, "Config: test.json");
            });
        });

        // Test boolean options
        AddTestGroup("Boolean Options", () =>
        {
            Test("Boolean flag mode (--diag)", async () =>
            {
                var result = await Runner.RunAsync("test", "target", "--diag", "-v");
                CliTestAssertions.AssertSuccess(result);
                CliTestAssertions.AssertOutputContains(result, "Diagnostic Mode: True");
            });

            Test("Boolean with true value (--diag true)", async () =>
            {
                var result = await Runner.RunAsync("test", "target", "--diag", "true", "-v");
                CliTestAssertions.AssertSuccess(result);
                CliTestAssertions.AssertOutputContains(result, "Diagnostic Mode: True");
            });

            Test("Boolean with false value (--diag false)", async () =>
            {
                var result = await Runner.RunAsync("test", "target", "--diag", "false", "-v");
                CliTestAssertions.AssertSuccess(result);
                CliTestAssertions.AssertOutputContains(result, "Diagnostic Mode: False");
            });

            Test("Boolean with equals true (--diag=true)", async () =>
            {
                var result = await Runner.RunAsync("test", "target", "--diag=true", "-v");
                CliTestAssertions.AssertSuccess(result);
                CliTestAssertions.AssertOutputContains(result, "Diagnostic Mode: True");
            });

            Test("Boolean with equals false (--diag=false)", async () =>
            {
                var result = await Runner.RunAsync("test", "target", "--diag=false", "-v");
                CliTestAssertions.AssertSuccess(result);
                CliTestAssertions.AssertOutputContains(result, "Diagnostic Mode: False");
            });

            Test("Boolean with yes value (--diag=yes)", async () =>
            {
                var result = await Runner.RunAsync("test", "target", "--diag=yes", "-v");
                CliTestAssertions.AssertSuccess(result);
                CliTestAssertions.AssertOutputContains(result, "Diagnostic Mode: True");
            });

            Test("Boolean with no value (--diag=no)", async () =>
            {
                var result = await Runner.RunAsync("test", "target", "--diag=no", "-v");
                CliTestAssertions.AssertSuccess(result);
                CliTestAssertions.AssertOutputContains(result, "Diagnostic Mode: False");
            });

            Test("Multiple boolean flags", async () =>
            {
                var result = await Runner.RunAsync("test", "target", "--verbose", "--parallel", "--coverage");
                CliTestAssertions.AssertSuccess(result);
                CliTestAssertions.AssertOutputContains(result, "Parallel: True");
                CliTestAssertions.AssertOutputContains(result, "Coverage Enabled: True");
            });
        });

        // Test environment variables
        AddTestGroup("Environment Variables", () =>
        {
            Test("Option from environment variable", async () =>
            {
                Environment.SetEnvironmentVariable("TEST_CONFIG", "env-config.json");
                try
                {
                    var result = await Runner.RunAsync("test", "target", "-v");
                    CliTestAssertions.AssertSuccess(result);
                    CliTestAssertions.AssertOutputContains(result, "Config: env-config.json");
                }
                finally
                {
                    Environment.SetEnvironmentVariable("TEST_CONFIG", null);
                }
            });

            Test("Command line overrides environment variable", async () =>
            {
                Environment.SetEnvironmentVariable("TEST_CONFIG", "env-config.json");
                try
                {
                    var result = await Runner.RunAsync("test", "target", "--config", "cli-config.json", "-v");
                    CliTestAssertions.AssertSuccess(result);
                    CliTestAssertions.AssertOutputContains(result, "Config: cli-config.json");
                }
                finally
                {
                    Environment.SetEnvironmentVariable("TEST_CONFIG", null);
                }
            });
        });

        // Test array options
        AddTestGroup("Array Options", () =>
        {
            Test("Multiple array values", async () =>
            {
                var result = await Runner.RunAsync("test", "target", 
                    "--tags", "unit", 
                    "--tags", "integration", 
                    "--tags", "fast", 
                    "-v");
                CliTestAssertions.AssertSuccess(result);
                CliTestAssertions.AssertOutputContains(result, "Tags: unit, integration, fast");
            });

            Test("Array with equals syntax", async () =>
            {
                var result = await Runner.RunAsync("test", "target", 
                    "--tags=unit", 
                    "--tags=integration", 
                    "-v");
                CliTestAssertions.AssertSuccess(result);
                CliTestAssertions.AssertOutputContains(result, "Tags: unit, integration");
            });
        });

        // Test subcommands
        AddTestGroup("Subcommands", () =>
        {
            Test("Should show subcommand help", async () =>
            {
                var result = await Runner.RunAsync("remote", "add", "--help");
                CliTestAssertions.AssertSuccess(result);
                CliTestAssertions.AssertOutputContains(result, "Add a remote repository");
                CliTestAssertions.AssertOutputContains(result, "ARGUMENTS:");
            });
        });
    }
}