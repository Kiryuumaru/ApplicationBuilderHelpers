using ApplicationBuilderHelpers.Attributes;
using Microsoft.Extensions.Hosting;

namespace ApplicationBuilderHelpers.Test.Cli.Commands;

[Command("config", "Configuration values")]
internal abstract class ConfigCommand : BaseCommand
{
    [CommandOption("format", Description = "Output format", FromAmong = ["json", "yaml", "table", "list"])]
    public string OutputFormat { get; set; } = "table";
}