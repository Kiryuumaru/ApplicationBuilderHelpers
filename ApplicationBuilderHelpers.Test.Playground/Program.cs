using System.Diagnostics;
using System.Text;

namespace ApplicationBuilderHelpers.Test.Playground;

internal class Program
{
    private static async Task<int> Main(string[] args)
    {
        Console.WriteLine("🧪 ApplicationBuilderHelpers Test Suite");
        Console.WriteLine("=========================================");
        Console.WriteLine();

        var testRunner = new TestRunner();
        
        // Run all test cases
        await testRunner.RunTestAsync("Global Help", TestGlobalHelp);
        await testRunner.RunTestAsync("Version Check", TestVersion);
        await testRunner.RunTestAsync("Build Command Help", TestBuildCommandHelp);
        await testRunner.RunTestAsync("Test Command Help", TestTestCommandHelp);
        await testRunner.RunTestAsync("Build Command - Missing Required Argument", TestBuildMissingArgument);
        await testRunner.RunTestAsync("Build Command - Valid Execution", TestBuildValidExecution);
        await testRunner.RunTestAsync("Test Command - Valid Execution", TestTestValidExecution);
        await testRunner.RunTestAsync("Main Command - Default Execution", TestMainCommandExecution);
        await testRunner.RunTestAsync("Global Options - Log Level", TestGlobalLogLevel);
        await testRunner.RunTestAsync("Command Options - Verbose and Config", TestCommandOptions);
        await testRunner.RunTestAsync("Invalid Command", TestInvalidCommand);
        await testRunner.RunTestAsync("Unknown Option", TestUnknownOption);
        
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

