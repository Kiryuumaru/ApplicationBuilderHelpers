using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ApplicationBuilderHelpers.Test.Playground.TestFramework;

/// <summary>
/// Provides assertion methods for CLI test results
/// </summary>
public static class CliTestAssertions
{
    public static void AssertSuccess(CliTestResult result, string? message = null)
    {
        if (!result.IsSuccess)
        {
            var errorMessage = $"Expected exit code 0 but got {result.ExitCode}";
            if (!string.IsNullOrEmpty(message))
                errorMessage = $"{message}: {errorMessage}";
            
            if (result.HasError)
                errorMessage += $"\nError output:\n{result.StandardError}";
            
            throw new AssertionException(errorMessage);
        }
    }

    public static void AssertFailure(CliTestResult result, string? message = null)
    {
        if (result.IsSuccess)
        {
            var errorMessage = $"Expected non-zero exit code but got 0";
            if (!string.IsNullOrEmpty(message))
                errorMessage = $"{message}: {errorMessage}";
            
            throw new AssertionException(errorMessage);
        }
    }

    public static void AssertExitCode(CliTestResult result, int expectedExitCode, string? message = null)
    {
        if (result.ExitCode != expectedExitCode)
        {
            var errorMessage = $"Expected exit code {expectedExitCode} but got {result.ExitCode}";
            if (!string.IsNullOrEmpty(message))
                errorMessage = $"{message}: {errorMessage}";
            
            throw new AssertionException(errorMessage);
        }
    }

    public static void AssertOutputContains(CliTestResult result, string expectedText, string? message = null)
    {
        if (!result.StandardOutput.Contains(expectedText, StringComparison.OrdinalIgnoreCase))
        {
            var errorMessage = $"Expected output to contain '{expectedText}'";
            if (!string.IsNullOrEmpty(message))
                errorMessage = $"{message}: {errorMessage}";
            
            errorMessage += $"\nActual output:\n{result.StandardOutput}";
            
            throw new AssertionException(errorMessage);
        }
    }

    public static void AssertOutputDoesNotContain(CliTestResult result, string unexpectedText, string? message = null)
    {
        if (result.StandardOutput.Contains(unexpectedText, StringComparison.OrdinalIgnoreCase))
        {
            var errorMessage = $"Expected output to NOT contain '{unexpectedText}'";
            if (!string.IsNullOrEmpty(message))
                errorMessage = $"{message}: {errorMessage}";
            
            throw new AssertionException(errorMessage);
        }
    }

    public static void AssertErrorContains(CliTestResult result, string expectedText, string? message = null)
    {
        if (!result.StandardError.Contains(expectedText, StringComparison.OrdinalIgnoreCase))
        {
            var errorMessage = $"Expected error to contain '{expectedText}'";
            if (!string.IsNullOrEmpty(message))
                errorMessage = $"{message}: {errorMessage}";
            
            errorMessage += $"\nActual error:\n{result.StandardError}";
            
            throw new AssertionException(errorMessage);
        }
    }

    public static void AssertOutputMatches(CliTestResult result, string pattern, string? message = null)
    {
        var regex = new Regex(pattern, RegexOptions.Multiline | RegexOptions.IgnoreCase);
        if (!regex.IsMatch(result.StandardOutput))
        {
            var errorMessage = $"Expected output to match pattern '{pattern}'";
            if (!string.IsNullOrEmpty(message))
                errorMessage = $"{message}: {errorMessage}";
            
            errorMessage += $"\nActual output:\n{result.StandardOutput}";
            
            throw new AssertionException(errorMessage);
        }
    }

    public static void AssertNoError(CliTestResult result, string? message = null)
    {
        if (result.HasError)
        {
            var errorMessage = "Expected no error output";
            if (!string.IsNullOrEmpty(message))
                errorMessage = $"{message}: {errorMessage}";
            
            errorMessage += $"\nError output:\n{result.StandardError}";
            
            throw new AssertionException(errorMessage);
        }
    }

    public static void AssertExecutionTime(CliTestResult result, TimeSpan maxDuration, string? message = null)
    {
        if (result.ExecutionTime > maxDuration)
        {
            var errorMessage = $"Expected execution time <= {maxDuration.TotalMilliseconds}ms but was {result.ExecutionTime.TotalMilliseconds}ms";
            if (!string.IsNullOrEmpty(message))
                errorMessage = $"{message}: {errorMessage}";
            
            throw new AssertionException(errorMessage);
        }
    }
}

public class AssertionException : Exception
{
    public AssertionException(string message) : base(message) { }
}

public abstract class TestSuiteBase
{
    protected CliTestRunner Runner { get; }
    private readonly string _suiteName;
    private readonly List<TestCase> _tests = new();
    private readonly List<TestGroup> _groups = new();

    protected TestSuiteBase(CliTestRunner runner, string suiteName)
    {
        Runner = runner;
        _suiteName = suiteName;
        DefineTests();
    }

    public string GetSuiteName() => _suiteName;

    protected abstract void DefineTests();

    protected void Test(string name, Func<Task> testAction)
    {
        _tests.Add(new TestCase { Name = name, Action = testAction });
    }

    protected void AddTestGroup(string groupName, Action defineGroupTests)
    {
        var group = new TestGroup { Name = groupName };
        var currentTests = _tests.Count;
        defineGroupTests();
        
        // Move tests added during group definition to the group
        for (int i = currentTests; i < _tests.Count; i++)
        {
            group.Tests.Add(_tests[i]);
        }
        _tests.RemoveRange(currentTests, _tests.Count - currentTests);
        
        _groups.Add(group);
    }

    public async Task<TestSuiteResult> RunAsync()
    {
        var result = new TestSuiteResult { SuiteName = _suiteName };
        var stopwatch = Stopwatch.StartNew();

        Console.WriteLine($"\n{'='.Repeat(60)}");
        Console.WriteLine($"Running Test Suite: {_suiteName}");
        Console.WriteLine($"{'='.Repeat(60)}");

        // Run standalone tests
        foreach (var test in _tests)
        {
            await RunTest(test, result, "");
        }

        // Run grouped tests
        foreach (var group in _groups)
        {
            Console.WriteLine($"\n  {group.Name}:");
            foreach (var test in group.Tests)
            {
                await RunTest(test, result, "  ");
            }
        }

        stopwatch.Stop();
        result.Duration = stopwatch.Elapsed;

        // Print summary
        Console.WriteLine($"\n{'─'.Repeat(60)}");
        Console.WriteLine($"Suite Summary: {result.Passed} passed, {result.Failed} failed, {result.Total} total");
        Console.WriteLine($"Duration: {result.Duration.TotalSeconds:F2}s");
        
        return result;
    }

    private async Task RunTest(TestCase test, TestSuiteResult suiteResult, string indent)
    {
        var testResult = new TestResult { Name = test.Name };
        suiteResult.Results.Add(testResult);

        try
        {
            var stopwatch = Stopwatch.StartNew();
            await test.Action();
            stopwatch.Stop();
            
            testResult.Passed = true;
            testResult.Duration = stopwatch.Elapsed;
            
            Console.WriteLine($"{indent}✅ {test.Name} ({testResult.Duration.TotalMilliseconds:F0}ms)");
        }
        catch (Exception ex)
        {
            testResult.Passed = false;
            testResult.Error = ex.Message;
            testResult.StackTrace = ex.StackTrace;
            
            Console.WriteLine($"{indent}❌ {test.Name}");
            Console.WriteLine($"{indent}   Error: {ex.Message}");
            
            if (Environment.GetEnvironmentVariable("SHOW_STACK_TRACE") == "true")
            {
                Console.WriteLine($"{indent}   Stack: {ex.StackTrace}");
            }
        }
    }

    private class TestCase
    {
        public string Name { get; set; } = "";
        public Func<Task> Action { get; set; } = null!;
    }

    private class TestGroup
    {
        public string Name { get; set; } = "";
        public List<TestCase> Tests { get; set; } = new();
    }
}

public class TestSuiteResult
{
    public string SuiteName { get; set; } = "";
    public List<TestResult> Results { get; set; } = new();
    public TimeSpan Duration { get; set; }
    
    public int Total => Results.Count;
    public int Passed => Results.Count(r => r.Passed);
    public int Failed => Results.Count(r => !r.Passed);
    public bool AllPassed => Failed == 0;
}

public class TestResult
{
    public string Name { get; set; } = "";
    public bool Passed { get; set; }
    public TimeSpan Duration { get; set; }
    public string? Error { get; set; }
    public string? StackTrace { get; set; }
}

internal static class CharExtensions
{
    public static string Repeat(this char c, int count) => new string(c, count);
}