using ApplicationBuilderHelpers.Attributes;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ApplicationBuilderHelpers.Test.Cli.Commands;

[Command("enum-limited", "Test command with limited enum choices")]
internal class EnumLimitedCommand : Command
{
    [CommandOption('l', "limited-level", Description = "LogLevel with limited FromAmong choices.", FromAmong = ["Trace", "Debug", "Information"])]
    public LogLevel LimitedLevel { get; set; } = LogLevel.Information;

    [CommandOption('v', "verbose", Description = "Enable verbose output")]
    public bool Verbose { get; set; }

    [CommandArgument("target", Description = "Target to process", Position = 0, Required = false)]
    public string? Target { get; set; }

    protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
    {
        Console.WriteLine($"EnumLimited Command executed successfully!");
        Console.WriteLine($"Target: {Target ?? "default"}");
        Console.WriteLine($"LimitedLevel: {LimitedLevel}");
        Console.WriteLine($"Verbose: {Verbose}");
        
        if (Verbose)
        {
            Console.WriteLine();
            Console.WriteLine("=== ENUM LIMITED COMMAND DETAILS ===");
            Console.WriteLine($"LimitedLevel Type: {LimitedLevel.GetType().FullName}");
            Console.WriteLine($"LimitedLevel Value: {LimitedLevel}");
            Console.WriteLine($"LimitedLevel Number: {(int)LimitedLevel}");
            Console.WriteLine("Limited LogLevel choices: Trace, Debug, Information");
            Console.WriteLine("======================================");
        }

        cancellationTokenSource.Cancel();
        return ValueTask.CompletedTask;
    }
}