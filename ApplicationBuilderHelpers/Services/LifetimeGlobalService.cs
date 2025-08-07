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

    public Task InvokeApplicationExitingCallbacksAsync()
    {
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

    public Task InvokeApplicationExitedCallbacksAsync()
    {
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
