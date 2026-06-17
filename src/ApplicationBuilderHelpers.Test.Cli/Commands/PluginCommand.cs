using ApplicationBuilderHelpers.Attributes;
using Microsoft.Extensions.Hosting;

namespace ApplicationBuilderHelpers.Test.Cli.Commands;

[Command("plugin", "Manage plugins and extensions")]
internal class PluginCommand : BaseCommand
{
    [CommandArgument("action", Description = "Plugin action to perform", Position = 0, Required = true, FromAmong = ["list", "install", "uninstall", "update", "search", "info"])]
    public required string Action { get; set; }

    [CommandArgument("plugin-name", Description = "Name of the plugin", Position = 1, Required = false)]
    public string? PluginName { get; set; }

    [CommandOption('g', "global", Description = "Apply to global plugin directory")]
    public bool Global { get; set; }

    [CommandOption('f', "force", Description = "Force the operation")]
    public bool Force { get; set; }

    [CommandOption("plugin-version", Description = "Specific version to install")]
    public string? Version { get; set; }

    [CommandOption("source", Description = "Plugin source or repository")]
    public string? Source { get; set; }

    [CommandOption("include-prerelease", Description = "Include prerelease versions")]
    public bool IncludePrerelease { get; set; }

    [CommandOption("output-format", Description = "Output format for list and search", FromAmong = ["table", "json", "yaml", "simple"])]
    public string OutputFormat { get; set; } = "table";

    [CommandOption("category", Description = "Filter by plugin category")]
    public string? Category { get; set; }

    [CommandOption("author", Description = "Filter by plugin author")]
    public string? Author { get; set; }

    [CommandOption("tags", Description = "Filter by tags")]
    public string[] Tags { get; set; } = [];

    [CommandOption("interactive", Description = "Run in interactive mode")]
    public bool Interactive { get; set; }

    [CommandOption("no-dependencies", Description = "Skip dependency installation")]
    public bool NoDependencies { get; set; }

    protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
    {
        // Print debug info if requested
        PrintDebugInfo();

        // Handle different actions with specific outputs
        switch (Action.ToLowerInvariant())
        {
            case "list":
                Console.WriteLine("Listing installed plugins");
                break;
            case "install":
                if (!string.IsNullOrEmpty(PluginName))
                {
                    Console.WriteLine($"Installing plugin: {PluginName}");
                }
                else
                {
                    Console.WriteLine("Installing plugin");
                }
                break;
            case "uninstall":
                Console.WriteLine($"Uninstalling plugin: {PluginName}");
                break;
            case "update":
                Console.WriteLine($"Updating plugin: {PluginName}");
                break;
            case "search":
                Console.WriteLine("Searching plugins");
                break;
            case "info":
                Console.WriteLine($"Plugin information: {PluginName}");
                break;
        }

        Console.WriteLine($"Global: {Global}");
        Console.WriteLine($"Force: {Force}");
        Console.WriteLine($"Interactive: {Interactive}");
        Console.WriteLine($"Include Prerelease: {IncludePrerelease}");
        Console.WriteLine($"No Dependencies: {NoDependencies}");
        Console.WriteLine($"Output Format: {OutputFormat}");

        if (!string.IsNullOrEmpty(Version))
        {
            Console.WriteLine($"Version: {Version}");
        }

        if (!string.IsNullOrEmpty(Source))
        {
            Console.WriteLine($"Source: {Source}");
        }

        if (!string.IsNullOrEmpty(Category))
        {
            Console.WriteLine($"Category: {Category}");
        }

        if (!string.IsNullOrEmpty(Author))
        {
            Console.WriteLine($"Author: {Author}");
        }

        if (Tags.Length > 0)
        {
            Console.WriteLine($"Tags: {string.Join(", ", Tags)}");
        }

        if (!Quiet)
        {
            Console.WriteLine($"Plugin {Action} completed successfully!");
        }

        cancellationTokenSource.Cancel();
        return ValueTask.CompletedTask;
    }
}