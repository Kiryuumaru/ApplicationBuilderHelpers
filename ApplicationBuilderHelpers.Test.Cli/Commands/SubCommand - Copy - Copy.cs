using ApplicationBuilderHelpers.Attributes;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApplicationBuilderHelpers.Test.Cli.Commands;

internal class SubCommand3 : ApplicationCommand
{
    [CommandOption('q', "qqq", Description = "test 3")]
    public string? SubTest { get; set; } = null;

    [CommandOption('z', "zzz", Description = "test 3 z")]
    public required string SubTest1 { get; set; }

    [CommandOption('x', "xxx", Description = "test 3 x", Required = true)]
    public string? SubTest2 { get; set; } = null;

    [CommandArgument("SubTest3", Position = 1, Description = "Positional SubTest3")]
    public required string? SubTest3 { get; set; }

    [CommandArgument(Position = 0, Description = "Positional SubTest4", FromAmong = ["aa", "bb", "cc"])]
    public string? SubTest4 { get; set; } = null;

    public SubCommand3() : base("sub sub3", "The sub command for the application.")
    {

    }

    protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken stoppingToken)
    {
        Console.WriteLine("Hello from SubCommand3");
        Console.WriteLine($"SubTest: {SubTest}");
        Console.WriteLine($"SubTest1: {SubTest1}");
        Console.WriteLine($"SubTest2: {SubTest2}");
        Console.WriteLine($"SubTest3: {SubTest3}");
        Console.WriteLine($"SubTest4: {SubTest4}");
        return ValueTask.CompletedTask;
    }
}
