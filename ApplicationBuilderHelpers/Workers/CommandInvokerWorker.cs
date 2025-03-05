using ApplicationBuilderHelpers.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ApplicationBuilderHelpers.Workers;

internal class CommandInvokerWorker(IServiceProvider serviceProvider) : BackgroundService
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var serviceProviderScope = _serviceProvider.CreateScope();
        var commandInvokerService = serviceProviderScope.ServiceProvider.GetRequiredService<CommandInvokerService>();
        await commandInvokerService.InvokeCommand(stoppingToken);
    }
}
