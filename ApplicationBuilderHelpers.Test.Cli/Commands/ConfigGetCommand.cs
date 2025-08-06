using ApplicationBuilderHelpers.Attributes;
using Microsoft.Extensions.Hosting;

namespace ApplicationBuilderHelpers.Test.Cli.Commands;

[Command("config get", "Get configuration values")]
internal class ConfigGetCommand : ConfigCommand
{
    [CommandArgument("key", Description = "Configuration key to retrieve", Position = 0, Required = false)]
    public string? Key { get; set; }

    [CommandOption('a', "all", Description = "Show all configuration values")]
    public bool ShowAll { get; set; }

    [CommandOption('g', "global", Description = "Show global configuration only")]
    public bool GlobalOnly { get; set; }

    [CommandOption('l', "local", Description = "Show local configuration only")]
    public bool LocalOnly { get; set; }

    [CommandOption('s', "section", Description = "Filter by configuration section")]
    public string? Section { get; set; }

    [CommandOption("path", Description = "Configuration file path")]
    public string? ConfigPath { get; set; }

    [CommandOption("default", Description = "Default value if key not found")]
    public string? DefaultValue { get; set; }

    protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
    {
        // Print debug info if requested
        PrintDebugInfo();

        Console.WriteLine("Configuration Get Operation");
        Console.WriteLine("===========================");
        
        if (ShowAll)
        {
            Console.WriteLine("Showing all configuration values");
        }
        else if (!string.IsNullOrEmpty(Key))
        {
            Console.WriteLine($"Getting configuration for key: {Key}");
            
            if (!string.IsNullOrEmpty(DefaultValue))
            {
                Console.WriteLine($"Default value: {DefaultValue}");
            }
        }
        else
        {
            Console.WriteLine("No key specified. Use --all to show all configurations or specify a key.");
        }

        Console.WriteLine($"Output Format: {OutputFormat}");
        Console.WriteLine($"Global Only: {GlobalOnly}");
        Console.WriteLine($"Local Only: {LocalOnly}");
        
        if (!string.IsNullOrEmpty(Section))
        {
            Console.WriteLine($"Section Filter: {Section}");
        }
        
        if (!string.IsNullOrEmpty(ConfigPath))
        {
            Console.WriteLine($"Config Path: {ConfigPath}");
        }

        if (!Quiet)
        {
            Console.WriteLine("Configuration retrieval completed!");
        }

        cancellationTokenSource.Cancel();
        return ValueTask.CompletedTask;
    }
}