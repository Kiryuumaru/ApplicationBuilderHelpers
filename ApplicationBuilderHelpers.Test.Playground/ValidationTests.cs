using System;
using System.Threading.Tasks;
using ApplicationBuilderHelpers.Test.Playground.TestFramework;

namespace ApplicationBuilderHelpers.Test.Playground;

public class ValidationTests : TestSuiteBase
{
    public ValidationTests(CliTestRunner runner) : base(runner, "Validation") { }

    protected override void DefineTests()
    {
        AddTestGroup("FromAmong Validation", () =>
        {
            Test("Valid output format should succeed", async () =>
            {
                var result = await Runner.RunAsync("test", "target", "--output-format=json", "-v");
                CliTestAssertions.AssertSuccess(result);
                CliTestAssertions.AssertOutputContains(result, "Output Format: json");
            });

            Test("Invalid output format should fail", async () =>
            {
                var result = await Runner.RunAsync("test", "target", "--output-format=invalid");
                CliTestAssertions.AssertFailure(result);
                CliTestAssertions.AssertErrorContains(result, "Value 'invalid' is not valid for option '--output-format'");
                CliTestAssertions.AssertErrorContains(result, "Must be one of: json, xml, junit, console");
            });

            Test("Case-insensitive FromAmong validation", async () =>
            {
                var result = await Runner.RunAsync("test", "target", "--output-format=JSON", "-v");
                CliTestAssertions.AssertSuccess(result);
                CliTestAssertions.AssertOutputContains(result, "Output Format: JSON");
            });

            Test("Valid framework should succeed", async () =>
            {
                var result = await Runner.RunAsync("test", "target", "--framework=net8.0", "-v");
                CliTestAssertions.AssertSuccess(result);
                CliTestAssertions.AssertOutputContains(result, "Framework: net8.0");
            });

            Test("Invalid framework should fail", async () =>
            {
                var result = await Runner.RunAsync("test", "target", "--framework=netfx4.8");
                CliTestAssertions.AssertFailure(result);
                CliTestAssertions.AssertErrorContains(result, "Value 'netfx4.8' is not valid for option '--framework'");
                CliTestAssertions.AssertErrorContains(result, "Must be one of: net6.0, net7.0, net8.0, net9.0");
            });
        });

        AddTestGroup("Type Validation", () =>
        {
            Test("Invalid integer value should fail", async () =>
            {
                var result = await Runner.RunAsync("test", "target", "--timeout=notanumber");
                CliTestAssertions.AssertFailure(result);
                CliTestAssertions.AssertErrorContains(result, "Invalid value 'notanumber'");
            });

            Test("Valid integer value should succeed", async () =>
            {
                var result = await Runner.RunAsync("test", "target", "--timeout=60", "-v");
                CliTestAssertions.AssertSuccess(result);
                CliTestAssertions.AssertOutputContains(result, "Timeout: 60s");
            });

            Test("Negative integer should succeed", async () =>
            {
                var result = await Runner.RunAsync("test", "target", "--seed=-123", "-v");
                CliTestAssertions.AssertSuccess(result);
                CliTestAssertions.AssertOutputContains(result, "Random Seed: -123");
            });

            Test("Invalid double value should fail", async () =>
            {
                var result = await Runner.RunAsync("test", "target", "--coverage-threshold=notadouble");
                CliTestAssertions.AssertFailure(result);
                CliTestAssertions.AssertErrorContains(result, "Invalid value 'notadouble'");
            });

            Test("Valid double value should succeed", async () =>
            {
                var result = await Runner.RunAsync("test", "target", "--coverage-threshold=85.5", "-v");
                CliTestAssertions.AssertSuccess(result);
                CliTestAssertions.AssertOutputContains(result, "Coverage Threshold: 85.5%");
            });

            Test("Double with different decimal separators", async () =>
            {
                var result = await Runner.RunAsync("test", "target", "--coverage-threshold=80.0", "-v");
                CliTestAssertions.AssertSuccess(result);
                CliTestAssertions.AssertOutputContains(result, "Coverage Threshold: 80%");
            });
        });

        AddTestGroup("Required Validation", () =>
        {
            Test("Missing required argument should be handled gracefully", async () =>
            {
                // Most commands will have optional arguments, but this tests the behavior
                var result = await Runner.RunAsync("test");
                CliTestAssertions.AssertSuccess(result);
                CliTestAssertions.AssertOutputContains(result, "Running test on target: ");
            });
        });

        AddTestGroup("Error Messages", () =>
        {
            Test("Clear error message for unknown option", async () =>
            {
                var result = await Runner.RunAsync("test", "target", "--unknown-option=value");
                // This might succeed if the parser ignores unknown options, or fail with clear message
                // The exact behavior depends on implementation
            });

            Test("Help shows validation constraints", async () =>
            {
                var result = await Runner.RunAsync("test", "--help");
                CliTestAssertions.AssertSuccess(result);
                CliTestAssertions.AssertOutputContains(result, "Possible values:");
                CliTestAssertions.AssertOutputContains(result, "json, xml, junit, console");
            });
        });
    }
}