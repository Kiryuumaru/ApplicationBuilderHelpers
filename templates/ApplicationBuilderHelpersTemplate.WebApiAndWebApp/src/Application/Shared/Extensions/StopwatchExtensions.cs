using System.Diagnostics;

namespace Application.Shared.Extensions;

public static class StopwatchExtensions
{
    public static double ElapsedNanoSeconds(this Stopwatch watch)
    {
        return watch.ElapsedTicks * 1000000000 / (double)Stopwatch.Frequency;
    }

    public static double ElapsedMicroSeconds(this Stopwatch watch)
    {
        return watch.ElapsedTicks * 1000000 / (double)Stopwatch.Frequency;
    }

    public static double ElapsedMilliSeconds(this Stopwatch watch)
    {
        return watch.ElapsedTicks * 1000 / (double)Stopwatch.Frequency;
    }
}
