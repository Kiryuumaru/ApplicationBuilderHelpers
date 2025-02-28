using ApplicationBuilderHelpers.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApplicationBuilderHelpers.Test.Cli.Commands;

internal class MainCommand : ApplicationCommand
{
    [CommandOption('t', "test")]
    public string? Test { get; set; } = null;

    public MainCommand() : base("The main command for the application.")
    {

    }

    protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken stoppingToken)
    {
        Console.WriteLine("Hello from main");
        return ValueTask.CompletedTask;
    }

    public override void AddServices(ApplicationHostBuilder applicationBuilder, IServiceCollection services)
    {
        Console.WriteLine("Hello from main AddServicesAddServicesAddServicesAddServicesAddServicesAddServices");
        base.AddServices(applicationBuilder, services);
    }
}
