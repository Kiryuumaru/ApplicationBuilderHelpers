using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApplicationBuilderHelpers.Exceptions;

public class CommandException : Exception
{
    public int ExitCode { get; }

    public CommandException(int exitCode)
        : base(null)
    {
        ExitCode = exitCode;
    }

    public CommandException(string message, int exitCode)
        : base(message)
    {
        ExitCode = exitCode;
    }
}
