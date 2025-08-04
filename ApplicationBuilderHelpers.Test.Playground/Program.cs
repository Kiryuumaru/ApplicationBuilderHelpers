using System.Diagnostics;
using System.Text;
using System.Reflection;

namespace ApplicationBuilderHelpers.Test.Playground;

internal class Program
{
    private static async Task<int> Main(string[] args)
    {
        Console.WriteLine("🚀 ApplicationBuilderHelpers Comprehensive Test Suite");
        Console.WriteLine("====================================================");
        Console.WriteLine();

        // Show diagnostic information
        Console.WriteLine($"📍 Current Directory: {Environment.CurrentDirectory}");
        Console.WriteLine($"📍 Assembly Location: {Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}");
        Console.WriteLine();

        // Try to find and run tests
        var testExePath = FindTestExe();
        Console.WriteLine($"🔍 Looking for test.exe at: {testExePath}");
        
        if (!File.Exists(testExePath))
        {
            Console.WriteLine("❌ Test executable not found! Building CLI project...");
            await BuildCliProject();
            testExePath = FindTestExe();
        }

        if (!File.Exists(testExePath))
        {
            Console.WriteLine("❌ Still not found after build attempt!");
            Console.WriteLine("💡 Manual steps to fix:");
            Console.WriteLine("   1. Run: dotnet build ApplicationBuilderHelpers.Test.Cli");
            Console.WriteLine("   2. Check if the exe exists at expected paths");
            Console.WriteLine("   3. Run this playground again");
            Console.WriteLine();
            Console.WriteLine("📂 Checked paths:");
            await ShowCheckedPaths();
            return 1;
        }

        Console.WriteLine($"✅ Test executable found at: {testExePath}");
        Console.WriteLine();

        // Run the comprehensive test suite
        var result = await ComprehensiveTestSuite.RunAllTests(testExePath);

        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();

        return result;
    }

    private static async Task ShowCheckedPaths()
    {
        var currentAssemblyPath = Assembly.GetExecutingAssembly().Location;
        var currentDir = Path.GetDirectoryName(currentAssemblyPath) ?? Environment.CurrentDirectory;
        
        var possiblePaths = new[]
        {
            Path.Combine(currentDir, "..", "..", "..", "..", "ApplicationBuilderHelpers.Test.Cli", "bin", "Debug", "net9.0", "test.exe"),
            Path.Combine(currentDir, "..", "..", "..", "..", "ApplicationBuilderHelpers.Test.Cli", "bin", "Release", "net9.0", "test.exe"),
            Path.Combine(Environment.CurrentDirectory, "ApplicationBuilderHelpers.Test.Cli", "bin", "Debug", "net9.0", "test.exe"),
            Path.Combine(Environment.CurrentDirectory, "ApplicationBuilderHelpers.Test.Cli", "bin", "Release", "net9.0", "test.exe"),
            Path.Combine(Environment.CurrentDirectory, "test.exe"),
            Path.Combine(Environment.CurrentDirectory, "..", "ApplicationBuilderHelpers.Test.Cli", "bin", "Debug", "net9.0", "test.exe"),
            Path.Combine(Environment.CurrentDirectory, "..", "ApplicationBuilderHelpers.Test.Cli", "bin", "Release", "net9.0", "test.exe")
        };

        foreach (var path in possiblePaths)
        {
            try
            {
                var normalizedPath = Path.GetFullPath(path);
                var exists = File.Exists(normalizedPath);
                Console.WriteLine($"   {(exists ? "✅" : "❌")} {normalizedPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ Error checking {path}: {ex.Message}");
            }
        }
    }

    private static string FindTestExe()
    {
        var currentAssemblyPath = Assembly.GetExecutingAssembly().Location;
        var currentDir = Path.GetDirectoryName(currentAssemblyPath) ?? Environment.CurrentDirectory;
        
        var possiblePaths = new[]
        {
            Path.Combine(currentDir, "..", "..", "..", "..", "ApplicationBuilderHelpers.Test.Cli", "bin", "Debug", "net9.0", "test.exe"),
            Path.Combine(currentDir, "..", "..", "..", "..", "ApplicationBuilderHelpers.Test.Cli", "bin", "Release", "net9.0", "test.exe"),
            Path.Combine(Environment.CurrentDirectory, "ApplicationBuilderHelpers.Test.Cli", "bin", "Debug", "net9.0", "test.exe"),
            Path.Combine(Environment.CurrentDirectory, "ApplicationBuilderHelpers.Test.Cli", "bin", "Release", "net9.0", "test.exe"),
            Path.Combine(Environment.CurrentDirectory, "test.exe"),
            Path.Combine(Environment.CurrentDirectory, "..", "ApplicationBuilderHelpers.Test.Cli", "bin", "Debug", "net9.0", "test.exe"),
            Path.Combine(Environment.CurrentDirectory, "..", "ApplicationBuilderHelpers.Test.Cli", "bin", "Release", "net9.0", "test.exe")
        };

        foreach (var path in possiblePaths)
        {
            try
            {
                var normalizedPath = Path.GetFullPath(path);
                if (File.Exists(normalizedPath))
                {
                    return normalizedPath;
                }
            }
            catch (Exception)
            {
                // Continue to next path
            }
        }

        try
        {
            return Path.GetFullPath(possiblePaths[0]);
        }
        catch
        {
            return possiblePaths[0];
        }
    }

    private static async Task BuildCliProject()
    {
        try
        {
            Console.WriteLine("   Building CLI project...");
            
            var projectPaths = new[]
            {
                "ApplicationBuilderHelpers.Test.Cli/ApplicationBuilderHelpers.Test.Cli.csproj",
                "../ApplicationBuilderHelpers.Test.Cli/ApplicationBuilderHelpers.Test.Cli.csproj",
                "ApplicationBuilderHelpers.Test.Cli.csproj"
            };

            foreach (var projectPath in projectPaths)
            {
                if (File.Exists(projectPath))
                {
                    Console.WriteLine($"   Found project: {projectPath}");
                    var psi = new ProcessStartInfo
                    {
                        FileName = "dotnet",
                        Arguments = $"build \"{projectPath}\" -c Debug --verbosity minimal",
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
                        var output = await process.StandardOutput.ReadToEndAsync();
                        var error = await process.StandardError.ReadToEndAsync();
                        
                        Console.WriteLine($"   Build completed with exit code: {process.ExitCode}");
                        if (!string.IsNullOrEmpty(output))
                        {
                            Console.WriteLine($"   Output: {output}");
                        }
                        if (!string.IsNullOrEmpty(error))
                        {
                            Console.WriteLine($"   Error: {error}");
                        }
                        return;
                    }
                }
            }
            
            Console.WriteLine("   ⚠️ Could not find CLI project to build");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ❌ Build failed: {ex.Message}");
        }
    }
}

public class ComprehensiveTestSuite
{
    public static async Task<int> RunAllTests(string testExePath)
    {
        var testRunner = new TestRunner();
        
        // Pre-flight checks
        Console.WriteLine("🔧 Pre-flight Checks");
        Console.WriteLine("─────────────────────");
        await testRunner.RunTestAsync("Test Executable Basic Execution", () => TestExecutableBasicExecution(testExePath));
        Console.WriteLine();

        // Core functionality tests
        Console.WriteLine("📋 Core Functionality Tests");
        Console.WriteLine("─────────────────────────────");
        await testRunner.RunTestAsync("Global Help", () => TestGlobalHelp(testExePath));
        await testRunner.RunTestAsync("Version Check", () => TestVersion(testExePath));
        await testRunner.RunTestAsync("Empty Arguments", () => TestEmptyArguments(testExePath));
        Console.WriteLine();

        // Command discovery tests
        Console.WriteLine("🔍 Command Discovery Tests");
        Console.WriteLine("───────────────────────────");
        await testRunner.RunTestAsync("Build Command Help", () => TestBuildCommandHelp(testExePath));
        await testRunner.RunTestAsync("Test Command Help", () => TestTestCommandHelp(testExePath));
        await testRunner.RunTestAsync("Deploy Command Help", () => TestDeployCommandHelp(testExePath));
        await testRunner.RunTestAsync("Serve Command Help", () => TestServeCommandHelp(testExePath));
        await testRunner.RunTestAsync("Invalid Command", () => TestInvalidCommand(testExePath));
        Console.WriteLine();

        // Cascading subcommand tests
        Console.WriteLine("🔗 Cascading Subcommand Tests");
        Console.WriteLine("──────────────────────────────");
        await testRunner.RunTestAsync("Config Set Command Help", () => TestConfigSetCommandHelp(testExePath));
        await testRunner.RunTestAsync("Config Get Command Help", () => TestConfigGetCommandHelp(testExePath));
        await testRunner.RunTestAsync("Database Migrate Command Help", () => TestDatabaseMigrateCommandHelp(testExePath));
        Console.WriteLine();

        // Command execution tests
        Console.WriteLine("🚀 Command Execution Tests");
        Console.WriteLine("───────────────────────────");
        await testRunner.RunTestAsync("Build Command Valid Execution", () => TestBuildValidExecution(testExePath));
        await testRunner.RunTestAsync("Test Command Valid Execution", () => TestTestValidExecution(testExePath));
        await testRunner.RunTestAsync("Deploy Command Valid Execution", () => TestDeployValidExecution(testExePath));
        await testRunner.RunTestAsync("Config Set Execution", () => TestConfigSetExecution(testExePath));
        await testRunner.RunTestAsync("Config Get Execution", () => TestConfigGetExecution(testExePath));
        await testRunner.RunTestAsync("Database Migrate Execution", () => TestDatabaseMigrateExecution(testExePath));
        await testRunner.RunTestAsync("Serve Command Execution", () => TestServeCommandExecution(testExePath));
        Console.WriteLine();

        // Options parsing tests
        Console.WriteLine("⚙️ Options Parsing Tests");
        Console.WriteLine("─────────────────────────");
        await testRunner.RunTestAsync("Boolean Options", () => TestBooleanOptions(testExePath));
        await testRunner.RunTestAsync("Boolean Options with Values", () => TestBooleanOptionsWithValues(testExePath));
        await testRunner.RunTestAsync("Boolean Options with Equals", () => TestBooleanOptionsWithEquals(testExePath));
        await testRunner.RunTestAsync("String Options", () => TestStringOptions(testExePath));
        await testRunner.RunTestAsync("Integer Options", () => TestIntegerOptions(testExePath));
        await testRunner.RunTestAsync("Double Options", () => TestDoubleOptions(testExePath));
        await testRunner.RunTestAsync("Nullable Options", () => TestNullableOptions(testExePath));
        await testRunner.RunTestAsync("Environment Variable Options", () => TestEnvironmentVariableOptions(testExePath));
        Console.WriteLine();

        // Array options tests
        Console.WriteLine("📚 Array Options Tests");
        Console.WriteLine("───────────────────────");
        await testRunner.RunTestAsync("Single Array Value", () => TestArrayOptionsSingle(testExePath));
        await testRunner.RunTestAsync("Multiple Array Values", () => TestArrayOptionsMultiple(testExePath));
        await testRunner.RunTestAsync("Multiple Different Arrays", () => TestMultipleDifferentArrays(testExePath));
        await testRunner.RunTestAsync("Empty Array Handling", () => TestEmptyArrayHandling(testExePath));
        Console.WriteLine();

        // Validation tests
        Console.WriteLine("✔️ Validation Tests");
        Console.WriteLine("────────────────────");
        await testRunner.RunTestAsync("FromAmong Validation Valid", () => TestFromAmongValidationValid(testExePath));
        await testRunner.RunTestAsync("FromAmong Validation Invalid", () => TestFromAmongValidationInvalid(testExePath));
        await testRunner.RunTestAsync("Compact Short Option Valid", () => TestCompactShortOptionValidLogLevel(testExePath));
        await testRunner.RunTestAsync("Compact Short Option Invalid", () => TestCompactShortOptionInvalidLogLevel(testExePath));
        await testRunner.RunTestAsync("Framework Targeting", () => TestFrameworkTargeting(testExePath));
        await testRunner.RunTestAsync("Output Format Validation", () => TestOutputFormatValidation(testExePath));
        Console.WriteLine();

        // Error handling tests
        Console.WriteLine("❌ Error Handling Tests");
        Console.WriteLine("────────────────────────");
        await testRunner.RunTestAsync("Missing Required Arguments", () => TestMissingRequiredArguments(testExePath));
        await testRunner.RunTestAsync("Invalid Type Values", () => TestInvalidTypeValues(testExePath));
        await testRunner.RunTestAsync("Missing Required Options", () => TestMissingRequiredOptions(testExePath));
        await testRunner.RunTestAsync("Invalid Command Combinations", () => TestInvalidCommandCombinations(testExePath));
        Console.WriteLine();

        // Global options tests
        Console.WriteLine("🌐 Global Options Tests");
        Console.WriteLine("────────────────────────");
        await testRunner.RunTestAsync("Global Log Level Option", () => TestGlobalLogLevelOption(testExePath));
        await testRunner.RunTestAsync("Global Quiet Option", () => TestGlobalQuietOption(testExePath));
        await testRunner.RunTestAsync("Global Dry Run Option", () => TestGlobalDryRunOption(testExePath));
        await testRunner.RunTestAsync("Global Config File Option", () => TestGlobalConfigFileOption(testExePath));
        Console.WriteLine();

        // Help system tests
        Console.WriteLine("❓ Help System Tests");
        Console.WriteLine("────────────────────");
        await testRunner.RunTestAsync("Color Support", () => TestColorSupport(testExePath));
        await testRunner.RunTestAsync("Two Column Layout", () => TestTwoColumnLayout(testExePath));
        await testRunner.RunTestAsync("Default Values Display", () => TestDefaultValuesDisplay(testExePath));
        await testRunner.RunTestAsync("Environment Variables Display", () => TestEnvironmentVariablesDisplay(testExePath));
        await testRunner.RunTestAsync("Global Options in Command Help", () => TestGlobalOptionsInCommandHelp(testExePath));
        Console.WriteLine();

        // Advanced feature tests
        Console.WriteLine("🎯 Advanced Feature Tests");
        Console.WriteLine("──────────────────────────");
        await testRunner.RunTestAsync("Complex Command Combination", () => TestComplexCommandCombination(testExePath));
        await testRunner.RunTestAsync("Long Option Names", () => TestLongOptionNames(testExePath));
        await testRunner.RunTestAsync("Mixed Short and Long Options", () => TestMixedShortAndLongOptions(testExePath));
        await testRunner.RunTestAsync("Special Characters in Values", () => TestSpecialCharactersInValues(testExePath));
        Console.WriteLine();

        testRunner.PrintSummary();
        
        return testRunner.HasFailures ? 1 : 0;
    }

    #region Pre-flight Tests

    private static async Task<TestResult> TestExecutableBasicExecution(string testExePath)
    {
        try
        {
            var result = await RunTestExe(testExePath, "--version");
            var success = result.ExitCode == 0 && !string.IsNullOrEmpty(result.Output);
            
            return new TestResult
            {
                Success = success,
                Message = success ? "✅ Test executable runs successfully" : "❌ Test executable failed to run",
                Output = result.Output,
                Error = result.Error,
                ExitCode = result.ExitCode,
                Arguments = "--version"
            };
        }
        catch (Exception ex)
        {
            return new TestResult
            {
                Success = false,
                Message = $"❌ Exception during basic execution: {ex.Message}",
                Output = "",
                Error = ex.ToString(),
                ExitCode = -1,
                Arguments = "--version"
            };
        }
    }

    #endregion

    #region Core Functionality Tests

    private static async Task<TestResult> TestGlobalHelp(string testExePath)
    {
        var result = await RunTestExe(testExePath, "--help");
        
        var expectedContent = new []
        {
            "ApplicationBuilderHelpers Test CLI",
            "2.1.0",
            "USAGE:",
            "COMMANDS:",
            "build",
            "test",
            "deploy",
            "serve",
            "config",
            "database"
        };

        return ValidateOutput(result, 0, expectedContent, "Global help should display CLI information and all commands");
    }

    private static async Task<TestResult> TestVersion(string testExePath)
    {
        var result = await RunTestExe(testExePath, "--version");
        return ValidateOutput(result, 0, ["2.1.0"], "Version should be displayed");
    }

    private static async Task<TestResult> TestEmptyArguments(string testExePath)
    {
        var result = await RunTestExe(testExePath);
        
        // Should either show default command or help
        var success = result.ExitCode == 0 && !string.IsNullOrEmpty(result.Output);
        
        return new TestResult
        {
            Success = success,
            Message = success ? "✅ Empty arguments handled gracefully" : "❌ Empty arguments caused issues",
            Output = result.Output,
            Error = result.Error,
            ExitCode = result.ExitCode,
            Arguments = "(empty)"
        };
    }

    #endregion

    #region Command Discovery Tests

    private static async Task<TestResult> TestBuildCommandHelp(string testExePath)
    {
        var result = await RunTestExe(testExePath, "build", "--help");
        
        var expectedContent = new[]
        {
            "Build the project",
            "USAGE:",
            "OPTIONS:",
            "ARGUMENTS:",
            "project",
            "--release",
            "--output"
        };

        return ValidateOutput(result, 0, expectedContent, "Build command help should show options and arguments");
    }

    private static async Task<TestResult> TestTestCommandHelp(string testExePath)
    {
        var result = await RunTestExe(testExePath, "test", "--help");
        
        var expectedContent = new[]
        {
            "Run various test operations",
            "OPTIONS:",
            "--verbose",
            "--config",
            "--tags",
            "--coverage",
            "--framework"
        };

        return ValidateOutput(result, 0, expectedContent, "Test command help should display comprehensive options");
    }

    private static async Task<TestResult> TestDeployCommandHelp(string testExePath)
    {
        var result = await RunTestExe(testExePath, "deploy", "--help");
        
        var expectedContent = new[]
        {
            "Deploy applications",
            "environment",
            "--strategy",
            "--services",
            "--dry-run"
        };

        return ValidateOutput(result, 0, expectedContent, "Deploy command help should show deployment options");
    }

    private static async Task<TestResult> TestServeCommandHelp(string testExePath)
    {
        var result = await RunTestExe(testExePath, "serve", "--help");
        
        var expectedContent = new[]
        {
            "Start a development server",
            "--port",
            "--host",
            "--https",
            "--watch"
        };

        return ValidateOutput(result, 0, expectedContent, "Serve command help should show server options");
    }

    private static async Task<TestResult> TestInvalidCommand(string testExePath)
    {
        var result = await RunTestExe(testExePath, "invalid-command");
        // The error message is "No command found.\n" - adjust expectation
        return ValidateOutput(result, 1, ["No command found"], "Invalid command should return error");
    }

    #endregion

    #region Cascading Subcommand Tests

    private static async Task<TestResult> TestConfigSetCommandHelp(string testExePath)
    {
        var result = await RunTestExe(testExePath, "config", "set", "--help");
        
        var expectedContent = new[]
        {
            "Set configuration values",
            "key",
            "value",
            "--global",
            "--force"
        };

        return ValidateOutput(result, 0, expectedContent, "Config set command should show cascading subcommand structure");
    }

    private static async Task<TestResult> TestConfigGetCommandHelp(string testExePath)
    {
        var result = await RunTestExe(testExePath, "config", "get", "--help");
        
        var expectedContent = new[]
        {
            "Get configuration values",
            "--all",
            "--global",
            "--format"
        };

        return ValidateOutput(result, 0, expectedContent, "Config get command should show configuration retrieval options");
    }

    private static async Task<TestResult> TestDatabaseMigrateCommandHelp(string testExePath)
    {
        var result = await RunTestExe(testExePath, "database", "migrate", "--help");
        
        var expectedContent = new[]
        {
            "Run database migrations",
            "--target",
            "--script",
            "--backup"
        };

        return ValidateOutput(result, 0, expectedContent, "Database migrate command should show migration options");
    }

    #endregion

    #region Command Execution Tests

    private static async Task<TestResult> TestBuildValidExecution(string testExePath)
    {
        var result = await RunTestExe(testExePath, "build", "MyProject.csproj");
        
        var expectedContent = new[]
        {
            "Building project: MyProject.csproj"
        };

        return ValidateOutput(result, 0, expectedContent, "Build command should execute with required argument");
    }

    private static async Task<TestResult> TestTestValidExecution(string testExePath)
    {
        var result = await RunTestExe(testExePath, "test", "MyTarget", "--verbose");
        
        var expectedContent = new[]
        {
            "Running test on target: MyTarget"
        };

        return ValidateOutput(result, 0, expectedContent, "Test command should execute with arguments");
    }

    private static async Task<TestResult> TestDeployValidExecution(string testExePath)
    {
        var result = await RunTestExe(testExePath, "deploy", "production", "v1.2.3");
        
        var expectedContent = new[]
        {
            "Deploying to environment: production",
            "Version: v1.2.3"
        };

        return ValidateOutput(result, 0, expectedContent, "Deploy command should execute with environment and version");
    }

    private static async Task<TestResult> TestConfigSetExecution(string testExePath)
    {
        var result = await RunTestExe(testExePath, "config", "set", "user.name", "testuser");
        
        var expectedContent = new[]
        {
            "Setting config user.name = testuser"
        };

        return ValidateOutput(result, 0, expectedContent, "Config set should work with cascading subcommands");
    }

    private static async Task<TestResult> TestConfigGetExecution(string testExePath)
    {
        var result = await RunTestExe(testExePath, "config", "get", "user.name");
        
        var expectedContent = new[]
        {
            "Getting configuration for key: user.name"
        };

        return ValidateOutput(result, 0, expectedContent, "Config get should work with cascading subcommands");
    }

    private static async Task<TestResult> TestDatabaseMigrateExecution(string testExePath)
    {
        var result = await RunTestExe(testExePath, "database", "migrate");
        
        var expectedContent = new[]
        {
            "Running database migrations"
        };

        return ValidateOutput(result, 0, expectedContent, "Database migrate should execute successfully");
    }

    private static async Task<TestResult> TestServeCommandExecution(string testExePath)
    {
        var result = await RunTestExe(testExePath, "serve", "--port", "3000");
        
        var expectedContent = new[]
        {
            "Starting development server",
            "Port: 3000"
        };

        return ValidateOutput(result, 0, expectedContent, "Serve command should start with specified port");
    }

    #endregion

    #region Options Parsing Tests

    private static async Task<TestResult> TestBooleanOptions(string testExePath)
    {
        var result = await RunTestExe(testExePath, "test", "MyTarget", "--verbose", "--parallel");
        
        var expectedContent = new[]
        {
            "Running test on target: MyTarget",
            "Parallel: True"
        };

        return ValidateOutput(result, 0, expectedContent, "Boolean flags should be parsed correctly");
    }

    private static async Task<TestResult> TestBooleanOptionsWithValues(string testExePath)
    {
        var result = await RunTestExe(testExePath, "test", "MyTarget", "--verbose", "--parallel", "true");
        
        var expectedContent = new[]
        {
            "Running test on target: MyTarget",
            "Parallel: True"
        };

        return ValidateOutput(result, 0, expectedContent, "Boolean flags with values should be parsed correctly");
    }

    private static async Task<TestResult> TestBooleanOptionsWithEquals(string testExePath)
    {
        var result = await RunTestExe(testExePath, "test", "MyTarget", "--verbose", "--parallel=true");
        
        var expectedContent = new[]
        {
            "Running test on target: MyTarget",
            "Parallel: True"
        };

        return ValidateOutput(result, 0, expectedContent, "Boolean flags with equals syntax should be parsed correctly");
    }

    private static async Task<TestResult> TestStringOptions(string testExePath)
    {
        var result = await RunTestExe(testExePath, "test", "MyTarget", "--config", "config.xml", "--verbose");
        
        var expectedContent = new[]
        {
            "Running test on target: MyTarget",
            "Config: config.xml"
        };

        return ValidateOutput(result, 0, expectedContent, "String options should be parsed correctly");
    }

    private static async Task<TestResult> TestIntegerOptions(string testExePath)
    {
        var result = await RunTestExe(testExePath, "test", "MyTarget", "--timeout", "120", "--verbose");
        
        var expectedContent = new[]
        {
            "Running test on target: MyTarget",
            "Timeout: 120s"
        };

        return ValidateOutput(result, 0, expectedContent, "Integer options should be parsed correctly");
    }

    private static async Task<TestResult> TestDoubleOptions(string testExePath)
    {
        var result = await RunTestExe(testExePath, "test", "MyTarget", "--coverage-threshold", "85.5", "--verbose");
        
        var expectedContent = new[]
        {
            "Running test on target: MyTarget",
            "Coverage Threshold: 85.5%"
        };

        return ValidateOutput(result, 0, expectedContent, "Double options should be parsed correctly");
    }

    private static async Task<TestResult> TestNullableOptions(string testExePath)
    {
        var result = await RunTestExe(testExePath, "test", "MyTarget", "--seed", "42", "--verbose");
        
        var expectedContent = new[]
        {
            "Running test on target: MyTarget",
            "Random Seed: 42"
        };

        return ValidateOutput(result, 0, expectedContent, "Nullable integer options should be parsed correctly");
    }

    private static async Task<TestResult> TestEnvironmentVariableOptions(string testExePath)
    {
        // Set environment variable
        Environment.SetEnvironmentVariable("TEST_CONFIG", "env-config.json");
        
        var result = await RunTestExe(testExePath, "test", "MyTarget", "--verbose");
        
        var expectedContent = new[]
        {
            "Running test on target: MyTarget",
            "Config: env-config.json"
        };

        // Clean up
        Environment.SetEnvironmentVariable("TEST_CONFIG", null);

        return ValidateOutput(result, 0, expectedContent, "Environment variable options should be used as defaults");
    }

    #endregion

    #region Array Options Tests

    private static async Task<TestResult> TestArrayOptionsSingle(string testExePath)
    {
        var result = await RunTestExe(testExePath, "test", "MyTarget", "--tags", "unit", "--verbose");
        
        var expectedContent = new[]
        {
            "Running test on target: MyTarget",
            "Tags: unit"
        };

        return ValidateOutput(result, 0, expectedContent, "Single array option should be parsed correctly");
    }

    private static async Task<TestResult> TestArrayOptionsMultiple(string testExePath)
    {
        var result = await RunTestExe(testExePath, "test", "MyTarget", "--tags", "unit", "--tags", "integration", "--verbose");
        
        var expectedContent = new[]
        {
            "Running test on target: MyTarget",
            "Tags: unit, integration"
        };

        return ValidateOutput(result, 0, expectedContent, "Multiple array options should be collected correctly");
    }

    private static async Task<TestResult> TestMultipleDifferentArrays(string testExePath)
    {
        var result = await RunTestExe(testExePath, "test", "MyTarget", "--tags", "unit", "--exclude", "slow", "--tags", "integration", "--verbose");
        
        var expectedContent = new[]
        {
            "Running test on target: MyTarget",
            "Tags: unit, integration",
            "Exclude: slow"
        };

        return ValidateOutput(result, 0, expectedContent, "Multiple different array types should work together");
    }

    private static async Task<TestResult> TestEmptyArrayHandling(string testExePath)
    {
        var result = await RunTestExe(testExePath, "test", "MyTarget", "--verbose");
        
        var success = result.ExitCode == 0;
        
        return new TestResult
        {
            Success = success,
            Message = success ? "✅ Empty arrays handled correctly" : "❌ Empty arrays caused issues",
            Output = result.Output,
            Error = result.Error,
            ExitCode = result.ExitCode,
            Arguments = "test MyTarget --verbose"
        };
    }

    #endregion

    #region Validation Tests

    private static async Task<TestResult> TestFromAmongValidationValid(string testExePath)
    {
        var result = await RunTestExe(testExePath, "build", "MyProject.csproj", "--target", "Release");
        
        var expectedContent = new[]
        {
            "Building project: MyProject.csproj",
            "Target: Release"
        };

        return ValidateOutput(result, 0, expectedContent, "Valid FromAmong option should be accepted");
    }

    private static async Task<TestResult> TestFromAmongValidationInvalid(string testExePath)
    {
        var result = await RunTestExe(testExePath, "build", "MyProject.csproj", "--target", "InvalidTarget");
        
        var expectedContent = new[]
        {
            "not valid",
            "Must be one of"
        };

        return ValidateOutput(result, 1, expectedContent, "Invalid FromAmong option should be rejected");
    }

    private static async Task<TestResult> TestCompactShortOptionValidLogLevel(string testExePath)
    {
        var result = await RunTestExe(testExePath, "test", "MyTarget", "-ldebug", "--verbose");
        
        var expectedContent = new[]
        {
            "Running test on target: MyTarget"
        };

        return ValidateOutput(result, 0, expectedContent, "Compact short option with value (-ldebug) should be parsed correctly");
    }

    private static async Task<TestResult> TestCompactShortOptionInvalidLogLevel(string testExePath)
    {
        var result = await RunTestExe(testExePath, "test", "MyTarget", "-linvalid");
        
        var expectedContent = new[]
        {
            "not valid",
            "Must be one of"
        };

        return ValidateOutput(result, 1, expectedContent, "Invalid compact short option value (-linvalid) should be rejected");
    }

    private static async Task<TestResult> TestFrameworkTargeting(string testExePath)
    {
        var result = await RunTestExe(testExePath, "test", "MyTarget", "--framework", "net9.0", "--verbose");
        
        var expectedContent = new[]
        {
            "Framework: net9.0"
        };

        return ValidateOutput(result, 0, expectedContent, "Framework targeting should work with valid values");
    }

    private static async Task<TestResult> TestOutputFormatValidation(string testExePath)
    {
        var result = await RunTestExe(testExePath, "test", "MyTarget", "--output-format", "json", "--verbose");
        
        var expectedContent = new[]
        {
            "Output Format: json"
        };

        return ValidateOutput(result, 0, expectedContent, "Output format validation should accept valid formats");
    }

    #endregion

    #region Error Handling Tests

    private static async Task<TestResult> TestMissingRequiredArguments(string testExePath)
    {
        var result = await RunTestExe(testExePath, "build");
        return ValidateOutput(result, 1, ["required", "project"], "Missing required arguments should produce error");
    }

    private static async Task<TestResult> TestInvalidTypeValues(string testExePath)
    {
        var result = await RunTestExe(testExePath, "test", "MyTarget", "--timeout", "invalid");
        return ValidateOutput(result, 1, ["Invalid", "timeout"], "Invalid type values should produce error");
    }

    private static async Task<TestResult> TestMissingRequiredOptions(string testExePath)
    {
        var result = await RunTestExe(testExePath, "deploy");
        return ValidateOutput(result, 1, ["required", "environment"], "Missing required options should produce error");
    }

    private static async Task<TestResult> TestInvalidCommandCombinations(string testExePath)
    {
        var result = await RunTestExe(testExePath, "config", "invalid-subcommand");
        return ValidateOutput(result, 1, ["No command found"], "Invalid command combinations should produce error");
    }

    #endregion

    #region Global Options Tests

    private static async Task<TestResult> TestGlobalLogLevelOption(string testExePath)
    {
        var result = await RunTestExe(testExePath, "--log-level", "debug", "test", "MyTarget");
        
        var success = result.ExitCode == 0;
        
        return new TestResult
        {
            Success = success,
            Message = success ? "✅ Global log level option works" : "❌ Global log level option failed",
            Output = result.Output,
            Error = result.Error,
            ExitCode = result.ExitCode,
            Arguments = "--log-level debug test MyTarget"
        };
    }

    private static async Task<TestResult> TestGlobalQuietOption(string testExePath)
    {
        var result = await RunTestExe(testExePath, "--quiet", "test", "MyTarget");
        
        var success = result.ExitCode == 0;
        
        return new TestResult
        {
            Success = success,
            Message = success ? "✅ Global quiet option works" : "❌ Global quiet option failed",
            Output = result.Output,
            Error = result.Error,
            ExitCode = result.ExitCode,
            Arguments = "--quiet test MyTarget"
        };
    }

    private static async Task<TestResult> TestGlobalDryRunOption(string testExePath)
    {
        var result = await RunTestExe(testExePath, "--dry-run", "build", "MyProject.csproj");
        
        var success = result.ExitCode == 0;
        
        return new TestResult
        {
            Success = success,
            Message = success ? "✅ Global dry run option works" : "❌ Global dry run option failed",
            Output = result.Output,
            Error = result.Error,
            ExitCode = result.ExitCode,
            Arguments = "--dry-run build MyProject.csproj"
        };
    }

    private static async Task<TestResult> TestGlobalConfigFileOption(string testExePath)
    {
        var result = await RunTestExe(testExePath, "--config-file", "custom-config.json", "test", "MyTarget");
        
        var success = result.ExitCode == 0;
        
        return new TestResult
        {
            Success = success,
            Message = success ? "✅ Global config file option works" : "❌ Global config file option failed",
            Output = result.Output,
            Error = result.Error,
            ExitCode = result.ExitCode,
            Arguments = "--config-file custom-config.json test MyTarget"
        };
    }

    #endregion

    #region Help System Tests

    private static async Task<TestResult> TestColorSupport(string testExePath)
    {
        var result = await RunTestExe(testExePath, "--help");
        
        // Check for ANSI color codes in output
        var hasColors = result.Output.Contains("\u001b[") || result.Output.Contains("38;2;");
        
        return new TestResult
        {
            Success = hasColors,
            Message = hasColors ? "✅ Help output contains color codes" : "❌ Help output missing color support",
            Output = result.Output,
            Error = result.Error,
            ExitCode = result.ExitCode,
            Arguments = "--help"
        };
    }

    private static async Task<TestResult> TestTwoColumnLayout(string testExePath)
    {
        var result = await RunTestExe(testExePath, "build", "--help");
        
        var expectedContent = new[]
        {
            "--release",
            "Build in release mode",
            "--output",
            "Output directory"
        };

        return ValidateOutput(result, 0, expectedContent, "Help should display in proper two-column layout");
    }

    private static async Task<TestResult> TestDefaultValuesDisplay(string testExePath)
    {
        var result = await RunTestExe(testExePath, "test", "--help");
        
        var expectedContent = new[]
        {
            "Default:",
            "console",
            "80.0"
        };

        return ValidateOutput(result, 0, expectedContent, "Help should display default values");
    }

    private static async Task<TestResult> TestEnvironmentVariablesDisplay(string testExePath)
    {
        var result = await RunTestExe(testExePath, "test", "--help");
        
        var expectedContent = new[]
        {
            "Environment variable:",
            "TEST_CONFIG"
        };

        return ValidateOutput(result, 0, expectedContent, "Help should display environment variable information");
    }

    private static async Task<TestResult> TestGlobalOptionsInCommandHelp(string testExePath)
    {
        var result = await RunTestExe(testExePath, "build", "--help");
        
        var expectedContent = new[]
        {
            "GLOBAL OPTIONS:",
            "--log-level",
            "--quiet",
            "--dry-run"
        };

        return ValidateOutput(result, 0, expectedContent, "Command help should display global options");
    }

    #endregion

    #region Advanced Feature Tests

    private static async Task<TestResult> TestComplexCommandCombination(string testExePath)
    {
        var result = await RunTestExe(testExePath, "test", "MyTarget", 
            "--verbose", "--parallel", "--coverage", 
            "--tags", "unit", "--tags", "integration",
            "--framework", "net9.0", "--timeout", "300");
        
        var expectedContent = new[]
        {
            "Running test on target: MyTarget",
            "Parallel: True",
            "Coverage Enabled: True",
            "Tags: unit, integration",
            "Framework: net9.0",
            "Timeout: 300s"
        };

        return ValidateOutput(result, 0, expectedContent, "Complex command combinations should work correctly");
    }

    private static async Task<TestResult> TestLongOptionNames(string testExePath)
    {
        var result = await RunTestExe(testExePath, "test", "MyTarget", "--coverage-threshold", "90.0", "--verbose");
        
        var expectedContent = new[]
        {
            "Coverage Threshold: 90%"
        };

        return ValidateOutput(result, 0, expectedContent, "Long option names should be parsed correctly");
    }

    private static async Task<TestResult> TestMixedShortAndLongOptions(string testExePath)
    {
        var result = await RunTestExe(testExePath, "test", "MyTarget", "-v", "--parallel", "-t", "unit", "--framework", "net9.0");
        
        var expectedContent = new[]
        {
            "Running test on target: MyTarget",
            "Parallel: True",
            "Tags: unit",
            "Framework: net9.0"
        };

        return ValidateOutput(result, 0, expectedContent, "Mixed short and long options should work together");
    }

    private static async Task<TestResult> TestSpecialCharactersInValues(string testExePath)
    {
        var result = await RunTestExe(testExePath, "test", "My Target With Spaces", "--config", "path/with-special_chars.json", "--verbose");
        
        var expectedContent = new[]
        {
            "Running test on target: My Target With Spaces",
            "Config: path/with-special_chars.json"
        };

        return ValidateOutput(result, 0, expectedContent, "Special characters in values should be handled correctly");
    }

    #endregion

    #region Helper Methods

    private static async Task<ProcessResult> RunTestExe(string testExePath, params string[] arguments)
    {
        try
        {
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

            if (!await Task.Run(() => process.WaitForExit(30000))) // Increased timeout
            {
                try { process.Kill(); } catch { }
                return new ProcessResult
                {
                    ExitCode = -1,
                    Output = output.ToString(),
                    Error = "Process timed out after 30 seconds",
                    Arguments = string.Join(" ", arguments)
                };
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

    private static TestResult ValidateOutput(ProcessResult result, int expectedExitCode, string[] expectedContent, string description)
    {
        var success = true;
        var message = new StringBuilder();

        if (result.ExitCode != expectedExitCode)
        {
            success = false;
            message.AppendLine($"❌ Expected exit code {expectedExitCode}, got {result.ExitCode}");
        }

        var missingContent = new List<string>();
        foreach (var expected in expectedContent)
        {
            if (!result.Output.Contains(expected, StringComparison.OrdinalIgnoreCase))
            {
                success = false;
                missingContent.Add(expected);
            }
        }

        if (missingContent.Count > 0)
        {
            message.AppendLine($"❌ Missing expected content: {string.Join(", ", missingContent.Select(c => $"'{c}'"))}");
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

    #endregion
}

public class TestRunner
{
    private readonly List<TestResult> _results = new();

    public bool HasFailures => _results.Any(r => !r.Success);

    public async Task RunTestAsync(string testName, Func<Task<TestResult>> testFunc)
    {
        Console.Write($"🔍 {testName,-50} ");
        
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
                if (!string.IsNullOrEmpty(result.Output) && result.Output.Length < 500)
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
    }

    public void PrintSummary()
    {
        var passed = _results.Count(r => r.Success);
        var failed = _results.Count(r => !r.Success);
        
        Console.WriteLine();
        Console.WriteLine("📊 Test Summary");
        Console.WriteLine("═══════════════");
        Console.WriteLine($"✅ Passed: {passed}");
        Console.WriteLine($"❌ Failed: {failed}");
        Console.WriteLine($"📋 Total:  {_results.Count}");
        Console.WriteLine($"📈 Success Rate: {(double)passed / _results.Count * 100:F1}%");
        Console.WriteLine();
        
        if (HasFailures)
        {
            Console.WriteLine("❌ Some tests failed! Check the output above for details.");
            Console.WriteLine();
            Console.WriteLine("Failed Tests:");
            foreach (var failedTest in _results.Where(r => !r.Success))
            {
                Console.WriteLine($"  • {failedTest.Arguments} - {failedTest.Message.Split('\n')[0]}");
            }
        }
        else
        {
            Console.WriteLine("🎉 All tests passed!");
        }
        
        Console.WriteLine();
        Console.WriteLine("💡 Manual Testing Commands:");
        Console.WriteLine("   test.exe --help");
        Console.WriteLine("   test.exe --version");
        Console.WriteLine("   test.exe build --help");
        Console.WriteLine("   test.exe build MyProject.csproj --release");
        Console.WriteLine("   test.exe test MyTarget --verbose --parallel");
        Console.WriteLine("   test.exe deploy production v1.0.0 --strategy blue-green");
        Console.WriteLine("   test.exe config set user.name testuser --global");
        Console.WriteLine("   test.exe config get --all --format json");
        Console.WriteLine("   test.exe database migrate --dry-run");
        Console.WriteLine("   test.exe serve --port 3000 --https");
        Console.WriteLine();
        Console.WriteLine("🔧 Advanced Testing Commands:");
        Console.WriteLine("   test.exe test MyTarget --tags unit --tags integration --framework net9.0");
        Console.WriteLine("   test.exe --log-level debug --dry-run build MyProject.csproj");
        Console.WriteLine("   test.exe deploy staging --services api --services web --env-vars PORT=8080");
        Console.WriteLine();
        Console.WriteLine("🚩 Boolean Option Testing Commands:");
        Console.WriteLine("   test.exe test MyTarget --diag                    # Sets diag=true (flag mode)");
        Console.WriteLine("   test.exe test MyTarget --diag=false              # Sets diag=false (equals syntax)");
        Console.WriteLine("   test.exe test MyTarget --diag false              # Sets diag=false (space syntax)");
        Console.WriteLine("   test.exe test MyTarget --diag true               # Sets diag=true (space syntax)");
        Console.WriteLine("   test.exe test MyTarget --verbose --diag=yes      # Multiple boolean options");
        Console.WriteLine("   test.exe test MyTarget --parallel=no --diag=1    # Various boolean values");
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

