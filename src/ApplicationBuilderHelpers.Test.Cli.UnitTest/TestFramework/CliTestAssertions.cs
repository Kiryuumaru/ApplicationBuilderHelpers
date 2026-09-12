using System.Text.RegularExpressions;

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
            throw new InvalidOperationException("Could not find test executable (test/test.exe/test.dll). Please build ApplicationBuilderHelpers.Test.Cli first.");
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
        var baseDir = AppContext.BaseDirectory;
        var nativeName = OperatingSystem.IsWindows() ? "test.exe" : "test";
        var apphostNames = OperatingSystem.IsWindows()
            ? new[] { "test.exe" }
            : new[] { "test", "test.exe" };
        // Prefer the CLI binary matching this test run's own configuration (avoids stale cross-config picks).
        var configs = baseDir.Contains($"{Path.DirectorySeparatorChar}Debug{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
            ? new[] { "Debug", "Release" }
            : new[] { "Release", "Debug" };

        // 1) Anchor off the test assembly directory: <...>/ApplicationBuilderHelpers.Test.Cli.UnitTest/bin/<Config>/<TFM>/
        //    so resolution is independent of the test runner's CWD.
        var testAssemblyDir = new DirectoryInfo(baseDir);
        for (var dir = testAssemblyDir; dir != null; dir = dir.Parent)
        {
            if (string.Equals(dir.Name, "ApplicationBuilderHelpers.Test.Cli.UnitTest", StringComparison.OrdinalIgnoreCase))
            {
                var srcDir = dir.Parent;
                if (srcDir != null)
                {
                    var cliOutDir = Path.Combine(srcDir.FullName, "ApplicationBuilderHelpers.Test.Cli", "bin");
                    var found = ProbeCliOutput(cliOutDir, apphostNames, configs);
                    if (found != null)
                    {
                        return found;
                    }
                }
                break;
            }
        }

        // 2) Sibling CLI output relative to the test assembly output (covers non-standard layouts).
        foreach (var tfm in new[] { "net10.0", "net9.0" })
        {
            foreach (var config in configs)
            {
                foreach (var fileName in apphostNames.Concat(new[] { "test.dll" }))
                {
                    var candidate = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..",
                        "ApplicationBuilderHelpers.Test.Cli", "bin", config, tfm, fileName));
                    if (File.Exists(candidate))
                    {
                        return candidate;
                    }
                }
            }
        }

        // 3) Legacy CWD-relative probes (kept for back-compat), extended to net10.0 + OS-aware names.
        // Hybrid union: branch nativeName entries plus master's 4 literal net10.0/test.exe
        // paths (M1-M4), so the literal superset holds on every OS.
        var possiblePaths = new[]
        {
            "../ApplicationBuilderHelpers.Test.Cli/bin/Debug/net10.0/" + nativeName,
            "../ApplicationBuilderHelpers.Test.Cli/bin/Release/net10.0/" + nativeName,
            "../ApplicationBuilderHelpers.Test.Cli/bin/Debug/net10.0/test.exe",
            "../ApplicationBuilderHelpers.Test.Cli/bin/Release/net10.0/test.exe",
            "../ApplicationBuilderHelpers.Test.Cli/bin/Debug/net9.0/test.exe",
            "../ApplicationBuilderHelpers.Test.Cli/bin/Release/net9.0/test.exe",
            "ApplicationBuilderHelpers.Test.Cli/bin/Debug/net10.0/" + nativeName,
            "ApplicationBuilderHelpers.Test.Cli/bin/Release/net10.0/" + nativeName,
            "ApplicationBuilderHelpers.Test.Cli/bin/Debug/net10.0/test.exe",
            "ApplicationBuilderHelpers.Test.Cli/bin/Release/net10.0/test.exe",
            "ApplicationBuilderHelpers.Test.Cli/bin/Debug/net9.0/test.exe",
            "ApplicationBuilderHelpers.Test.Cli/bin/Release/net9.0/test.exe",
            "./" + nativeName,
            nativeName
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

    private static string? ProbeCliOutput(string cliBinDir, string[] apphostNames, string[] configs)
    {
        if (!Directory.Exists(cliBinDir))
        {
            return null;
        }

        // Prefer net10.0 apphost in matching config, then net9.0 legacy, then framework-dependent test.dll.
        foreach (var tfm in new[] { "net10.0", "net9.0" })
        {
            foreach (var config in configs)
            {
                var configDir = Path.Combine(cliBinDir, config, tfm);
                foreach (var fileName in apphostNames)
                {
                    var candidate = Path.Combine(configDir, fileName);
                    if (File.Exists(candidate))
                    {
                        return candidate;
                    }
                }
            }
        }

        foreach (var tfm in new[] { "net10.0", "net9.0" })
        {
            foreach (var config in configs)
            {
                var candidate = Path.Combine(cliBinDir, config, tfm, "test.dll");
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }
        }

        return null;
    }
}