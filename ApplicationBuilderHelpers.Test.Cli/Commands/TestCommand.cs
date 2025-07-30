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

    [CommandOption("timeout", Description = "Timeout in seconds", Required = false)]
    public int Timeout { get; set; } = 30;

    [CommandOption("parallel", Description = "Run tests in parallel")]
    public bool Parallel { get; set; }

    [CommandOption("target", Description = "Target to test", Required = true)]
    public string[] Targets { get; set; } = [];

    protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
    {
        Console.WriteLine($"Running test on targets: [{string.Join(", ", Targets)}]");
        if (Verbose)
        {
            Console.WriteLine($"Config: {ConfigPath ?? "default"}");
            Console.WriteLine($"Timeout: {Timeout}s");
            Console.WriteLine($"Parallel: {Parallel}");
            Console.WriteLine($"Target count: {Targets.Length}");
        }

        cancellationTokenSource.Cancel(); // Cancel the application host to stop further processing

        return ValueTask.CompletedTask;
    }
}
