using ApplicationBuilderHelpers.Attributes;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApplicationBuilderHelpers.Test.Cli.Commands;

internal class SubCommand : ApplicationCommand
{
    [CommandOption('e', "test", Description = "test 1")]
    public string? SubTest { get; set; } = null;

    public SubCommand() : base("sub", "The sub command for the application.")
    {

    }

    protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
    {
        Console.WriteLine("Hello from SubCommand");
        return ValueTask.CompletedTask;
    }
}
