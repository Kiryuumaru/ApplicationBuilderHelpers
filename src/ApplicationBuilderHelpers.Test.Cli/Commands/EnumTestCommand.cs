using ApplicationBuilderHelpers.Attributes;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ApplicationBuilderHelpers.Test.Cli.Commands;

[Command("enum-test", "Test command for enum auto-population without FromAmong")]
internal class EnumTestCommand : Command
{
    [CommandOption('e', "enum-level", Description = "LogLevel enum without FromAmong specified.", EnvironmentVariable = "ENUM_LEVEL")]
    public LogLevel EnumLevel { get; set; } = LogLevel.Information;

    [CommandOption('v', "verbose", Description = "Enable verbose output")]
    public bool Verbose { get; set; }

    [CommandArgument("target", Description = "Target to process", Position = 0, Required = false)]
    public string? Target { get; set; }

    protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
    {
        Console.WriteLine($"EnumTest Command executed successfully!");
        Console.WriteLine($"Target: {Target ?? "default"}");
        Console.WriteLine($"EnumLevel: {EnumLevel}");
        Console.WriteLine($"Verbose: {Verbose}");
        
        if (Verbose)
        {
            Console.WriteLine();
            Console.WriteLine("=== ENUM TEST COMMAND DETAILS ===");
            Console.WriteLine($"EnumLevel Type: {EnumLevel.GetType().FullName}");
            Console.WriteLine($"EnumLevel Value: {EnumLevel}");
            Console.WriteLine($"EnumLevel Number: {(int)EnumLevel}");
            Console.WriteLine("Available LogLevel values:");
            foreach (var level in Enum.GetValues<LogLevel>())
            {
                Console.WriteLine($"  - {level} ({(int)level})");
            }
            Console.WriteLine("===================================");
        }

        cancellationTokenSource.Cancel();
        return ValueTask.CompletedTask;
    }
}