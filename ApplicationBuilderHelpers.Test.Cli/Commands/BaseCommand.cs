using AbsolutePathHelpers;
using ApplicationBuilderHelpers.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApplicationBuilderHelpers.Test.Cli.Commands;

internal abstract class BaseCommand : ApplicationCommand
{
    [CommandOption("test-parent", Description = "Test parent args with env var", EnvironmentVariable = "ENV_TEST1_PARENT")]
    public required string? TestParent { get; set; } = null;

    protected BaseCommand(string? description = null)
        : base(description)
    {
    }

    protected BaseCommand(string name, string? description = null)
        : base(name, description)
    {
    }
}
