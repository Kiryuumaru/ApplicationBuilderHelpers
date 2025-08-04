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
        // Always print debug info first if enabled
        PrintDebugInfo();

        Console.WriteLine($"Running test on target: {Target ?? "default"}");
        
        if (Verbose)
        {
            Console.WriteLine("===============================================");
            Console.WriteLine("[CFG] DETAILED TEST CONFIGURATION");
            Console.WriteLine("===============================================");
            Console.WriteLine();

            // Core Configuration
            Console.WriteLine("[CORE] Core Configuration:");
            Console.WriteLine($"   Config: {ConfigPath ?? "default"}");
            Console.WriteLine($"   Timeout: {Timeout}s");
            Console.WriteLine($"   Parallel: {Parallel}");
            Console.WriteLine($"   Diagnostic Mode: {DiagnosticMode}");
            Console.WriteLine();

            // Output Configuration
            Console.WriteLine("[OUT] Output Configuration:");
            Console.WriteLine($"   Output Format: {OutputFormat}");
            if (!string.IsNullOrEmpty(ResultsDirectory))
            {
                Console.WriteLine($"   Results Directory: {ResultsDirectory}");
            }
            if (Loggers.Length > 0)
            {
                Console.WriteLine($"   Loggers: {string.Join(", ", Loggers)}");
            }
            Console.WriteLine();

            // Coverage Configuration
            Console.WriteLine("[COV] Coverage Configuration:");
            Console.WriteLine($"   Coverage Enabled: {EnableCoverage}");
            Console.WriteLine($"   Coverage Threshold: {CoverageThreshold}%");
            Console.WriteLine();

            // Reliability Configuration
            Console.WriteLine("[REL] Reliability Configuration:");
            Console.WriteLine($"   Retry Count: {RetryCount}");
            Console.WriteLine($"   Blame Mode: {BlameMode}");
            Console.WriteLine($"   Blame Crash: {BlameCrash}");
            Console.WriteLine($"   Blame Hang: {BlameHang}");
            Console.WriteLine($"   Blame Hang Timeout: {BlameHangTimeout} minutes");
            Console.WriteLine();

            // Runtime Configuration
            if (!string.IsNullOrEmpty(Framework) || !string.IsNullOrEmpty(Runtime) || MaxCpuCount.HasValue)
            {
                Console.WriteLine("[RUN] Runtime Configuration:");
                if (!string.IsNullOrEmpty(Framework))
                {
                    Console.WriteLine($"   Framework: {Framework}");
                }
                if (!string.IsNullOrEmpty(Runtime))
                {
                    Console.WriteLine($"   Runtime: {Runtime}");
                }
                if (MaxCpuCount.HasValue)
                {
                    Console.WriteLine($"   Max CPU Count: {MaxCpuCount}");
                }
                Console.WriteLine();
            }

            // Test Selection
            if (Tags.Length > 0 || ExcludePatterns.Length > 0 || !string.IsNullOrEmpty(Filter) || Seed.HasValue)
            {
                Console.WriteLine("[SEL] Test Selection:");
                if (Tags.Length > 0)
                {
                    Console.WriteLine($"   Tags: {string.Join(", ", Tags)}");
                }
                if (ExcludePatterns.Length > 0)
                {
                    Console.WriteLine($"   Exclude: {string.Join(", ", ExcludePatterns)}");
                }
                if (!string.IsNullOrEmpty(Filter))
                {
                    Console.WriteLine($"   Filter: {Filter}");
                }
                if (Seed.HasValue)
                {
                    Console.WriteLine($"   Random Seed: {Seed}");
                }
                Console.WriteLine();
            }

            // Data Collection
            if (DataCollectors.Length > 0)
            {
                Console.WriteLine("[DAT] Data Collection:");
                Console.WriteLine($"   Data Collectors: {string.Join(", ", DataCollectors)}");
                Console.WriteLine();
            }

            // Additional Settings
            if (!string.IsNullOrEmpty(SettingsFile))
            {
                Console.WriteLine("[SET] Additional Settings:");
                Console.WriteLine($"   Settings File: {SettingsFile}");
                Console.WriteLine();
            }

            // Global Options from Base
            Console.WriteLine("[GLB] Global Options:");
            Console.WriteLine($"   Log Level: {LogLevel}");
            Console.WriteLine($"   Quiet Mode: {Quiet}");
            if (EnvironmentVariables.Length > 0)
            {
                Console.WriteLine($"   Environment Variables: {string.Join(", ", EnvironmentVariables)}");
            }
            Console.WriteLine();

            Console.WriteLine("===============================================");
        }

        // Print summary of all non-default values
        Console.WriteLine();
        Console.WriteLine("[SUM] PARSED OPTIONS SUMMARY:");
        Console.WriteLine("-----------------------------------------------");
        PrintOptionsSummary();

        cancellationTokenSource.Cancel(); // Cancel the application host to stop further processing

        return ValueTask.CompletedTask;
    }

    private void PrintOptionsSummary()
    {
        var nonDefaultOptions = new List<string>();

        // Check all properties for non-default values
        if (Verbose) nonDefaultOptions.Add("verbose=true");
        if (!string.IsNullOrEmpty(ConfigPath)) nonDefaultOptions.Add($"config=\"{ConfigPath}\"");
        if (Timeout != 30) nonDefaultOptions.Add($"timeout={Timeout}");
        if (Parallel) nonDefaultOptions.Add("parallel=true");
        if (Tags.Length > 0) nonDefaultOptions.Add($"tags=[{string.Join(", ", Tags)}]");
        if (ExcludePatterns.Length > 0) nonDefaultOptions.Add($"exclude=[{string.Join(", ", ExcludePatterns)}]");
        if (!string.IsNullOrEmpty(Filter)) nonDefaultOptions.Add($"filter=\"{Filter}\"");
        if (OutputFormat != "console") nonDefaultOptions.Add($"output-format={OutputFormat}");
        if (EnableCoverage) nonDefaultOptions.Add("coverage=true");
        if (Math.Abs(CoverageThreshold - 80.0) > 0.001) nonDefaultOptions.Add($"coverage-threshold={CoverageThreshold}");
        if (RetryCount > 0) nonDefaultOptions.Add($"retry-count={RetryCount}");
        if (Seed.HasValue) nonDefaultOptions.Add($"seed={Seed}");
        if (DataCollectors.Length > 0) nonDefaultOptions.Add($"collect=[{string.Join(", ", DataCollectors)}]");
        if (BlameMode) nonDefaultOptions.Add("blame=true");
        if (BlameCrash) nonDefaultOptions.Add("blame-crash=true");
        if (BlameHang) nonDefaultOptions.Add("blame-hang=true");
        if (BlameHangTimeout != 5) nonDefaultOptions.Add($"blame-hang-timeout={BlameHangTimeout}");
        if (!string.IsNullOrEmpty(ResultsDirectory)) nonDefaultOptions.Add($"results-directory=\"{ResultsDirectory}\"");
        if (Loggers.Length > 0) nonDefaultOptions.Add($"logger=[{string.Join(", ", Loggers)}]");
        if (!string.IsNullOrEmpty(Framework)) nonDefaultOptions.Add($"framework={Framework}");
        if (!string.IsNullOrEmpty(Runtime)) nonDefaultOptions.Add($"runtime={Runtime}");
        if (MaxCpuCount.HasValue) nonDefaultOptions.Add($"max-cpu-count={MaxCpuCount}");
        if (DiagnosticMode) nonDefaultOptions.Add("diag=true");
        if (!string.IsNullOrEmpty(SettingsFile)) nonDefaultOptions.Add($"settings=\"{SettingsFile}\"");
        if (!string.IsNullOrEmpty(Target)) nonDefaultOptions.Add($"target=\"{Target}\"");

        // Base class options
        if (LogLevel != "information") nonDefaultOptions.Add($"log-level={LogLevel}");
        if (Quiet) nonDefaultOptions.Add("quiet=true");
        if (EnvironmentVariables.Length > 0) nonDefaultOptions.Add($"env=[{string.Join(", ", EnvironmentVariables)}]");
        if (DebugParser) nonDefaultOptions.Add("debug-parser=true");

        if (nonDefaultOptions.Count == 0)
        {
            Console.WriteLine("   (All options are at their default values)");
        }
        else
        {
            foreach (var option in nonDefaultOptions)
            {
                Console.WriteLine($"   {option}");
            }
        }
        Console.WriteLine();
    }
}
