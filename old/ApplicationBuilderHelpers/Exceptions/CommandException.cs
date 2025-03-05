using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApplicationBuilderHelpers.Exceptions;

public class CommandException(string message, int exitCode) : Exception(message)
{
    public int ExitCode { get; } = exitCode;
}
