namespace Application.Identity.Constants;

internal static class CleanupWorkerDefaults
{
    public static readonly TimeSpan CleanupInterval = TimeSpan.FromHours(24);
    public static readonly TimeSpan InitialDelay = TimeSpan.FromMinutes(5);
}
