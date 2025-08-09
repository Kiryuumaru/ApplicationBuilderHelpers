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
    /// Initializes a new instance of the <see cref="CommandException"/> class with the specified exit code.
    /// </summary>
    /// <param name="exitCode">The exit code that indicates the failure reason.</param>
    public CommandException(int exitCode)
        : base(null)
    {
        ExitCode = exitCode;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandException"/> class with the specified message and exit code.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="exitCode">The exit code that indicates the failure reason.</param>
    public CommandException(string message, int exitCode)
        : base(message)
    {
        ExitCode = exitCode;
    }
}
