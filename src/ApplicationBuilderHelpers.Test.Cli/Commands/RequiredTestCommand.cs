using ApplicationBuilderHelpers.Attributes;
using Microsoft.Extensions.Hosting;

namespace ApplicationBuilderHelpers.Test.Cli.Commands;

[Command("required-test", "Test command with required options")]
internal class RequiredTestCommand : BaseCommand
{
    [CommandOption('n', "name", Description = "Name parameter (required)", Required = true)]
    public required string Name { get; set; }

    [CommandOption('e', "email", Description = "Email parameter (required)", Required = true)]
    public required string Email { get; set; }

    [CommandOption('a', "age", Description = "Age parameter (optional)")]
    public int Age { get; set; } = 0;

    [CommandOption('f', "force", Description = "Force flag (optional)")]
    public bool Force { get; set; }

    [CommandArgument("target", Description = "Target argument (required)", Position = 0, Required = true)]
    public required string Target { get; set; }

    [CommandArgument("source", Description = "Source argument (optional)", Position = 1, Required = false)]
    public string? Source { get; set; }

    protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
    {
        // Print debug info if requested
        PrintDebugInfo();

        Console.WriteLine("Required Test Command Executed");
        Console.WriteLine($"Target: {Target}");
        Console.WriteLine($"Source: {Source ?? "not provided"}");
        Console.WriteLine($"Name: {Name}");
        Console.WriteLine($"Email: {Email}");
        Console.WriteLine($"Age: {Age}");
        Console.WriteLine($"Force: {Force}");

        if (!Quiet)
        {
            Console.WriteLine("Required test command completed successfully!");
        }

        cancellationTokenSource.Cancel();
        return ValueTask.CompletedTask;
    }
}