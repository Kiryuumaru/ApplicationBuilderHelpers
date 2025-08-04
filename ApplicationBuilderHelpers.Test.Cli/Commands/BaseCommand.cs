using ApplicationBuilderHelpers.Attributes;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApplicationBuilderHelpers.Test.Cli.Commands;

internal abstract class BaseCommand : Command
{
    [CommandOption('l', "log-level", Description = "Set the logging level", FromAmong = ["trace", "debug", "information", "warning", "error", "critical", "none"])]
    public string LogLevel { get; set; } = "information";

    [CommandOption('q', "quiet", Description = "Suppress output except errors")]
    public bool Quiet { get; set; }

    [CommandOption("env", Description = "Environment variables to set")]
    public string[] EnvironmentVariables { get; set; } = [];
}
