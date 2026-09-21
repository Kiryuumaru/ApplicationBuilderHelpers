using ApplicationBuilderHelpers.Common;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// Guards for <see cref="ThreadHelpers.WaitThread(Action, CancellationToken)"/>
/// and <see cref="ThreadHelpers.WaitThread(Func{Task}, CancellationToken)"/>:
/// async work completes before the returned task completes, faults surface on
/// the returned task, pre-start cancellation wins without invoking the work,
/// and the success path leaves no residue behind.
/// </summary>
public sealed class ThreadHelperTests
{
    private static readonly TimeSpan GuardTimeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task AsyncFunc_CompletesBeforeReturnedTaskCompletes()
    {
        var completed = false;

        Task waitTask = ThreadHelpers.WaitThread(async () =>
        {
            await Task.Delay(250);
            completed = true;
        });

        Task winner = await Task.WhenAny(waitTask, Task.Delay(GuardTimeout));
        Assert.Same(waitTask, winner);
        await waitTask;

        Assert.True(completed);
    }

    [Fact]
    public async Task Fault_SurfacesViaReturnedTask()
    {
        var failure = await Assert.ThrowsAsync<InvalidOperationException>(() => ThreadHelpers.WaitThread(async () =>
        {
            await Task.Delay(50);
            throw new InvalidOperationException("Probe failure.");
        }));

        Assert.Equal("Probe failure.", failure.Message);
    }

    [Fact]
    public async Task SyncFault_SurfacesViaReturnedTask()
    {
        var failure = await Assert.ThrowsAsync<InvalidOperationException>(() => ThreadHelpers.WaitThread(() =>
        {
            throw new InvalidOperationException("Sync probe failure.");
        }));

        Assert.Equal("Sync probe failure.", failure.Message);
    }

    [Fact]
    public async Task PrecanceledToken_WinsWithoutInvokingWork()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var actionInvoked = false;
        var funcInvoked = false;

        await Assert.ThrowsAsync<OperationCanceledException>(() => ThreadHelpers.WaitThread(() =>
        {
            actionInvoked = true;
        }, cts.Token));
        await Assert.ThrowsAsync<OperationCanceledException>(() => ThreadHelpers.WaitThread(() =>
        {
            funcInvoked = true;
            return Task.CompletedTask;
        }, cts.Token));

        Assert.False(actionInvoked);
        Assert.False(funcInvoked);
    }

    [Fact]
    public async Task SuccessPath_CompletesCleanlyOnRepeatUse()
    {
        var actionCount = 0;
        var funcCount = 0;

        for (var i = 0; i < 3; i++)
        {
            Task actionTask = ThreadHelpers.WaitThread(() => Interlocked.Increment(ref actionCount));
            Task actionWinner = await Task.WhenAny(actionTask, Task.Delay(GuardTimeout));
            Assert.Same(actionTask, actionWinner);
            await actionTask;

            Task funcTask = ThreadHelpers.WaitThread(async () =>
            {
                await Task.Delay(10);
                Interlocked.Increment(ref funcCount);
            });
            Task funcWinner = await Task.WhenAny(funcTask, Task.Delay(GuardTimeout));
            Assert.Same(funcTask, funcWinner);
            await funcTask;
        }

        Assert.Equal(3, actionCount);
        Assert.Equal(3, funcCount);
    }
}
