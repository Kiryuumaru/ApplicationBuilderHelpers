using System.Diagnostics;
using System.Text;

namespace ApplicationBuilderHelpers.Test.Playground;

internal class Program
{
    private static async Task<int> Main(string[] args)
    {
        // Run the comprehensive test suite
        return await ComprehensiveTestSuite.RunAllTests();
    }
}

public class ComprehensiveTestSuite
{
    public static async Task<int> RunAllTests()
    {
        Console.WriteLine("🧪 ApplicationBuilderHelpers Comprehensive Test Suite");
        Console.WriteLine("====================================================");
        Console.WriteLine();

        var testRunner = new TestRunner();
        
        // === CORE FUNCTIONALITY TESTS ===
        Console.WriteLine("📋 Core Functionality Tests");
        Console.WriteLine("----------------------------");
        await testRunner.RunTestAsync("Global Help", TestGlobalHelp);
        await testRunner.RunTestAsync("Version Check", TestVersion);
        await testRunner.RunTestAsync("Build Command Help", TestBuildCommandHelp);
        await testRunner.RunTestAsync("Test Command Help", TestTestCommandHelp);
        Console.WriteLine();

        // === COMMAND EXECUTION TESTS ===
        Console.WriteLine("🚀 Command Execution Tests");
        Console.WriteLine("---------------------------");
        await testRunner.RunTestAsync("Main Command - Default Execution", TestMainCommandExecution);
        await testRunner.RunTestAsync("Build Command - Valid Execution", TestBuildValidExecution);
        await testRunner.RunTestAsync("Test Command - Valid Execution", TestTestValidExecution);
        Console.WriteLine();

        // === ERROR HANDLING TESTS ===
        Console.WriteLine("❌ Error Handling Tests");
        Console.WriteLine("------------------------");
        await testRunner.RunTestAsync("Build Command - Missing Required Argument", TestBuildMissingArgument);
        await testRunner.RunTestAsync("Invalid Command", TestInvalidCommand);
        await testRunner.RunTestAsync("Unknown Option", TestUnknownOption);
        Console.WriteLine();

        // === OPTIONS PARSING TESTS ===
        Console.WriteLine("⚙️ Options Parsing Tests");
        Console.WriteLine("-------------------------");
        await testRunner.RunTestAsync("Command Options - Complex", TestCommandOptions);
        await testRunner.RunTestAsync("Boolean Options", TestBooleanOptions);
        await testRunner.RunTestAsync("Short vs Long Options", TestShortVsLongOptions);
        Console.WriteLine();

        // === ARRAY OPTIONS TESTS ===
        Console.WriteLine("📚 Array Options Tests");
        Console.WriteLine("-----------------------");
        await testRunner.RunTestAsync("Array Options - Single Value", TestArrayOptionsSingle);
        await testRunner.RunTestAsync("Array Options - Multiple Values", TestArrayOptionsMultiple);
        await testRunner.RunTestAsync("Array Options - Mixed", TestArrayOptionsMixed);
        Console.WriteLine();

        // === ENVIRONMENT VARIABLES TESTS ===
        Console.WriteLine("🌍 Environment Variables Tests");
        Console.WriteLine("-------------------------------");
        await testRunner.RunTestAsync("Environment Variables - Basic", TestEnvironmentVariables);
        await testRunner.RunTestAsync("Environment Variables - Override", TestEnvironmentVariablesOverride);
        await testRunner.RunTestAsync("Environment Variables - Missing", TestEnvironmentVariablesMissing);
        Console.WriteLine();

        // === GLOBAL OPTIONS TESTS ===
        Console.WriteLine("🌐 Global Options Tests");
        Console.WriteLine("------------------------");
        await testRunner.RunTestAsync("Global Options - Log Level", TestGlobalLogLevel);
        await testRunner.RunTestAsync("Global Options - Complex Command", TestGlobalOptionsComplex);
        Console.WriteLine();

        // === DEFAULT VALUES TESTS ===
        Console.WriteLine("🎯 Default Values Tests");
        Console.WriteLine("------------------------");
        await testRunner.RunTestAsync("Default Values Display", TestDefaultValuesInHelp);
        await testRunner.RunTestAsync("Default Values Usage", TestDefaultValuesUsage);
        Console.WriteLine();

        // === HELP SYSTEM TESTS ===
        Console.WriteLine("❓ Help System Tests");
        Console.WriteLine("--------------------");
        await testRunner.RunTestAsync("Help - Two Column Layout", TestTwoColumnLayout);
        await testRunner.RunTestAsync("Help - Color Support", TestColorSupport);
        await testRunner.RunTestAsync("Help - FromAmong Display", TestFromAmongDisplay);
        await testRunner.RunTestAsync("Help - Environment Variables Display", TestEnvironmentVariablesDisplay);
        Console.WriteLine();

        // === EDGE CASES TESTS ===
        Console.WriteLine("🔄 Edge Cases Tests");
        Console.WriteLine("-------------------");
        await testRunner.RunTestAsync("Empty Arguments", TestEmptyArguments);
        await testRunner.RunTestAsync("Only Flags", TestOnlyFlags);
        await testRunner.RunTestAsync("Mixed Options and Arguments", TestMixedOptionsAndArguments);
        await testRunner.RunTestAsync("Special Characters in Values", TestSpecialCharacters);
        Console.WriteLine();

        // === TYPE PARSER INTEGRATION TESTS ===
        Console.WriteLine("🔧 Type Parser Integration Tests");
        Console.WriteLine("---------------------------------");
        await testRunner.RunTestAsync("Integer Option Parsing", TestIntegerParsing);
        await testRunner.RunTestAsync("Invalid Type Values", TestInvalidTypeValues);
        Console.WriteLine();

        // === COMMAND DISCOVERY TESTS ===
        Console.WriteLine("🔍 Command Discovery Tests");
        Console.WriteLine("---------------------------");
        await testRunner.RunTestAsync("Case Sensitivity", TestCaseSensitivity);
        Console.WriteLine();

        testRunner.PrintSummary();
        
        return testRunner.HasFailures ? 1 : 0;
    }

    private static async Task<TestResult> TestGlobalHelp()
    {
        var result = await RunTestExe("--help");
        
        var expectedContent = new[]
        {
            "ApplicationBuilderHelpers Test CLI v2.1.0",
            "USAGE:",
            "GLOBAL OPTIONS:",
            "-l, --log-level <LOGLEVEL>",
            "Set the logging level",
            "-h, --help",
            "Show this help message",
            "-V, --version",
            "Show version information",
            "COMMANDS:",
            "build",
            "Build the project with optional deployment",
            "test",
            "Run various test operations"
        };

        return ValidateOutput(result, 0, expectedContent, "Global help should display correctly");
    }

    private static async Task<TestResult> TestVersion()
    {
        var result = await RunTestExe("--version");
        
        return ValidateOutput(result, 0, ["2.1.0"], "Version should be displayed");
    }

    private static async Task<TestResult> TestBuildCommandHelp()
    {
        var result = await RunTestExe("build", "--help");
        
        var expectedContent = new[]
        {
            "Build the project with optional deployment",
            "USAGE:",
            "test build [OPTIONS] [GLOBAL OPTIONS] [ARGS]",
            "OPTIONS:",
            "-r, --release",
            "Build in release mode",
            "-o, --output <OUTPUTPATH>",
            "Output directory",
            "--target <TARGET>",
            "Build target",
            "ARGUMENTS:",
            "project",
            "Project file to build (REQUIRED)",
            "GLOBAL OPTIONS:",
            "-l, --log-level <LOGLEVEL>",
            "Set the logging level",
            "-h, --help",
            "-V, --version"
        };

        return ValidateOutput(result, 0, expectedContent, "Build command help should show required argument");
    }

    private static async Task<TestResult> TestTestCommandHelp()
    {
        var result = await RunTestExe("test", "--help");
        
        var expectedContent = new[]
        {
            "Run various test operations",
            "OPTIONS:",
            "-v, --verbose",
            "Enable verbose output",
            "-c, --config <CONFIGPATH>",
            "Configuration file path",
            "--timeout <TIMEOUT>",
            "Timeout in seconds",
            "--parallel",
            "Run tests in parallel",
            "ARGUMENTS:",
            "target",
            "Target to test",
            "GLOBAL OPTIONS:"
        };

        return ValidateOutput(result, 0, expectedContent, "Test command help should display correctly");
    }

    private static async Task<TestResult> TestBuildMissingArgument()
    {
        var result = await RunTestExe("build");
        
        return ValidateOutput(result, 1, ["Missing required argument: project"], "Should error when required argument is missing");
    }

    private static async Task<TestResult> TestBuildValidExecution()
    {
        var result = await RunTestExe("build", "MyProject.csproj");
        
        var expectedContent = new[]
        {
            "Building project: MyProject.csproj",
            "Target: Debug",
            "Release mode: False"
        };

        return ValidateOutput(result, 0, expectedContent, "Build command should execute successfully with required argument");
    }

    private static async Task<TestResult> TestTestValidExecution()
    {
        var result = await RunTestExe("test", "MyTarget", "--verbose", "--parallel");
        
        var expectedContent = new[]
        {
            "Running test on target: MyTarget",
            "Config: default",
            "Timeout: 30s",
            "Parallel: True"
        };

        return ValidateOutput(result, 0, expectedContent, "Test command should execute with options");
    }

    private static async Task<TestResult> TestMainCommandExecution()
    {
        var result = await RunTestExe();
        
        var expectedContent = new[]
        {
            "ApplicationBuilderHelpers Test CLI - Default Command",
            "Use --help to see available commands and options."
        };

        return ValidateOutput(result, 0, expectedContent, "Main command should execute as default");
    }

    private static async Task<TestResult> TestGlobalLogLevel()
    {
        var result = await RunTestExe("--log-level", "debug", "build", "test.csproj");
        
        var expectedContent = new[]
        {
            "Building project: test.csproj"
        };

        return ValidateOutput(result, 0, expectedContent, "Global log level option should work");
    }

    private static async Task<TestResult> TestCommandOptions()
    {
        var result = await RunTestExe("build", "MyProject.csproj", "--release", "--output", "bin/Release", "--target", "Release");
        
        var expectedContent = new[]
        {
            "Building project: MyProject.csproj",
            "Target: Release",
            "Release mode: True",
            "Output: bin/Release"
        };

        return ValidateOutput(result, 0, expectedContent, "Command options should be parsed correctly");
    }

    // === MISSING TEST METHODS ===
    private static async Task<TestResult> TestArrayOptions()
    {
        var result = await RunTestExe("test", "MyTarget", "--tags", "unit", "--tags", "integration", "--verbose");
        
        var expectedContent = new[]
        {
            "Running test on target: MyTarget",
            "Tags: unit, integration"
        };

        return ValidateOutput(result, 0, expectedContent, "Array options should be parsed correctly");
    }

    private static async Task<TestResult> TestEnvironmentVariables()
    {
        // Set environment variable and test
        Environment.SetEnvironmentVariable("TEST_CONFIG", "/path/to/config.xml");
        var result = await RunTestExe("test", "MyTarget", "--verbose");
        Environment.SetEnvironmentVariable("TEST_CONFIG", null); // Clean up
        
        var expectedContent = new[]
        {
            "Running test on target: MyTarget",
            "Config: /path/to/config.xml"
        };

        return ValidateOutput(result, 0, expectedContent, "Environment variables should be used as defaults");
    }

    private static async Task<TestResult> TestEnvironmentVariablesOverride()
    {
        Environment.SetEnvironmentVariable("TEST_CONFIG", "/env/config.xml");
        var result = await RunTestExe("test", "MyTarget", "--config", "/override/config.xml", "--verbose");
        Environment.SetEnvironmentVariable("TEST_CONFIG", null);
        
        var expectedContent = new[]
        {
            "Running test on target: MyTarget",
            "Config: /override/config.xml"
        };

        return ValidateOutput(result, 0, expectedContent, "Command line options should override environment variables");
    }

    private static async Task<TestResult> TestEnvironmentVariablesMissing()
    {
        Environment.SetEnvironmentVariable("TEST_CONFIG", null); // Ensure it's not set
        var result = await RunTestExe("test", "MyTarget", "--verbose");
        
        var expectedContent = new[]
        {
            "Running test on target: MyTarget",
            "Config: default"
        };

        return ValidateOutput(result, 0, expectedContent, "Missing environment variables should use defaults");
    }

    private static async Task<TestResult> TestBooleanOptions()
    {
        var result = await RunTestExe("test", "MyTarget", "--verbose", "--parallel");
        
        var expectedContent = new[]
        {
            "Running test on target: MyTarget",
            "Parallel: True"
        };

        return ValidateOutput(result, 0, expectedContent, "Boolean flags should be parsed correctly");
    }

    private static async Task<TestResult> TestShortVsLongOptions()
    {
        var result1 = await RunTestExe("test", "MyTarget", "-v");
        var result2 = await RunTestExe("test", "MyTarget", "--verbose");
        
        var bothSuccessful = result1.ExitCode == 0 && result2.ExitCode == 0;
        var similarOutput = result1.Output.Contains("MyTarget") && result2.Output.Contains("MyTarget");
        
        return new TestResult
        {
            Success = bothSuccessful && similarOutput,
            Message = bothSuccessful && similarOutput ? "✅ Short and long options should work identically" : "❌ Short and long options behave differently",
            Output = $"Short: {result1.Output}\nLong: {result2.Output}",
            Error = $"Short: {result1.Error}\nLong: {result2.Error}",
            ExitCode = bothSuccessful ? 0 : 1,
            Arguments = "Short vs Long comparison"
        };
    }

    private static async Task<TestResult> TestArrayOptionsSingle()
    {
        var result = await RunTestExe("test", "MyTarget", "--tags", "unit", "--verbose");
        
        var expectedContent = new[]
        {
            "Running test on target: MyTarget",
            "Tags: unit"
        };

        return ValidateOutput(result, 0, expectedContent, "Single array option should be parsed correctly");
    }

    private static async Task<TestResult> TestArrayOptionsMultiple()
    {
        var result = await RunTestExe("test", "MyTarget", "--tags", "unit", "--tags", "integration", "--tags", "e2e", "--verbose");
        
        var expectedContent = new[]
        {
            "Running test on target: MyTarget",
            "Tags: unit, integration, e2e"
        };

        return ValidateOutput(result, 0, expectedContent, "Multiple array options should be collected correctly");
    }

    private static async Task<TestResult> TestArrayOptionsMixed()
    {
        var result = await RunTestExe("test", "MyTarget", "--tags", "unit", "--exclude", "slow", "--tags", "fast", "--verbose");
        
        var expectedContent = new[]
        {
            "Running test on target: MyTarget",
            "Tags: unit, fast",
            "Exclude: slow"
        };

        return ValidateOutput(result, 0, expectedContent, "Mixed array options should be parsed independently");
    }

    // === ERROR HANDLING TESTS ===
    private static async Task<TestResult> TestMissingRequiredOption()
    {
        var result = await RunTestExe("build", "test.csproj", "--output", "/path/output");
        
        // Assuming we add a required option to test this
        return ValidateOutput(result, 0, ["Building project: test.csproj"], "Should handle missing non-required options gracefully");
    }

    private static async Task<TestResult> TestInvalidCommand()
    {
        var result = await RunTestExe("invalid-command");
        
        return ValidateOutput(result, 1, ["No command found."], "Should error on invalid command");
    }

    private static async Task<TestResult> TestUnknownOption()
    {
        var result = await RunTestExe("build", "test.csproj", "--unknown-option");
        
        return ValidateOutput(result, 1, ["Unknown option: --unknown-option"], "Should error on unknown option");
    }

    private static async Task<TestResult> TestGlobalOptionsComplex()
    {
        var result = await RunTestExe("--log-level", "debug", "test", "MyTarget", "--verbose", "--parallel");
        
        var expectedContent = new[]
        {
            "Running test on target: MyTarget",
            "Parallel: True"
        };

        return ValidateOutput(result, 0, expectedContent, "Global options should work with complex commands");
    }

    private static async Task<TestResult> TestDefaultValuesInHelp()
    {
        var result = await RunTestExe("build", "--help");
        
        var expectedContent = new[]
        {
            "Default:",
            "Debug"
        };

        return ValidateOutput(result, 0, expectedContent, "Default values should be displayed in help");
    }

    private static async Task<TestResult> TestDefaultValuesUsage()
    {
        var result = await RunTestExe("build", "test.csproj");
        
        var expectedContent = new[]
        {
            "Building project: test.csproj",
            "Target: Debug",
            "Output: bin/Debug"
        };

        return ValidateOutput(result, 0, expectedContent, "Default values should be used when options not specified");
    }

    private static async Task<TestResult> TestTwoColumnLayout()
    {
        var result = await RunTestExe("build", "--help");
        
        var expectedContent = new[]
        {
            "-r, --release",
            "Build in release mode",
            "-o, --output",
            "Output directory"
        };

        return ValidateOutput(result, 0, expectedContent, "Help should display in proper two-column layout");
    }

    private static async Task<TestResult> TestColorSupport()
    {
        var result = await RunTestExe("--help");
        
        // Check for ANSI color codes in output
        var hasColors = result.Output.Contains("\u001b[") || result.Output.Contains("[96m") || result.Output.Contains("[93m");
        
        return new TestResult
        {
            Success = hasColors,
            Message = hasColors ? "✅ Help output should contain color codes" : "❌ Help output missing color support",
            Output = result.Output,
            Error = result.Error,
            ExitCode = result.ExitCode,
            Arguments = "--help"
        };
    }

    private static async Task<TestResult> TestFromAmongDisplay()
    {
        var result = await RunTestExe("build", "--help");
        
        var expectedContent = new[]
        {
            "Possible values:",
            "Debug, Release, Test"
        };

        return ValidateOutput(result, 0, expectedContent, "FromAmong values should be displayed in help");
    }

    private static async Task<TestResult> TestEnvironmentVariablesDisplay()
    {
        var result = await RunTestExe("build", "--help");
        
        var expectedContent = new[]
        {
            "Environment variable:",
            "BUILD_OUTPUT"
        };

        return ValidateOutput(result, 0, expectedContent, "Environment variables should be displayed in help");
    }

    private static async Task<TestResult> TestEmptyArguments()
    {
        var result = await RunTestExe();
        
        // Should show default command or global help
        return new TestResult
        {
            Success = result.ExitCode == 0,
            Message = result.ExitCode == 0 ? "✅ Empty arguments should be handled gracefully" : "❌ Empty arguments caused error",
            Output = result.Output,
            Error = result.Error,
            ExitCode = result.ExitCode,
            Arguments = "(empty)"
        };
    }

    private static async Task<TestResult> TestOnlyFlags()
    {
        var result = await RunTestExe("test", "--verbose", "--parallel");
        
        var expectedContent = new[]
        {
            "Running test on target: default",
            "Parallel: True"
        };

        return ValidateOutput(result, 0, expectedContent, "Commands with only boolean flags should work");
    }

    private static async Task<TestResult> TestMixedOptionsAndArguments()
    {
        var result = await RunTestExe("build", "--release", "MyProject.csproj", "--output", "bin/Release", "--target", "Release");
        
        var expectedContent = new[]
        {
            "Building project: MyProject.csproj",
            "Release mode: True",
            "Output: bin/Release",
            "Target: Release"
        };

        return ValidateOutput(result, 0, expectedContent, "Mixed options and arguments should be parsed correctly");
    }

    private static async Task<TestResult> TestSpecialCharacters()
    {
        var result = await RunTestExe("test", "My-Project_Test", "--config", "/path/with spaces/config.xml");
        
        var expectedContent = new[]
        {
            "Running test on target: My-Project_Test",
            "Config: /path/with spaces/config.xml"
        };

        return ValidateOutput(result, 0, expectedContent, "Special characters in values should be handled correctly");
    }

    private static async Task<TestResult> TestIntegerParsing()
    {
        var result = await RunTestExe("test", "MyTarget", "--timeout", "120", "--verbose");
        
        var expectedContent = new[]
        {
            "Running test on target: MyTarget",
            "Timeout: 120s"
        };

        return ValidateOutput(result, 0, expectedContent, "Integer options should be parsed correctly");
    }

    private static async Task<TestResult> TestInvalidTypeValues()
    {
        var result = await RunTestExe("test", "MyTarget", "--timeout", "invalid");
        
        return ValidateOutput(result, 1, ["Invalid value", "invalid"], "Invalid type values should produce error");
    }

    private static async Task<TestResult> TestCaseSensitivity()
    {
        var result1 = await RunTestExe("build", "test.csproj");
        var result2 = await RunTestExe("BUILD", "test.csproj"); // Should fail
        
        var success1 = result1.ExitCode == 0;
        var success2 = result2.ExitCode != 0; // Should fail
        
        return new TestResult
        {
            Success = success1 && success2,
            Message = success1 && success2 ? "✅ Commands should be case sensitive" : "❌ Case sensitivity not working correctly",
            Output = $"Lower: {result1.Output}\nUpper: {result2.Output}",
            Error = $"Lower: {result1.Error}\nUpper: {result2.Error}",
            ExitCode = success1 && success2 ? 0 : 1,
            Arguments = "Case sensitivity test"
        };
    }

    private static async Task<ProcessResult> RunTestExe(params string[] arguments)
    {
        try
        {
            // Find the test.exe in the CLI project's output directory
            var testExePath = FindTestExe();
            
            if (!File.Exists(testExePath))
            {
                // Try to build the CLI project first
                await BuildCliProject();
                testExePath = FindTestExe();
                
                if (!File.Exists(testExePath))
                    throw new FileNotFoundException($"test.exe not found at {testExePath}");
            }

            var psi = new ProcessStartInfo
            {
                FileName = testExePath,
                WorkingDirectory = Environment.CurrentDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            foreach (var arg in arguments)
            {
                psi.ArgumentList.Add(arg);
            }

            var output = new StringBuilder();
            var error = new StringBuilder();

            using var process = new Process { StartInfo = psi };
            
            process.OutputDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                    output.AppendLine(e.Data);
            };
            
            process.ErrorDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                    error.AppendLine(e.Data);
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            // Set a timeout for the process
            if (!await Task.Run(() => process.WaitForExit(10000)))
            {
                try
                {
                    process.Kill();
                }
                catch { }
                throw new TimeoutException("Process timed out after 10 seconds");
            }

            return new ProcessResult
            {
                ExitCode = process.ExitCode,
                Output = output.ToString(),
                Error = error.ToString(),
                Arguments = string.Join(" ", arguments)
            };
        }
        catch (Exception ex)
        {
            return new ProcessResult
            {
                ExitCode = -1,
                Output = "",
                Error = $"Exception: {ex.Message}",
                Arguments = string.Join(" ", arguments)
            };
        }
    }

    private static string FindTestExe()
    {
        // Look for test.exe in various possible locations
        var basePath = Path.GetDirectoryName(typeof(Program).Assembly.Location) ?? "";
        var possiblePaths = new[]
        {
            Path.Combine(basePath, "..", "..", "..", "..", "ApplicationBuilderHelpers.Test.Cli", "bin", "Debug", "net9.0", "test.exe"),
            Path.Combine(basePath, "..", "..", "..", "..", "ApplicationBuilderHelpers.Test.Cli", "bin", "Release", "net9.0", "test.exe"),
            Path.Combine(Environment.CurrentDirectory, "test.exe"),
            Path.Combine(Environment.CurrentDirectory, "ApplicationBuilderHelpers.Test.Cli", "bin", "Debug", "net9.0", "test.exe"),
            Path.Combine(Environment.CurrentDirectory, "ApplicationBuilderHelpers.Test.Cli", "bin", "Release", "net9.0", "test.exe")
        };

        return possiblePaths.FirstOrDefault(File.Exists) ?? possiblePaths[0];
    }

    private static async Task BuildCliProject()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "build ../ApplicationBuilderHelpers.Test.Cli/ApplicationBuilderHelpers.Test.Cli.csproj -c Debug",
                WorkingDirectory = Environment.CurrentDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process != null)
            {
                await process.WaitForExitAsync();
            }
        }
        catch
        {
            // Ignore build errors for now
        }
    }

    private static TestResult ValidateOutput(ProcessResult result, int expectedExitCode, string[] expectedContent, string description)
    {
        var success = true;
        var message = new StringBuilder();

        if (result.ExitCode != expectedExitCode)
        {
            success = false;
            message.AppendLine($"❌ Expected exit code {expectedExitCode}, got {result.ExitCode}");
        }

        foreach (var expected in expectedContent)
        {
            if (!result.Output.Contains(expected, StringComparison.OrdinalIgnoreCase))
            {
                success = false;
                message.AppendLine($"❌ Missing expected content: '{expected}'");
            }
        }

        if (!string.IsNullOrEmpty(result.Error) && result.ExitCode == 0)
        {
            success = false;
            message.AppendLine($"❌ Unexpected error output: {result.Error}");
        }

        return new TestResult
        {
            Success = success,
            Message = success ? $"✅ {description}" : message.ToString(),
            Output = result.Output,
            Error = result.Error,
            ExitCode = result.ExitCode,
            Arguments = result.Arguments
        };
    }
}

public class TestRunner
{
    private readonly List<TestResult> _results = new();

    public bool HasFailures => _results.Any(r => !r.Success);

    public async Task RunTestAsync(string testName, Func<Task<TestResult>> testFunc)
    {
        Console.Write($"🔍 Running: {testName,-40} ");
        
        try
        {
            var result = await testFunc();
            _results.Add(result);
            
            if (result.Success)
            {
                Console.WriteLine("✅ PASS");
            }
            else
            {
                Console.WriteLine("❌ FAIL");
                Console.WriteLine($"   {result.Message}");
                if (!string.IsNullOrEmpty(result.Arguments))
                {
                    Console.WriteLine($"   Command: test.exe {result.Arguments}");
                }
                if (!string.IsNullOrEmpty(result.Output))
                {
                    Console.WriteLine($"   Output: {result.Output.Replace("\n", "\\n").Replace("\r", "")}");
                }
                if (!string.IsNullOrEmpty(result.Error))
                {
                    Console.WriteLine($"   Error: {result.Error.Replace("\n", "\\n").Replace("\r", "")}");
                }
                Console.WriteLine($"   Exit Code: {result.ExitCode}");
            }
        }
        catch (Exception ex)
        {
            _results.Add(new TestResult 
            { 
                Success = false, 
                Message = $"❌ Exception: {ex.Message}",
                Arguments = "",
                Output = "",
                Error = ex.ToString(),
                ExitCode = -1
            });
            Console.WriteLine("❌ FAIL (Exception)");
            Console.WriteLine($"   {ex.Message}");
        }
        
        Console.WriteLine();
    }

    public void PrintSummary()
    {
        var passed = _results.Count(r => r.Success);
        var failed = _results.Count(r => !r.Success);
        
        Console.WriteLine("📊 Test Summary");
        Console.WriteLine("===============");
        Console.WriteLine($"✅ Passed: {passed}");
        Console.WriteLine($"❌ Failed: {failed}");
        Console.WriteLine($"📋 Total:  {_results.Count}");
        Console.WriteLine();
        
        if (HasFailures)
        {
            Console.WriteLine("❌ Some tests failed!");
        }
        else
        {
            Console.WriteLine("🎉 All tests passed!");
        }
    }
}

public record ProcessResult
{
    public int ExitCode { get; init; }
    public string Output { get; init; } = "";
    public string Error { get; init; } = "";
    public string Arguments { get; init; } = "";
}

public record TestResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = "";
    public string Output { get; init; } = "";
    public string Error { get; init; } = "";
    public int ExitCode { get; init; }
    public string Arguments { get; init; } = "";
}

