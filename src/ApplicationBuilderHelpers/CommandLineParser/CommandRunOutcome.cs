namespace ApplicationBuilderHelpers.CommandLineParser;

/// <summary>
/// Command/host run resolution returned by <see cref="CommandRunOrchestrator"/>.
/// Fault and canceled resolutions propagate as exceptions (identity and stack
/// preserved); only the success resolutions return.
/// </summary>
internal enum CommandRunOutcome
{
    /// <summary>
    /// The command finished with success; the host was stopped; Exiting callbacks ran.
    /// </summary>
    CommandSuccess,

    /// <summary>
    /// The command finished faulted (propagates; Exiting skipped, Exited still runs).
    /// </summary>
    CommandFault,

    /// <summary>
    /// The command finished canceled (propagates as external cancellation or the
    /// original internal <see cref="System.OperationCanceledException"/>; Exiting ran).
    /// </summary>
    CommandCanceled,

    /// <summary>
    /// The host finished faulted (propagates; the command was drained; Exiting skipped).
    /// </summary>
    HostFault,

    /// <summary>
    /// The host finished canceled (propagates the host exception; the command was
    /// drained; Exiting ran).
    /// </summary>
    HostCanceled,

    /// <summary>
    /// The host finished with success; the command was drained; a non-zero host exit
    /// code throws <see cref="Exceptions.CommandException"/>.
    /// </summary>
    HostSuccessWithCode,
}
