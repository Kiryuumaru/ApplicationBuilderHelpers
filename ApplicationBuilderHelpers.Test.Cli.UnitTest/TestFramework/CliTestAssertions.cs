using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest.TestFramework;

/// <summary>
/// Provides assertion methods for CLI test results using XUnit assertions
/// </summary>
public static class CliTestAssertions
{
    public static void AssertSuccess(CliTestResult result, string? message = null)
    {
        Assert.True(result.IsSuccess, 
            $"{message ?? "Expected success"}: Exit code {result.ExitCode}" +
            (result.HasError ? $"\nError output:\n{result.StandardError}" : ""));
    }

    public static void AssertFailure(CliTestResult result, string? message = null)
    {
        Assert.False(result.IsSuccess, 
            $"{message ?? "Expected failure"}: Got exit code 0 (success)");
    }

    public static void AssertExitCode(CliTestResult result, int expectedExitCode, string? message = null)
    {
        Assert.Equal(expectedExitCode, result.ExitCode);
    }

    public static void AssertOutputContains(CliTestResult result, string expectedText, string? message = null)
    {
        Assert.Contains(expectedText, result.StandardOutput, StringComparison.OrdinalIgnoreCase);
    }

    public static void AssertOutputDoesNotContain(CliTestResult result, string unexpectedText, string? message = null)
    {
        Assert.DoesNotContain(unexpectedText, result.StandardOutput, StringComparison.OrdinalIgnoreCase);
    }

    public static void AssertErrorContains(CliTestResult result, string expectedText, string? message = null)
    {
        Assert.Contains(expectedText, result.StandardError, StringComparison.OrdinalIgnoreCase);
    }

    public static void AssertOutputMatches(CliTestResult result, string pattern, string? message = null)
    {
        var regex = new Regex(pattern, RegexOptions.Multiline | RegexOptions.IgnoreCase);
        Assert.Matches(regex, result.StandardOutput);
    }

    public static void AssertNoError(CliTestResult result, string? message = null)
    {
        Assert.False(result.HasError, 
            $"{message ?? "Expected no error output"}: {result.StandardError}");
    }

    public static void AssertExecutionTime(CliTestResult result, TimeSpan maxDuration, string? message = null)
    {
        Assert.True(result.ExecutionTime <= maxDuration,
            $"{message ?? "Execution time exceeded limit"}: {result.ExecutionTime.TotalMilliseconds}ms > {maxDuration.TotalMilliseconds}ms");
    }
}

/// <summary>
/// Base class for XUnit test collections that test the CLI
/// </summary>
public abstract class CliTestBase : IAsyncLifetime
{
    protected CliTestRunner Runner { get; private set; } = null!;
    private static string? _testExecutablePath;

    public async Task InitializeAsync()
    {
        _testExecutablePath ??= GetTestExecutablePath();
        if (_testExecutablePath == null)
        {
            throw new InvalidOperationException("Could not find test.exe. Please build ApplicationBuilderHelpers.Test.Cli first.");
        }

        Runner = new CliTestRunner(_testExecutablePath, verbose: false);
        
        // Validate that the executable works
        if (!await Runner.ValidateExecutableAsync())
        {
            throw new InvalidOperationException("Test executable failed validation. Cannot proceed with testing.");
        }
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    private static string? GetTestExecutablePath()
    {
        var possiblePaths = new[]
        {
            "../ApplicationBuilderHelpers.Test.Cli/bin/Debug/net9.0/test.exe",
            "../ApplicationBuilderHelpers.Test.Cli/bin/Release/net9.0/test.exe",
            "ApplicationBuilderHelpers.Test.Cli/bin/Debug/net9.0/test.exe",
            "ApplicationBuilderHelpers.Test.Cli/bin/Release/net9.0/test.exe",
            "./test.exe",
            "test.exe"
        };

        foreach (var path in possiblePaths)
        {
            if (File.Exists(path))
            {
                return Path.GetFullPath(path);
            }
        }

        return null;
    }
}