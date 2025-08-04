using ApplicationBuilderHelpers.Test.Playground;
using ApplicationBuilderHelpers.Test.Playground.TestFramework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

Console.WriteLine("ApplicationBuilderHelpers Test Playground");
Console.WriteLine("=========================================\n");

// Determine test executable path
var testExePath = GetTestExecutablePath();
if (testExePath == null)
{
    Console.Error.WriteLine("❌ Could not find test.exe. Please build ApplicationBuilderHelpers.Test.Cli first.");
    Console.Error.WriteLine("   Expected locations:");
    Console.Error.WriteLine("   - ApplicationBuilderHelpers.Test.Cli/bin/Debug/net9.0/test.exe");
    Console.Error.WriteLine("   - ApplicationBuilderHelpers.Test.Cli/bin/Release/net9.0/test.exe");
    return 1;
}

Console.WriteLine($"🎯 Test executable: {testExePath}");

// Check if specific test suite is requested
var requestedSuite = args.FirstOrDefault();
var verbose = args.Contains("--verbose") || args.Contains("-v");

if (verbose)
{
    Environment.SetEnvironmentVariable("SHOW_STACK_TRACE", "true");
}

// Create test runner
var runner = new CliTestRunner(testExePath, verbose);

// Validate that the executable works
Console.WriteLine("🔍 Validating test executable...");
if (!await runner.ValidateExecutableAsync())
{
    Console.Error.WriteLine("❌ Test executable failed validation. Cannot proceed with testing.");
    return 1;
}
Console.WriteLine("✅ Test executable validation passed");

// Define all available test suites
var allTestSuites = new List<TestSuiteBase>();

try
{
    allTestSuites.Add(new CommandLineParsingTests(runner));
    Console.WriteLine("✅ CommandLineParsingTests loaded");
}
catch (Exception ex)
{
    Console.WriteLine($"⚠️ CommandLineParsingTests failed to load: {ex.Message}");
}

try
{
    allTestSuites.Add(new ValidationTests(runner));
    Console.WriteLine("✅ ValidationTests loaded");
}
catch (Exception ex)
{
    Console.WriteLine($"⚠️ ValidationTests failed to load: {ex.Message}");
}

try
{
    allTestSuites.Add(new ComplexScenariosTests(runner));
    Console.WriteLine("✅ ComplexScenariosTests loaded");
}
catch (Exception ex)
{
    Console.WriteLine($"⚠️ ComplexScenariosTests failed to load: {ex.Message}");
}

try
{
    allTestSuites.Add(new EdgeCaseTests(runner));
    Console.WriteLine("✅ EdgeCaseTests loaded");
}
catch (Exception ex)
{
    Console.WriteLine($"⚠️ EdgeCaseTests failed to load: {ex.Message}");
}

try
{
    allTestSuites.Add(new HelpSystemTests(runner));
    Console.WriteLine("✅ HelpSystemTests loaded");
}
catch (Exception ex)
{
    Console.WriteLine($"⚠️ HelpSystemTests failed to load: {ex.Message}");
}

try
{
    allTestSuites.Add(new ExitCodeTests(runner));
    Console.WriteLine("✅ ExitCodeTests loaded");
}
catch (Exception ex)
{
    Console.WriteLine($"⚠️ ExitCodeTests failed to load: {ex.Message}");
}

if (!allTestSuites.Any())
{
    Console.Error.WriteLine("❌ No test suites could be loaded. Cannot proceed with testing.");
    return 1;
}

Console.WriteLine($"\n📋 Loaded {allTestSuites.Count} test suite(s)");

// Filter suites if requested
var testSuites = allTestSuites;
if (!string.IsNullOrEmpty(requestedSuite))
{
    testSuites = allTestSuites
        .Where(s => s.GetSuiteName().Contains(requestedSuite, StringComparison.OrdinalIgnoreCase))
        .ToList();

    if (!testSuites.Any())
    {
        Console.Error.WriteLine($"❌ No test suite found matching '{requestedSuite}'");
        Console.WriteLine("\nAvailable test suites:");
        foreach (var suite in allTestSuites)
        {
            Console.WriteLine($"  - {suite.GetSuiteName()}");
        }
        return 1;
    }

    Console.WriteLine($"🎯 Running filtered test suites matching '{requestedSuite}':");
    foreach (var suite in testSuites)
    {
        Console.WriteLine($"  - {suite.GetSuiteName()}");
    }
}

Console.WriteLine();

// Run tests
var overallStopwatch = Stopwatch.StartNew();
var results = new List<TestSuiteResult>();

foreach (var suite in testSuites)
{
    try
    {
        var result = await suite.RunAsync();
        results.Add(result);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Suite '{suite.GetSuiteName()}' failed with exception: {ex.Message}");
        results.Add(new TestSuiteResult 
        { 
            SuiteName = suite.GetSuiteName(),
            Results = [new TestResult { Name = "Suite Execution", Passed = false, Error = ex.Message }]
        });
    }
}

overallStopwatch.Stop();

PrintAllTestResults(results);

// Print overall summary
PrintOverallSummary(results, overallStopwatch.Elapsed);

// Return exit code based on test results
return results.All(r => r.AllPassed) ? 0 : 1;

static string? GetTestExecutablePath()
{
    // Try multiple paths to find test.exe
    var possiblePaths = new[]
    {
        // Direct path in bin folder
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..",
            "ApplicationBuilderHelpers.Test.Cli", "bin", "Debug", "net9.0", "test.exe"),
        
        // Release build
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..",
            "ApplicationBuilderHelpers.Test.Cli", "bin", "Release", "net9.0", "test.exe"),
        
        // Alternative .NET versions
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..",
            "ApplicationBuilderHelpers.Test.Cli", "bin", "Debug", "net8.0", "test.exe"),
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..",
            "ApplicationBuilderHelpers.Test.Cli", "bin", "Debug", "net7.0", "test.exe"),
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..",
            "ApplicationBuilderHelpers.Test.Cli", "bin", "Debug", "net6.0", "test.exe"),
    };

    foreach (var path in possiblePaths)
    {
        var fullPath = Path.GetFullPath(path);
        if (File.Exists(fullPath))
            return fullPath;
    }

    // Try to find it using pattern matching
    var baseDir = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", ".."));
    var cliProjectDir = Path.Combine(baseDir, "ApplicationBuilderHelpers.Test.Cli", "bin");

    if (Directory.Exists(cliProjectDir))
    {
        var exeFiles = Directory.GetFiles(cliProjectDir, "test.exe", SearchOption.AllDirectories);
        if (exeFiles.Any())
            return exeFiles.First();
    }

    return null;
}

static void PrintAllTestResults(List<TestSuiteResult> results)
{
    Console.WriteLine($"\n{'='.Repeat(80)}");
    Console.WriteLine("DETAILED TEST RESULTS");
    Console.WriteLine($"{'='.Repeat(80)}");

    foreach (var suite in results)
    {
        Console.WriteLine($"\n📁 {suite.SuiteName}");
        Console.WriteLine($"{'─'.Repeat(50)}");
        Console.WriteLine($"Duration: {suite.Duration.TotalSeconds:F2}s | Total: {suite.Total} | Passed: {suite.Passed} | Failed: {suite.Failed}");
        
        if (suite.Results.Any())
        {
            Console.WriteLine();
            foreach (var test in suite.Results)
            {
                var status = test.Passed ? "✅" : "❌";
                var duration = test.Duration.TotalMilliseconds > 0 ? $"({test.Duration.TotalMilliseconds:F0}ms)" : "";
                Console.WriteLine($"  {status} {test.Name} {duration}");
                
                if (!test.Passed && !string.IsNullOrEmpty(test.Error))
                {
                    Console.WriteLine($"    💬 {test.Error}");
                }
            }
        }
        
        Console.WriteLine();
    }
}

static void PrintOverallSummary(List<TestSuiteResult> results, TimeSpan totalDuration)
{
    Console.WriteLine($"\n{'='.Repeat(80)}");
    Console.WriteLine("COMPREHENSIVE TEST SUMMARY");
    Console.WriteLine($"{'='.Repeat(80)}");

    var totalTests = results.Sum(r => r.Total);
    var totalPassed = results.Sum(r => r.Passed);
    var totalFailed = results.Sum(r => r.Failed);

    // Detailed suite-by-suite breakdown
    Console.WriteLine("\n📊 DETAILED SUITE BREAKDOWN:");
    Console.WriteLine($"{"Suite Name",-25} {"Tests",-8} {"Passed",-8} {"Failed",-8} {"Rate",-8} {"Duration",-10}");
    Console.WriteLine($"{new string('─', 25)} {new string('─', 8)} {new string('─', 8)} {new string('─', 8)} {new string('─', 8)} {new string('─', 10)}");
    
    foreach (var suite in results)
    {
        var suiteStatus = suite.AllPassed ? "✅" : "❌";
        var passRate = suite.Total > 0 ? (double)suite.Passed / suite.Total * 100 : 0;
        Console.WriteLine($"{(suite.SuiteName.Length > 23 ? suite.SuiteName.Substring(0, 20) + "..." : suite.SuiteName),-25} " +
                         $"{suite.Total,-8} {suite.Passed,-8} {suite.Failed,-8} {passRate:F1}%{"",-3} {suite.Duration.TotalSeconds:F2}s{"",-4}");
    }

    // Print all individual test results
    Console.WriteLine($"\n📋 ALL TEST RESULTS:");
    var testCounter = 1;
    foreach (var suite in results)
    {
        Console.WriteLine($"\n📁 {suite.SuiteName} ({suite.Passed}/{suite.Total} passed)");
        foreach (var test in suite.Results)
        {
            var status = test.Passed ? "✅" : "❌";
            var duration = test.Duration.TotalMilliseconds > 0 ? $"({test.Duration.TotalMilliseconds:F0}ms)" : "";
            Console.WriteLine($"  {testCounter:D3}. {status} {test.Name} {duration}");
            
            if (!test.Passed && !string.IsNullOrEmpty(test.Error))
            {
                Console.WriteLine($"       💬 Error: {test.Error}");
            }
            testCounter++;
        }
    }

    // Overall statistics
    Console.WriteLine($"\n📈 OVERALL STATISTICS:");
    Console.WriteLine($"┌─────────────────────────────────────────────────────────────────┐");
    Console.WriteLine($"│ Test Suites:        {results.Count,8}                                     │");
    Console.WriteLine($"│ Total Tests:        {totalTests,8}                                     │");
    Console.WriteLine($"│ Passed Tests:       {totalPassed,8} ✅                                   │");
    Console.WriteLine($"│ Failed Tests:       {totalFailed,8} ❌                                   │");
    Console.WriteLine($"│ Total Duration:     {totalDuration.TotalSeconds,8:F2}s                              │");
    
    if (totalTests > 0)
    {
        var passRate = (double)totalPassed / totalTests * 100;
        var avgTestDuration = totalDuration.TotalMilliseconds / totalTests;
        Console.WriteLine($"│ Success Rate:       {passRate,8:F1}%                               │");
        Console.WriteLine($"│ Avg Test Duration:  {avgTestDuration,8:F1}ms                              │");
    }
    Console.WriteLine($"└─────────────────────────────────────────────────────────────────┘");

    // Failed tests summary
    if (totalFailed > 0)
    {
        Console.WriteLine($"\n❌ FAILED TESTS SUMMARY ({totalFailed} total):");
        var failedCount = 1;
        foreach (var suite in results)
        {
            var failedTests = suite.Results.Where(r => !r.Passed).ToList();
            if (failedTests.Any())
            {
                Console.WriteLine($"\n  📁 {suite.SuiteName} - {failedTests.Count} failed test(s):");
                foreach (var test in failedTests)
                {
                    Console.WriteLine($"    {failedCount,2}. ❌ {test.Name}");
                    Console.WriteLine($"        💬 {test.Error}");
                    if (!string.IsNullOrEmpty(test.StackTrace) && Environment.GetEnvironmentVariable("SHOW_STACK_TRACE") == "true")
                    {
                        var stackLines = test.StackTrace.Split('\n').Take(3);
                        foreach (var line in stackLines)
                        {
                            Console.WriteLine($"        📍 {line.Trim()}");
                        }
                    }
                    failedCount++;
                }
            }
        }
    }
    else if (totalTests > 0)
    {
        Console.WriteLine("\n🎉 PERFECT SCORE - ALL TESTS PASSED!");
        Console.WriteLine("🚀 The ApplicationBuilderHelpers CLI is working flawlessly!");
        Console.WriteLine($"✨ Successfully executed {totalTests} tests across {results.Count} test suites");
        Console.WriteLine($"⚡ Average execution time: {totalDuration.TotalMilliseconds / totalTests:F1}ms per test");
        
        // Performance assessment
        var avgSuiteDuration = totalDuration.TotalSeconds / results.Count;
        if (avgSuiteDuration < 2.0)
            Console.WriteLine("🏆 Excellent performance - very fast execution!");
        else if (avgSuiteDuration < 5.0)
            Console.WriteLine("👍 Good performance - reasonable execution time");
        else
            Console.WriteLine("⏱️ Consider optimizing - execution time could be improved");
    }
    else
    {
        Console.WriteLine("\n⚠️ No tests were executed.");
        Console.WriteLine("💡 Check if test suites are properly loaded and CLI executable is available.");
    }

    // Final recommendations
    Console.WriteLine($"\n💡 USAGE TIPS:");
    Console.WriteLine($"   • Run specific suite: dotnet run -- \"Command Line\"");
    Console.WriteLine($"   • Verbose output: dotnet run -- --verbose");
    Console.WriteLine($"   • Show stack traces: SHOW_STACK_TRACE=true dotnet run");
    Console.WriteLine($"   • Filter tests: dotnet run -- \"Validation\"");

    Console.WriteLine($"\n{'='.Repeat(80)}");
}

internal static class StringExtensions
{
    public static string Repeat(this char c, int count) => new string(c, count);
}