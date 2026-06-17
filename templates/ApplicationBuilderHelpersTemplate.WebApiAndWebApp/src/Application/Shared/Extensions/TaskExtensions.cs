using Application.Shared.Utilities;

namespace Application.Shared.Extensions;

/// <summary>
/// Provides extension methods for Task operations.
/// </summary>
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
            catch
            {
                // Intentionally empty: fire-and-forget task should not propagate exceptions
            }
        }
    }

    public static Task WaitThread(this Task task)
    {
        return ThreadHelpers.WaitThread(() => task);
    }
}
