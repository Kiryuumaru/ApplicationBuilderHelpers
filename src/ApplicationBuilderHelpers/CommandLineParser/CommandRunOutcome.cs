namespace ApplicationBuilderHelpers.CommandLineParser;

/// <summary>
/// Joint command/host run resolution returned by <see cref="CommandRunOrchestrator"/>.
/// Fault and canceled resolutions propagate as exceptions (identity and stack
/// preserved); only the success resolutions return.
/// </summary>
internal enum CommandRunOutcome
{
    /// <summary>
    /// The command won with success; the host was stopped; Exiting callbacks ran.
    /// </summary>
    CommandSuccess,

    /// <summary>
    /// The command won faulted (propagates; Exiting skipped, Exited still runs).
    /// </summary>
    CommandFault,

    /// <summary>
    /// The command won canceled (propagates as external cancellation or the
    /// original internal <see cref="System.OperationCanceledException"/>; Exiting ran).
    /// </summary>
    CommandCanceled,

    /// <summary>
    /// The host won faulted (propagates; the command was drained; Exiting skipped).
    /// </summary>
    HostFault,

    /// <summary>
    /// The host won canceled (propagates the host exception; the command was
    /// drained; Exiting ran).
    /// </summary>
    HostCanceled,

    /// <summary>
    /// The host won with success; the command was drained; a non-zero host exit
    /// code throws <see cref="Exceptions.CommandException"/>.
    /// </summary>
    HostSuccessWithCode,
}
