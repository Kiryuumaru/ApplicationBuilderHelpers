using System;
using System.Threading;
using System.Threading.Tasks;

namespace ApplicationBuilderHelpers.Common;

internal static class CancellationTokenExtensions
{
    public static CancellationToken WithTimeout(this CancellationToken cancellationToken, TimeSpan timeout)
    {
        return CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, new CancellationTokenSource(timeout).Token).Token;
    }

    public static Task WhenCanceled(this CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource<bool>();
        CancellationTokenRegistration? reg = null;
        reg = cancellationToken.Register(s =>
        {
            tcs.TrySetResult(true);
            reg?.Unregister();
        }, tcs);
        return tcs.Task;
    }

    public static void WhenCanceled(this CancellationToken cancellationToken, Func<Task> onCancelled)
    {
        Task.Run(async () =>
        {
            await cancellationToken.WhenCanceled();
            await onCancelled();
        }).Forget();
    }

    public static void WhenCanceled(this CancellationToken cancellationToken, Action onCancelled)
    {
        Task.Run(async () =>
        {
            await cancellationToken.WhenCanceled();
            onCancelled();
        }).Forget();
    }
}
