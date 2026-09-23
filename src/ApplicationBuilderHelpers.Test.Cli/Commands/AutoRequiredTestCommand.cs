using ApplicationBuilderHelpers.Attributes;
using Microsoft.Extensions.Hosting;

namespace ApplicationBuilderHelpers.Test.Cli.Commands;

[Command("auto-required-test", "Test command demonstrating automatic required detection")]
internal class AutoRequiredTestCommand : BaseCommand
{
    [CommandOption('n', "name", Description = "Name parameter (auto-detected as required)")]
    public required string Name { get; set; }

    [CommandOption('e', "email", Description = "Email parameter (auto-detected as required)")]
    public required string Email { get; set; }

    [CommandOption('p', "phone", Description = "Phone parameter (explicitly required)", Required = true)]
    public string Phone { get; set; } = string.Empty;

    [CommandOption('a', "age", Description = "Age parameter (optional)")]
    public int Age { get; set; } = 0;

    [CommandOption('f', "force", Description = "Force flag (optional)")]
    public bool Force { get; set; }

    [CommandArgument("target", Description = "Target argument (auto-detected as required)", Position = 0)]
    public required string Target { get; set; }

    [CommandArgument("project", Description = "Project argument (explicitly required)", Position = 1, Required = true)]
    public string Project { get; set; } = string.Empty;

    [CommandArgument("source", Description = "Source argument (optional)", Position = 2, Required = false)]
    public string? Source { get; set; }

    protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
    {
        PrintDebugInfo();

        Console.WriteLine("Auto Required Test Command Executed");
        Console.WriteLine("=== Required via C# keyword ===");
        Console.WriteLine($"Target: {Target}");
        Console.WriteLine($"Name: {Name}");
        Console.WriteLine($"Email: {Email}");
        
        Console.WriteLine("=== Required via attribute ===");
        Console.WriteLine($"Project: {Project}");
        Console.WriteLine($"Phone: {Phone}");
        
        Console.WriteLine("=== Optional parameters ===");
        Console.WriteLine($"Source: {Source ?? "not provided"}");
        Console.WriteLine($"Age: {Age}");
        Console.WriteLine($"Force: {Force}");

        if (!Quiet)
        {
            Console.WriteLine("Auto required test command completed successfully!");
        }

        return ValueTask.CompletedTask;
    }
}