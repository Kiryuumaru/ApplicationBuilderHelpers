using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ApplicationBuilderHelpers.Services;

internal class CommandInvokerService
{
    private Func<CancellationToken, ValueTask>? _action = null;

    public void SetCommand(Func<CancellationToken, ValueTask> action)
    {
        _action = action;
    }

    public async ValueTask InvokeCommand(CancellationToken stoppingToken)
    {
        if (_action != null)
        {
            await _action.Invoke(stoppingToken);
        }
    }
}
