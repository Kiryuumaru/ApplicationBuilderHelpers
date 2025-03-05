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

    public override bool ExitOnRunComplete => false;

    public MainCommand() : base("The main command for the application.")
    {

    }

    protected override async ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken stoppingToken)
    {
        Console.WriteLine("Hello from main");
    }

    public override void AddServices(ApplicationHostBuilder applicationBuilder, IServiceCollection services)
    {
        Console.WriteLine("Hello from main AddServicesAddServicesAddServicesAddServicesAddServicesAddServices");
        base.AddServices(applicationBuilder, services);
    }
}
