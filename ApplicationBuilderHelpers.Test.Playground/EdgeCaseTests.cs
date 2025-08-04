using System;
using System.Threading.Tasks;
using ApplicationBuilderHelpers.Test.Playground.TestFramework;

namespace ApplicationBuilderHelpers.Test.Playground;

public class EdgeCaseTests : TestSuiteBase
{
    public EdgeCaseTests(CliTestRunner runner) : base(runner, "Edge Cases") { }

    protected override void DefineTests()
    {
        Test("Empty string as option value", async () =>
        {
            var result = await Runner.RunAsync("test", "target", "--config=", "-v");
            CliTestAssertions.AssertSuccess(result);
            CliTestAssertions.AssertOutputContains(result, "Config: ");
        });

        Test("Unicode in arguments", async () =>
        {
            var result = await Runner.RunAsync("test", "🎯", "-v");
            CliTestAssertions.AssertSuccess(result);
            CliTestAssertions.AssertOutputContains(result, "Running test on target: 🎯");
        });

        Test("Special characters in option values", async () =>
        {
            var result = await Runner.RunAsync("test", "target", 
                "--config=config with spaces.json",
                "--filter=name='test case'",
                "-v");
            CliTestAssertions.AssertSuccess(result);
            CliTestAssertions.AssertOutputContains(result, "Config: config with spaces.json");
            CliTestAssertions.AssertOutputContains(result, "Filter: name='test case'");
        });

        Test("Option with equals in value", async () =>
        {
            var result = await Runner.RunAsync("test", "target", "--filter=key=value", "-v");
            CliTestAssertions.AssertSuccess(result);
            CliTestAssertions.AssertOutputContains(result, "Filter: key=value");
        });

        Test("Negative number as option value", async () =>
        {
            var result = await Runner.RunAsync("test", "target", "--seed=-12345", "-v");
            CliTestAssertions.AssertSuccess(result);
            CliTestAssertions.AssertOutputContains(result, "Random Seed: -12345");
        });

        Test("Mixed option styles in same command", async () =>
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
        });

        AddTestGroup("Boundary Cases", () =>
        {
            Test("Very long option value", async () =>
            {
                var longValue = new string('x', 1000);
                var result = await Runner.RunAsync("test", "target", $"--filter={longValue}", "-v");
                CliTestAssertions.AssertSuccess(result);
                CliTestAssertions.AssertOutputContains(result, $"Filter: {longValue}");
            });

            Test("Many array elements", async () =>
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
                
                // Should contain first and last tags
                CliTestAssertions.AssertOutputContains(result, "tag00");
                CliTestAssertions.AssertOutputContains(result, "tag49");
            });

            Test("Zero values", async () =>
            {
                var result = await Runner.RunAsync("test", "target", "--timeout=0", "--seed=0", "-v");
                CliTestAssertions.AssertSuccess(result);
                CliTestAssertions.AssertOutputContains(result, "Timeout: 0s");
                CliTestAssertions.AssertOutputContains(result, "Random Seed: 0");
            });

            Test("Maximum integer values", async () =>
            {
                var result = await Runner.RunAsync("test", "target", $"--timeout={int.MaxValue}", "-v");
                CliTestAssertions.AssertSuccess(result);
                CliTestAssertions.AssertOutputContains(result, $"Timeout: {int.MaxValue}s");
            });
        });

        AddTestGroup("Error Recovery", () =>
        {
            Test("Malformed option should provide helpful error", async () =>
            {
                var result = await Runner.RunAsync("test", "target", "--timeout=");
                // Should either succeed with default or fail with clear message
                if (!result.IsSuccess)
                {
                    CliTestAssertions.AssertErrorContains(result, "timeout");
                }
            });

            Test("Conflicting boolean values", async () =>
            {
                // Some parsers might handle this differently
                var result = await Runner.RunAsync("test", "target", "--diag=true", "--diag=false", "-v");
                CliTestAssertions.AssertSuccess(result);
                // Last value should win
                CliTestAssertions.AssertOutputContains(result, "Diagnostic Mode: False");
            });
        });

        AddTestGroup("Platform Specific", () =>
        {
            Test("Path separators in values", async () =>
            {
                var path = OperatingSystem.IsWindows() ? "C:\\temp\\config.json" : "/tmp/config.json";
                var result = await Runner.RunAsync("test", "target", $"--config={path}", "-v");
                CliTestAssertions.AssertSuccess(result);
                CliTestAssertions.AssertOutputContains(result, $"Config: {path}");
            });

            Test("Case sensitivity based on platform", async () =>
            {
                var result = await Runner.RunAsync("test", "TARGET", "-v");
                CliTestAssertions.AssertSuccess(result);
                CliTestAssertions.AssertOutputContains(result, "Running test on target: TARGET");
            });
        });
    }
}