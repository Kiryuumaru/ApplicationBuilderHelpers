namespace Application.NativeCmd.Exceptions;

public sealed class NativeCmdException : Exception
{
    public int ExitCode { get; }

    public NativeCmdException(string message)
        : base(message)
    {
        ExitCode = -1;
    }

    public NativeCmdException(int exitCode)
        : base(null)
    {
        ExitCode = exitCode;
    }

    public NativeCmdException(string message, int exitCode)
        : base(message)
    {
        ExitCode = exitCode;
    }
}
