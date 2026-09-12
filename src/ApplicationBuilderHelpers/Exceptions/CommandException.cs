using System;

namespace ApplicationBuilderHelpers.Exceptions;

/// <summary>
/// Represents an exception that is thrown when a command execution fails and provides an exit code.
/// </summary>
public class CommandException : Exception
{
    /// <summary>
    /// Gets the exit code associated with this command exception.
    /// </summary>
    public int ExitCode { get; }

    /// <summary>
    /// Gets the structured error kind for programmatic handling (replaces message-sniffing).
    /// Defaults to <see cref="CommandErrorKind.Fault"/> for backward compatibility.
    /// </summary>
    public CommandErrorKind Kind { get; }

    /// <summary>
    /// Optional structured command name for <see cref="CommandErrorKind.RequiresSubcommand"/> hints
    /// (avoids parsing it back out of the message text).
    /// </summary>
    public string? CommandName { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandException"/> class with the specified exit code.
    /// </summary>
    /// <param name="exitCode">The exit code that indicates the failure reason.</param>
    public CommandException(int exitCode)
        : base(null)
    {
        ExitCode = exitCode;
        Kind = CommandErrorKind.Fault;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandException"/> class with the specified message and exit code.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="exitCode">The exit code that indicates the failure reason.</param>
    public CommandException(string message, int exitCode)
        : this(message, exitCode, CommandErrorKind.Fault)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandException"/> class with the specified message, exit code, and error kind.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="exitCode">The exit code that indicates the failure reason.</param>
    /// <param name="kind">The structured error kind for programmatic handling.</param>
    /// <param name="commandName">Optional command name for subcommand hints.</param>
    public CommandException(string message, int exitCode, CommandErrorKind kind, string? commandName = null)
        : base(message)
    {
        ExitCode = exitCode;
        Kind = kind;
        CommandName = commandName;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandException"/> class with the specified exit code and error kind.
    /// </summary>
    /// <param name="exitCode">The exit code that indicates the failure reason.</param>
    /// <param name="kind">The structured error kind for programmatic handling.</param>
    public CommandException(int exitCode, CommandErrorKind kind)
        : base(null)
    {
        ExitCode = exitCode;
        Kind = kind;
    }
}
