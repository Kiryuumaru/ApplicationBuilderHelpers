namespace Application.Shared.Extensions;

/// <summary>
/// Extension methods for Task.
/// </summary>
public static class TaskExtensions
{
    /// <summary>
    /// Observes the task to prevent unobserved task exceptions.
    /// </summary>
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
            catch { }
        }
    }
}
