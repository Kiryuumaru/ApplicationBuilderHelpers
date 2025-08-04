using ApplicationBuilderHelpers.Attributes;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApplicationBuilderHelpers.Test.Cli.Commands;

[Command("config set", "Set configuration values")]
internal class ConfigSetCommand : ConfigCommand
{
    [CommandArgument("key", Description = "Configuration key", Position = 0, Required = true)]
    public required string Key { get; set; }

    [CommandArgument("value", Description = "Configuration value", Position = 1, Required = true)]
    public required string Value { get; set; }

    [CommandOption('g', "global", Description = "Set global configuration")]
    public bool Global { get; set; }

    [CommandOption('f', "force", Description = "Force overwrite existing value")]
    public bool Force { get; set; }

    protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
    {
        Console.WriteLine($"Setting config {Key} = {Value}");
        Console.WriteLine($"Global: {Global}");
        Console.WriteLine($"Force: {Force}");

        cancellationTokenSource.Cancel();
        return ValueTask.CompletedTask;
    }
}