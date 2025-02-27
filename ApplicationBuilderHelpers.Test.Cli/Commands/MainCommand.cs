using ApplicationBuilderHelpers.Attributes;
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

    protected override ValueTask Run(CancellationToken stoppingToken)
    {
        Console.WriteLine("Hello from main");
        return ValueTask.CompletedTask;
    }
}
