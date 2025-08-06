using ApplicationBuilderHelpers.Attributes;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApplicationBuilderHelpers.Test.Cli.Commands;

[Command(description: "Default command for ApplicationBuilderHelpers Test CLI")]
internal class MainCommand : BaseCommand
{
    [CommandOption('v', "verbose", Description = "Enable verbose output")]
    public bool Verbose { get; set; }

    [CommandOption('c', "config", Description = "Configuration file path", EnvironmentVariable = "TEST_CONFIG")]
    public string? ConfigPath { get; set; }

    [CommandOption("timeout", Description = "Timeout in seconds")]
    public int Timeout { get; set; } = 30;

    protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
    {
        // Always print debug info first if enabled
        PrintDebugInfo();

        Console.WriteLine("ApplicationBuilderHelpers Test CLI - Default Command");
        
        if (Verbose)
        {
            Console.WriteLine();
            Console.WriteLine("===============================================");
            Console.WriteLine("[CFG] MAIN COMMAND CONFIGURATION");
            Console.WriteLine("===============================================");
            Console.WriteLine();
            
            Console.WriteLine("[CORE] Configuration:");
            Console.WriteLine($"   Config: {ConfigPath ?? "default"}");
            Console.WriteLine($"   Timeout: {Timeout}s");
            Console.WriteLine($"   Verbose: {Verbose}");
            Console.WriteLine();
            
            Console.WriteLine("[GLB] Global Options:");
            Console.WriteLine($"   Log Level: {LogLevel}");
            Console.WriteLine($"   Quiet Mode: {Quiet}");
            if (EnvironmentVariables.Length > 0)
            {
                Console.WriteLine($"   Environment Variables: {string.Join(", ", EnvironmentVariables)}");
            }
            Console.WriteLine();
            
            Console.WriteLine("[SUM] PARSED OPTIONS SUMMARY:");
            Console.WriteLine("-----------------------------------------------");
            PrintOptionsSummary();
            
            Console.WriteLine("===============================================");
            Console.WriteLine();
        }
        
        Console.WriteLine("Use --help to see available commands and options.");

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
