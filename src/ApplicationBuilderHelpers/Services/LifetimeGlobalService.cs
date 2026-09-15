using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ApplicationBuilderHelpers.Services;

internal class LifetimeGlobalService
{
    internal CancellationTokenSource CancellationTokenSource { get; private set; } = new();

    private readonly List<Action> applicationExitingActionCallback = [];
    private readonly List<Action> applicationExitedActionCallback = [];
    private readonly List<Func<Task>> ApplicationExitingTaskCallback = [];
    private readonly List<Func<Task>> ApplicationExitedTaskCallback = [];

    // Exactly-once guards: CommandExecutor invokes Exiting from the
    // command-wins-canceled, host-wins-canceled, and trailing success paths,
    // where early rethrows can otherwise skip or repeat a call site.
    // The first caller wins via Interlocked.Exchange; late callers no-op.
    private int exitingInvoked;
    private int exitedInvoked;

    public CancellationTokenSource CreateCancellationTokenSource()
    {
        return CancellationTokenSource.CreateLinkedTokenSource(CancellationTokenSource.Token);
    }

    public CancellationToken CreateCancellationToken()
    {
        return CancellationTokenSource.Token;
    }

    public void ApplicationExitingCallback(Action callback)
    {
        applicationExitingActionCallback.Add(callback);
    }

    public void ApplicationExitingCallback(Func<Task> callback)
    {
        ApplicationExitingTaskCallback.Add(callback);
    }

    public void ApplicationExitedCallback(Action callback)
    {
        applicationExitedActionCallback.Add(callback);
    }

    public void ApplicationExitedCallback(Func<Task> callback)
    {
        ApplicationExitedTaskCallback.Add(callback);
    }

    /// <summary>
    /// Invokes registered ApplicationExiting callbacks exactly once.
    /// The first caller runs the callbacks; concurrent or late callers no-op.
    /// </summary>
    public Task InvokeApplicationExitingCallbacksAsync()
    {
        if (Interlocked.Exchange(ref exitingInvoked, 1) == 1)
        {
            return Task.CompletedTask;
        }

        List<Task> tasks = [];
        foreach (var action in applicationExitingActionCallback)
        {
            tasks.Add(Task.Run(action));
        }
        foreach (var task in ApplicationExitingTaskCallback)
        {
            tasks.Add(Task.Run(async () => await task()));
        }
        return Task.WhenAll(tasks);
    }

    /// <summary>
    /// Invokes registered ApplicationExited callbacks exactly once.
    /// The finally-path owner wins; repeat invocations no-op.
    /// </summary>
    public Task InvokeApplicationExitedCallbacksAsync()
    {
        if (Interlocked.Exchange(ref exitedInvoked, 1) == 1)
        {
            return Task.CompletedTask;
        }

        List<Task> tasks = [];
        foreach (var action in applicationExitedActionCallback)
        {
            tasks.Add(Task.Run(action));
        }
        foreach (var task in ApplicationExitedTaskCallback)
        {
            tasks.Add(Task.Run(async () => await task()));
        }
        return Task.WhenAll(tasks);
    }
}
