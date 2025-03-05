using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Threading;
using System;
using System.Diagnostics.CodeAnalysis;

namespace ApplicationBuilderHelpers;

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public abstract class ApplicationCommand
{
    protected ApplicationCommand(string? description = null)
    {
        Name = null;
        Description = description;
    }

    protected ApplicationCommand(string name, string? description = null)
    {
        Name = name;
        Description = description;
    }

    public string? Name { get; }

    public string? Description { get; }

    internal ValueTask RunInternal(CancellationToken stoppingToken)
    {
        return Run(stoppingToken);
    }

    protected abstract ValueTask Run(CancellationToken stoppingToken);
}
