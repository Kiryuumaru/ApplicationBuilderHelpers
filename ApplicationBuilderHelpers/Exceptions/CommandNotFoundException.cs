using System;

namespace ApplicationBuilderHelpers.Exceptions;

/// <summary>
/// Exception thrown when a command is not found.
/// </summary>
public class CommandNotFoundException : CommandException
{
    public CommandNotFoundException(string message) : base(message)
    {
    }

    public CommandNotFoundException(string message, Exception innerException) : base(message, innerException)
    {
    }
}