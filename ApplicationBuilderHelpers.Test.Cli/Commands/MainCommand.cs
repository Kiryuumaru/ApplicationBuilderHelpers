using AbsolutePathHelpers;
using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.Exceptions;
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

internal class MainCommand : BaseCommand
{
    [CommandOption('t', "test", Description = "Test args with env var", EnvironmentVariable = "ENV_TEST1")]
    public required string? Test { get; set; } = null;

    [CommandOption("test-path", Description = "Test args path")]
    public required AbsolutePath TestPath { get; set; }

    [CommandOption("test-paths", Description = "Test args paths")]
    public required AbsolutePath[] TestPaths { get; set; }

    [CommandOption('l', "log-level", Description = "Level of logs to show.")]
    public LogLevel LogLevel { get; set; } = LogLevel.Information;

    [CommandOption(
        "log-level1",
        EnvironmentVariable = "CCCC",
        Description = "Level of logs to show.",
        FromAmong = ["Trace", "Debug", "Information", "Warning", "Error", "Critical"],
        CaseSensitive = false)]
    public string LogLevel1 { get; set; } = "Information";

    [CommandOption(
        "write-logs",
        EnvironmentVariable = "WRITE_LOGS",
        Description = "Write logs file.")]
    public bool WriteLogs { get; set; } = false;

    public override bool ExitOnRunComplete => true;

    public MainCommand() : base("The main command for the application.")
    {

    }

    protected override async ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken stoppingToken)
    {
        Console.WriteLine("Hello from main");
        Console.WriteLine($"Test: {Test}");
        Console.WriteLine($"LogLevel: {LogLevel}");
        Console.WriteLine($"SSS: {TestPath}");
        Console.WriteLine($"WriteLogs: {WriteLogs}");
        foreach (var p in TestPaths)
        {
            Console.WriteLine($"Test paths: {p}");
        }

        throw new CommandException("Test error", 113);
    }

    public override void AddServices(ApplicationHostBuilder applicationBuilder, IServiceCollection services)
    {
        Console.WriteLine("Hello from main AddServicesAddServicesAddServicesAddServicesAddServicesAddServices");
        base.AddServices(applicationBuilder, services);
    }

    protected override ValueTask CommanPreparation(CancellationToken stoppingToken)
    {
        return base.CommanPreparation(stoppingToken);
    }
}
