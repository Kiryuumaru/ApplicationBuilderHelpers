namespace Presentation.WebApp.Server.FunctionalTests;

/// <summary>
/// A Fact attribute with a 2-minute timeout.
/// Functional tests may be slower than unit tests but shouldn't hang indefinitely.
/// </summary>
public class TimedFactAttribute : FactAttribute
{
    /// <summary>
    /// Default timeout of 2 minutes in milliseconds.
    /// </summary>
    public const int DefaultTimeoutMs = 120_000;

    public TimedFactAttribute()
    {
        Timeout = DefaultTimeoutMs;
    }
}

/// <summary>
/// A Theory attribute with a 2-minute timeout.
/// </summary>
public class TimedTheoryAttribute : TheoryAttribute
{
    /// <summary>
    /// Default timeout of 2 minutes in milliseconds.
    /// </summary>
    public const int DefaultTimeoutMs = 120_000;

    public TimedTheoryAttribute()
    {
        Timeout = DefaultTimeoutMs;
    }
}
