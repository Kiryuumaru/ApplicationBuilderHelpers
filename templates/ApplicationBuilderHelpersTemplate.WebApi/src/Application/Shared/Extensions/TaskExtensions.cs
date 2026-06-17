namespace Application.Shared.Extensions;

public static class TaskExtensions
{
    public static void Forget(this Task task)
    {
        if (!task.IsCompleted || task.IsFaulted)
        {
            _ = ForgetAwaited(task);
        }

        async static Task ForgetAwaited(Task task)
        {
            try
            {
                await task;
            }
            catch { /* Intentional fire-and-forget; exceptions are discarded by design */ }
        }
    }

    public static Task WaitThread(this Task task)
    {
        return Utilities.ThreadHelpers.WaitThread(() => task);
    }
}
