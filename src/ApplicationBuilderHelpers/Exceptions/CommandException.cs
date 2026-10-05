using System;

namespace ApplicationBuilderHelpers.Exceptions;

/// <summary>
/// Command failure carrying an exit code and a usage-vs-fault kind.
/// </summary>
public class CommandException : Exception
{
    /// <summary>
    /// Gets the process exit code; usage errors use 2, faults use 1 unless a custom code is supplied.
    /// </summary>
    public int ExitCode { get; }

    /// <summary>
    /// Gets the usage-vs-fault kind; defaults to <see cref="CommandErrorKind.Fault"/>.
    /// </summary>
    public CommandErrorKind Kind { get; }

    /// <summary>
    /// Gets the optional command name qualifying the error footer; null renders global hints.
    /// </summary>
    public string? CommandName { get; }

    /// <summary>
    /// Gets whether this failure is an allowed-value (<c>FromAmong</c>) violation.
    /// The help/version forgiveness gate lets these beat <c>--help</c>/<c>--version</c>;
    /// all other deferred validation stays forgiven. Set only by the NotAmong factory.
    /// </summary>
    internal bool IsAllowedValueViolation { get; set; }

    /// <summary>
    /// Throws with an exit code and an empty message.
    /// </summary>
    /// <param name="exitCode">The process exit code.</param>
    public CommandException(int exitCode)
        : base(null)
    {
        ExitCode = exitCode;
        Kind = CommandErrorKind.Fault;
    }

    /// <summary>
    /// Throws with a message and an exit code; kind defaults to fault.
    /// </summary>
    /// <param name="message">The user-facing error text.</param>
    /// <param name="exitCode">The process exit code.</param>
    public CommandException(string message, int exitCode)
        : this(message, exitCode, CommandErrorKind.Fault)
    {
    }

    /// <summary>
    /// Throws with a message, an exit code, and an explicit usage-vs-fault kind.
    /// </summary>
    /// <param name="message">The user-facing error text.</param>
    /// <param name="exitCode">The process exit code.</param>
    /// <param name="kind">The usage-vs-fault kind selecting the footer and the default exit.</param>
    /// <param name="commandName">The optional command name qualifying the footer.</param>
    public CommandException(string message, int exitCode, CommandErrorKind kind, string? commandName = null)
        : base(message)
    {
        ExitCode = exitCode;
        Kind = kind;
        CommandName = commandName;
    }

    /// <summary>
    /// Throws with an exit code and a kind but an empty message.
    /// </summary>
    /// <param name="exitCode">The process exit code.</param>
    /// <param name="kind">The usage-vs-fault kind selecting the footer and the default exit.</param>
    public CommandException(int exitCode, CommandErrorKind kind)
        : base(null)
    {
        ExitCode = exitCode;
        Kind = kind;
    }
}
