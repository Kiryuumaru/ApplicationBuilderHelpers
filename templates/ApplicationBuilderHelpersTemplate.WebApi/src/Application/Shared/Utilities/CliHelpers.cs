using AbsolutePathHelpers;
using Application.NativeCmd.Exceptions;
using CliWrap;
using CliWrap.EventStream;
using System.Runtime.InteropServices;
using System.Text;

namespace Application.Shared.Utilities;

public static class CliHelpers
{
    public static Command BuildRun(
        string path,
        string[] args,
        AbsolutePath? workingDirectory = default,
        IDictionary<string, string?>? environmentVariables = default,
        PipeSource? inPipeTarget = default,
        PipeTarget? outPipeTarget = default,
        PipeTarget? errPipeTarget = default)
    {
        Command osCli = Cli.Wrap(path)
            .WithArguments(args, true)
            .WithValidation(CommandResultValidation.None);

        if (workingDirectory != null)
        {
            osCli = osCli
                .WithWorkingDirectory(workingDirectory);
        }

        if (environmentVariables != null)
        {
            osCli = osCli
            .WithEnvironmentVariables(environmentVariables.ToDictionary());
        }

        if (inPipeTarget != null)
        {
            osCli = osCli
                .WithStandardInputPipe(inPipeTarget);
        }

        if (outPipeTarget != null)
        {
            osCli = osCli
                .WithStandardOutputPipe(outPipeTarget);
        }

        if (errPipeTarget != null)
        {
            osCli = osCli
                .WithStandardErrorPipe(errPipeTarget);
        }

        return osCli;
    }

    public static Command BuildRun(
        string command,
        AbsolutePath? workingDirectory = default,
        IDictionary<string, string?>? environmentVariables = default,
        PipeSource? inPipeTarget = default,
        PipeTarget? outPipeTarget = default,
        PipeTarget? errPipeTarget = default)
    {
        return BuildRunWithScript(command, workingDirectory, environmentVariables, inPipeTarget, outPipeTarget, errPipeTarget);
    }

    private static Command BuildRunWithScript(
        string command,
        AbsolutePath? workingDirectory = default,
        IDictionary<string, string?>? environmentVariables = default,
        PipeSource? inPipeTarget = default,
        PipeTarget? outPipeTarget = default,
        PipeTarget? errPipeTarget = default)
    {
        var scriptPath = CreateTempScriptFile(command);

        Command osCli;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            osCli = Cli.Wrap("cmd")
                .WithArguments(["/c", $"call \"{scriptPath}\""], false);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            osCli = Cli.Wrap("/bin/bash")
                .WithArguments(["-c", $"\"chmod +x '{scriptPath}' && '{scriptPath}'\""], false);
        }
        else
        {
            throw new PlatformNotSupportedException($"Unsupported operating system: {RuntimeInformation.OSDescription}");
        }

        if (workingDirectory != null)
        {
            osCli = osCli.WithWorkingDirectory(workingDirectory);
        }

        if (environmentVariables != null)
        {
            osCli = osCli.WithEnvironmentVariables(environmentVariables.ToDictionary());
        }

        if (inPipeTarget != null)
        {
            osCli = osCli.WithStandardInputPipe(inPipeTarget);
        }

        if (outPipeTarget != null)
        {
            osCli = osCli.WithStandardOutputPipe(outPipeTarget);
        }

        if (errPipeTarget != null)
        {
            osCli = osCli.WithStandardErrorPipe(errPipeTarget);
        }

        return osCli;
    }

    private static string CreateTempScriptFile(string command)
    {
        var tempDir = Path.GetTempPath();
        var scriptFileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? $"{Guid.NewGuid()}_script.cmd"
            : $"{Guid.NewGuid()}_script.sh";

        var scriptPath = Path.Combine(tempDir, scriptFileName);

        File.WriteAllText(scriptPath, command);

        return scriptPath;
    }

    private static void CleanupTempScriptFile(string scriptPath)
    {
        try
        {
            if (File.Exists(scriptPath))
            {
                File.Delete(scriptPath);
            }
        }
        catch { /* Best-effort temp file cleanup; non-critical if it fails */ }
    }

    public static async Task<string> RunOnce(
        string path,
        string[] args,
        AbsolutePath? workingDirectory = default,
        IDictionary<string, string?>? environmentVariables = default,
        CancellationToken stoppingToken = default)
    {
        var stdBuffer = new StringBuilder();

        var result = await BuildRun(path, args, workingDirectory, environmentVariables, null, PipeTarget.ToStringBuilder(stdBuffer), PipeTarget.ToStringBuilder(stdBuffer))
            .ExecuteAsync(stoppingToken);

        if (result.ExitCode != 0)
        {
            throw new NativeCmdException(stdBuffer.ToString(), result.ExitCode);
        }

        return stdBuffer.ToString();
    }

    public static async Task<string> RunOnce(
        string command,
        AbsolutePath? workingDirectory = default,
        IDictionary<string, string?>? environmentVariables = default,
        CancellationToken stoppingToken = default)
    {
        var stdBuffer = new StringBuilder();
        var scriptPath = CreateTempScriptFile(command);

        try
        {
            var result = await BuildRunWithScript(command, workingDirectory, environmentVariables, null, PipeTarget.ToStringBuilder(stdBuffer), PipeTarget.ToStringBuilder(stdBuffer))
                .ExecuteAsync(stoppingToken);

            if (result.ExitCode != 0)
            {
                throw new NativeCmdException(stdBuffer.ToString(), result.ExitCode);
            }

            return stdBuffer.ToString();
        }
        finally
        {
            CleanupTempScriptFile(scriptPath);
        }
    }

    public static async Task<string> RunOnceAndIgnore(
        string command,
        AbsolutePath? workingDirectory = default,
        IDictionary<string, string?>? environmentVariables = default,
        CancellationToken stoppingToken = default)
    {
        var stdBuffer = new StringBuilder();
        var scriptPath = CreateTempScriptFile(command);

        try
        {
            _ = await BuildRunWithScript(command, workingDirectory, environmentVariables, null, PipeTarget.ToStringBuilder(stdBuffer), PipeTarget.ToStringBuilder(stdBuffer))
                .ExecuteAsync(stoppingToken);

            return stdBuffer.ToString();
        }
        finally
        {
            CleanupTempScriptFile(scriptPath);
        }
    }

    public static IAsyncEnumerable<CommandEvent> RunListen(
        string path,
        string[] args,
        AbsolutePath? workingDirectory = default,
        IDictionary<string, string?>? environmentVariables = default,
        PipeSource? inPipeTarget = default,
        CancellationToken stoppingToken = default)
    {
        var osCli = BuildRun(path, args, workingDirectory, environmentVariables, inPipeTarget);

        return osCli.ListenAsync(stoppingToken);
    }

    public static IAsyncEnumerable<CommandEvent> RunListen(
        string command,
        AbsolutePath? workingDirectory = default,
        IDictionary<string, string?>? environmentVariables = default,
        PipeSource? inPipeTarget = default,
        CancellationToken stoppingToken = default)
    {
        var osCli = BuildRunWithScript(command, workingDirectory, environmentVariables, inPipeTarget);

        return osCli.ListenAsync(stoppingToken);
    }
}
