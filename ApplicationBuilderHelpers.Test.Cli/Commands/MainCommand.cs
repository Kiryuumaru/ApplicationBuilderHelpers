using ApplicationBuilderHelpers.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApplicationBuilderHelpers.Test.Cli.Commands;

internal class MainCommand : ApplicationCommand
{
    [CommandOption('t', "test", EnvironmentVariable = "ENV_TEST1")]
    public required string? Test { get; set; } = null;

    [CommandOption('l', "log-level", Description = "Level of logs to show.")]
    public required LogLevel LogLevel { get; set; } = LogLevel.Information;

    public override bool ExitOnRunComplete => false;

    public MainCommand() : base("The main command for the application.")
    {

    }

    protected override async ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken stoppingToken)
    {
        Console.WriteLine("Hello from main");
        Console.WriteLine($"Test: {Test}");
        Console.WriteLine($"LogLevel: {LogLevel}");
    }

    public override void AddServices(ApplicationHostBuilder applicationBuilder, IServiceCollection services)
    {
        Console.WriteLine("Hello from main AddServicesAddServicesAddServicesAddServicesAddServicesAddServices");
        base.AddServices(applicationBuilder, services);
    }
}
