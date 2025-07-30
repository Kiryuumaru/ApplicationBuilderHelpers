using ApplicationBuilderHelpers.Attributes;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApplicationBuilderHelpers.Test.Cli.Commands;

[Command]
internal class MainCommand : BaseCommand
{
    protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
    {
        Console.WriteLine("Hello from mainCommand");
        return ValueTask.CompletedTask;
    }
}
