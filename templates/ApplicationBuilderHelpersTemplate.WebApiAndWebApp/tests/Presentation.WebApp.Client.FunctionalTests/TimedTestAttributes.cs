namespace Presentation.WebApp.Client.FunctionalTests;

/// <summary>
/// A Fact attribute with a 30-second timeout.
/// UI tests should complete quickly - if they don't, something is wrong.
/// </summary>
public class TimedFactAttribute : FactAttribute
{
    /// <summary>
    /// Default timeout of 30 seconds in milliseconds.
    /// </summary>
    public const int DefaultTimeoutMs = 30_000;

    public TimedFactAttribute()
    {
        Timeout = DefaultTimeoutMs;
    }
}

/// <summary>
/// A Theory attribute with a 30-second timeout.
/// </summary>
public class TimedTheoryAttribute : TheoryAttribute
{
    /// <summary>
    /// Default timeout of 30 seconds in milliseconds.
    /// </summary>
    public const int DefaultTimeoutMs = 30_000;

    public TimedTheoryAttribute()
    {
        Timeout = DefaultTimeoutMs;
    }
}
