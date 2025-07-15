using ApplicationBuilderHelpers.Attributes;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApplicationBuilderHelpers.Test.Cli.Commands;

internal class SubCommand2 : ApplicationCommand
{
    [CommandOption('w', "test", Description = "test 2")]
    public string? SubTest { get; set; } = null;

    public SubCommand2() : base("sub sub2", "The sub command for the application.")
    {

    }

    protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
    {
        Console.WriteLine("Hello from SubCommand2");
        return ValueTask.CompletedTask;
    }
}
