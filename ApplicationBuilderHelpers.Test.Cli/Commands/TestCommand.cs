using ApplicationBuilderHelpers.Attributes;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApplicationBuilderHelpers.Test.Cli.Commands;

[Command("test", "Run various test operations")]
internal class TestCommand : BaseCommand
{
    [CommandOption('v', "verbose", Description = "Enable verbose output")]
    public bool Verbose { get; set; }

    [CommandOption('c', "config", Description = "Configuration file path", EnvironmentVariable = "TEST_CONFIG")]
    public string? ConfigPath { get; set; }

    [CommandOption("timeout", Description = "Timeout in seconds")]
    public int Timeout { get; set; } = 30;

    [CommandOption("parallel", Description = "Run tests in parallel")]
    public bool Parallel { get; set; }

    [CommandOption('t', "tags", Description = "Test tags to include")]
    public string[] Tags { get; set; } = [];

    [CommandOption('e', "exclude", Description = "Test patterns to exclude")]
    public string[] ExcludePatterns { get; set; } = [];

    [CommandOption("filter", Description = "Test filter expression")]
    public string? Filter { get; set; }

    [CommandOption("output-format", Description = "Output format", FromAmong = ["json", "xml", "junit", "console"])]
    public string OutputFormat { get; set; } = "console";

    [CommandOption("coverage", Description = "Enable code coverage")]
    public bool EnableCoverage { get; set; }

    [CommandOption("coverage-threshold", Description = "Minimum coverage percentage")]
    public double CoverageThreshold { get; set; } = 80.0;

    [CommandOption("retry-count", Description = "Number of retries for failed tests")]
    public int RetryCount { get; set; } = 0;

    [CommandOption("seed", Description = "Random seed for test ordering")]
    public int? Seed { get; set; }

    [CommandOption("collect", Description = "Data collectors to enable")]
    public string[] DataCollectors { get; set; } = [];

    [CommandOption("blame", Description = "Enable blame mode for crash diagnostics")]
    public bool BlameMode { get; set; }

    [CommandOption("blame-crash", Description = "Collect crash dump on test host crash")]
    public bool BlameCrash { get; set; }

    [CommandOption("blame-hang", Description = "Collect hang dump on test timeout")]
    public bool BlameHang { get; set; }

    [CommandOption("blame-hang-timeout", Description = "Timeout for hang detection in minutes")]
    public int BlameHangTimeout { get; set; } = 5;

    [CommandOption("results-directory", Description = "Directory to store test results", EnvironmentVariable = "TEST_RESULTS_DIR")]
    public string? ResultsDirectory { get; set; }

    [CommandOption("logger", Description = "Logger configuration")]
    public string[] Loggers { get; set; } = [];

    [CommandOption("framework", Description = "Target framework for tests", FromAmong = ["net6.0", "net7.0", "net8.0", "net9.0"])]
    public string? Framework { get; set; }

    [CommandOption("runtime", Description = "Target runtime for tests")]
    public string? Runtime { get; set; }

    [CommandOption("max-cpu-count", Description = "Maximum CPU count to use")]
    public int? MaxCpuCount { get; set; }

    [CommandOption("diag", Description = "Enable diagnostic mode")]
    public bool DiagnosticMode { get; set; }

    [CommandOption("settings", Description = "Settings file for test run")]
    public string? SettingsFile { get; set; }

    [CommandArgument("target", Description = "Target to test", Position = 0, Required = false)]
    public string? Target { get; set; }

    protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
    {
        Console.WriteLine($"Running test on target: {Target ?? "default"}");
        
        if (Verbose)
        {
            Console.WriteLine($"Config: {ConfigPath ?? "default"}");
            Console.WriteLine($"Timeout: {Timeout}s");
            Console.WriteLine($"Parallel: {Parallel}");
            Console.WriteLine($"Output Format: {OutputFormat}");
            Console.WriteLine($"Coverage Enabled: {EnableCoverage}");
            Console.WriteLine($"Coverage Threshold: {CoverageThreshold}%");
            Console.WriteLine($"Retry Count: {RetryCount}");
            Console.WriteLine($"Blame Mode: {BlameMode}");
            Console.WriteLine($"Blame Crash: {BlameCrash}");
            Console.WriteLine($"Blame Hang: {BlameHang}");
            Console.WriteLine($"Blame Hang Timeout: {BlameHangTimeout} minutes");
            Console.WriteLine($"Diagnostic Mode: {DiagnosticMode}");
            
            if (Seed.HasValue)
            {
                Console.WriteLine($"Random Seed: {Seed}");
            }
            
            if (!string.IsNullOrEmpty(Filter))
            {
                Console.WriteLine($"Filter: {Filter}");
            }
            
            if (!string.IsNullOrEmpty(Framework))
            {
                Console.WriteLine($"Framework: {Framework}");
            }
            
            if (!string.IsNullOrEmpty(Runtime))
            {
                Console.WriteLine($"Runtime: {Runtime}");
            }
            
            if (!string.IsNullOrEmpty(ResultsDirectory))
            {
                Console.WriteLine($"Results Directory: {ResultsDirectory}");
            }
            
            if (!string.IsNullOrEmpty(SettingsFile))
            {
                Console.WriteLine($"Settings File: {SettingsFile}");
            }
            
            if (MaxCpuCount.HasValue)
            {
                Console.WriteLine($"Max CPU Count: {MaxCpuCount}");
            }
            
            if (Tags.Length > 0)
            {
                Console.WriteLine($"Tags: {string.Join(", ", Tags)}");
            }
            
            if (ExcludePatterns.Length > 0)
            {
                Console.WriteLine($"Exclude: {string.Join(", ", ExcludePatterns)}");
            }
            
            if (DataCollectors.Length > 0)
            {
                Console.WriteLine($"Data Collectors: {string.Join(", ", DataCollectors)}");
            }
            
            if (Loggers.Length > 0)
            {
                Console.WriteLine($"Loggers: {string.Join(", ", Loggers)}");
            }
        }

        cancellationTokenSource.Cancel(); // Cancel the application host to stop further processing

        return ValueTask.CompletedTask;
    }
}
