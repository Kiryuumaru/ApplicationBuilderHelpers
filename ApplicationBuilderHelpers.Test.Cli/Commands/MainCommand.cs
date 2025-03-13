using AbsolutePathHelpers;
using ApplicationBuilderHelpers.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApplicationBuilderHelpers.Test.Cli.Commands;

internal class MainCommand : ApplicationCommand
{
    [CommandOption('t', "test", Description = "Test args with env var", EnvironmentVariable = "ENV_TEST1")]
    public required string? Test { get; set; } = null;

    [CommandOption("test-path", Description = "Test args path")]
    public required AbsolutePath TestPath { get; set; }

    [CommandOption("test-paths", Description = "Test args paths")]
    public required AbsolutePath[] TestPaths { get; set; }

    [CommandOption('l', "log-level", Description = "Level of logs to show.")]
    public required LogLevel LogLevel { get; set; } = LogLevel.Information;

    public override bool ExitOnRunComplete => false;

    public MainCommand() : base("The main command for the application.")
    {

    }

    protected override async ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken stoppingToken)
    {
        Console.WriteLine("Hello from main");
        //Console.WriteLine($"LogLevel: {LogLevel}");
        Console.WriteLine($"SSS: {TestPath}");
        foreach (var p in TestPaths)
        {
            Console.WriteLine($"Test paths: {p}");
        }
    }

    public override void AddServices(ApplicationHostBuilder applicationBuilder, IServiceCollection services)
    {
        Console.WriteLine("Hello from main AddServicesAddServicesAddServicesAddServicesAddServicesAddServices");
        base.AddServices(applicationBuilder, services);
    }
}
