using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ApplicationBuilderHelpers.Services;

/// <summary>Run-scoped cancellation source plus exactly-once exit callbacks.</summary>
internal class LifetimeGlobalService
{
    /// <summary>Per-run root source; linking here propagates cancellation to derived tokens.</summary>
    internal CancellationTokenSource CancellationTokenSource { get; private set; } = new();

    private readonly List<Action> applicationExitingActionCallback = [];
    private readonly List<Action> applicationExitedActionCallback = [];
    private readonly List<Func<Task>> ApplicationExitingTaskCallback = [];
    private readonly List<Func<Task>> ApplicationExitedTaskCallback = [];

    private int exitingInvoked;
    private int exitedInvoked;

    /// <summary>Derives a linked source from the run root; the caller owns disposal.</summary>
    /// <returns>A source that cancels when the run root cancels.</returns>
    public CancellationTokenSource CreateCancellationTokenSource()
    {
        return CancellationTokenSource.CreateLinkedTokenSource(CancellationTokenSource.Token);
    }

    /// <summary>Exposes the run-root token.</summary>
    /// <returns>The run-root cancellation token.</returns>
    public CancellationToken CreateCancellationToken()
    {
        return CancellationTokenSource.Token;
    }

    /// <summary>Registers a sync callback for the exiting phase.</summary>
    /// <param name="callback">Invoked once when the exiting phase runs.</param>
    public void ApplicationExitingCallback(Action callback)
    {
        applicationExitingActionCallback.Add(callback);
    }

    /// <summary>Registers an async callback for the exiting phase.</summary>
    /// <param name="callback">Invoked once when the exiting phase runs.</param>
    public void ApplicationExitingCallback(Func<Task> callback)
    {
        ApplicationExitingTaskCallback.Add(callback);
    }

    /// <summary>Registers a sync callback for the exited phase.</summary>
    /// <param name="callback">Invoked once when the exited phase runs.</param>
    public void ApplicationExitedCallback(Action callback)
    {
        applicationExitedActionCallback.Add(callback);
    }

    /// <summary>Registers an async callback for the exited phase.</summary>
    /// <param name="callback">Invoked once when the exited phase runs.</param>
    public void ApplicationExitedCallback(Func<Task> callback)
    {
        ApplicationExitedTaskCallback.Add(callback);
    }

    /// <summary>Runs exiting callbacks once; later calls return a completed task.</summary>
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

    /// <summary>Runs exited callbacks once from the finally path; later calls return a completed task.</summary>
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
