using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ApplicationBuilderHelpers.Test.Playground.TestFramework;

namespace ApplicationBuilderHelpers.Test.Playground;

class MainProgram
{
    static async Task<int> Main(string[] args)
    {
        Console.WriteLine("ApplicationBuilderHelpers Test Playground");
        Console.WriteLine("=========================================\n");

        // Simple test to make sure basic functionality works
        var result = await BasicCliTest();
        return result ? 0 : 1;
    }

    static async Task<bool> BasicCliTest()
    {
        var testExePath = FindTestExecutable();
        if (testExePath == null)
        {
            Console.WriteLine("? Could not find test.exe");
            return false;
        }

        Console.WriteLine($"?? Found test executable: {testExePath}");
        
        try
        {
            var runner = new CliTestRunner(testExePath);
            
            // Test 1: Basic help
            var helpResult = await runner.RunAsync("--help");
            if (helpResult.ExitCode == 0 && helpResult.StandardOutput.Contains("USAGE"))
            {
                Console.WriteLine("? Help test passed");
            }
            else
            {
                Console.WriteLine("? Help test failed");
                return false;
            }

            // Test 2: Version
            var versionResult = await runner.RunAsync("--version");
            if (versionResult.ExitCode == 0)
            {
                Console.WriteLine("? Version test passed");
            }
            else
            {
                Console.WriteLine("? Version test failed");
                return false;
            }

            // Test 3: Test command
            var testResult = await runner.RunAsync("test", "mytarget", "--verbose");
            if (testResult.ExitCode == 0 && testResult.StandardOutput.Contains("Running test on target: mytarget"))
            {
                Console.WriteLine("? Test command passed");
            }
            else
            {
                Console.WriteLine("? Test command failed");
                Console.WriteLine($"Output: {testResult.StandardOutput}");
                Console.WriteLine($"Error: {testResult.StandardError}");
                return false;
            }

            Console.WriteLine("\n? All basic tests passed!");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"? Test failed with exception: {ex.Message}");
            return false;
        }
    }

    static string? FindTestExecutable()
    {
        var paths = new[]
        {
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", 
                "ApplicationBuilderHelpers.Test.Cli", "bin", "Debug", "net9.0", "test.exe"),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", 
                "ApplicationBuilderHelpers.Test.Cli", "bin", "Release", "net9.0", "test.exe"),
        };

        foreach (var path in paths)
        {
            var fullPath = Path.GetFullPath(path);
            if (File.Exists(fullPath))
                return fullPath;
        }

        return null;
    }
}