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
            if (Tags.Length > 0)
            {
                Console.WriteLine($"Tags: {string.Join(", ", Tags)}");
            }
            if (ExcludePatterns.Length > 0)
            {
                Console.WriteLine($"Exclude: {string.Join(", ", ExcludePatterns)}");
            }
        }

        cancellationTokenSource.Cancel(); // Cancel the application host to stop further processing

        return ValueTask.CompletedTask;
    }
}
