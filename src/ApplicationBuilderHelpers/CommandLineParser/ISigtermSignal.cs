using System;

namespace ApplicationBuilderHelpers.CommandLineParser;

/// <summary>
/// SIGTERM source, testable abstraction.
/// The scope's linked CTS links the outer token only; SIGTERM joins via the handler
/// calling Cancel, so the scope observes outer + Ctrl+C + SIGTERM while host
/// <c>ApplicationStopping</c> stays host-owned.
/// Exists so tests can simulate SIGTERM in-process with a fake.
/// </summary>
internal interface ISigtermSignal
{
    /// <summary>
    /// Raised when SIGTERM is observed.
    /// </summary>
    event Action? Signaled;
}
