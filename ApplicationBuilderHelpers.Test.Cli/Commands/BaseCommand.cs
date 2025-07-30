using ApplicationBuilderHelpers.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApplicationBuilderHelpers.Test.Cli.Commands;

internal abstract class BaseCommand : Command
{
    [CommandOption('l', "log-level", Description = "Set the logging level", FromAmong = ["debug", "info", "warn", "error"])]
    public string LogLevel { get; set; } = "info";
}
