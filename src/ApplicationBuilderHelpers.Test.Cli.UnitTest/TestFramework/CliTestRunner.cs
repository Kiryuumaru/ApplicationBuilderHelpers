using System.Diagnostics;
using System.Text;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest.TestFramework;

/// <summary>
/// Provides utilities for running CLI tests with the test.exe
/// </summary>
public class CliTestRunner
{
    private readonly string _executablePath;
    private readonly bool _verbose;
    private readonly TimeSpan _defaultTimeout;
    private readonly Dictionary<string, string> _environmentVariables;

    public CliTestRunner(string executablePath, bool verbose = false, TimeSpan? defaultTimeout = null)
    {
        _executablePath = executablePath ?? throw new ArgumentNullException(nameof(executablePath));
        _verbose = verbose;
        _defaultTimeout = defaultTimeout ?? TimeSpan.FromSeconds(30);
        _environmentVariables = [];
        
        if (!File.Exists(_executablePath))
        {
            throw new FileNotFoundException($"Test executable not found at: {_executablePath}");
        }
    }

    /// <summary>
    /// Sets an environment variable for all subsequent test runs
    /// </summary>
    public CliTestRunner WithEnvironmentVariable(string name, string value)
    {
        _environmentVariables[name] = value;
        return this;
    }

    /// <summary>
    /// Clears all custom environment variables
    /// </summary>
    public CliTestRunner ClearEnvironmentVariables()
    {
        _environmentVariables.Clear();
        return this;
    }

    /// <summary>
    /// Runs the CLI with specified arguments
    /// </summary>
    public async Task<CliTestResult> RunAsync(params string[] args)
    {
        return await RunAsync(_defaultTimeout, null, args);
    }

    /// <summary>
    /// Runs the CLI with specified environment variables and arguments
    /// </summary>
    public async Task<CliTestResult> RunAsync(Dictionary<string, string>? environmentVariables, params string[] args)
    {
        return await RunAsync(_defaultTimeout, environmentVariables, args);
    }

    /// <summary>
    /// Runs the CLI with specified timeout and arguments
    /// </summary>
    public async Task<CliTestResult> RunAsync(TimeSpan timeout, params string[] args)
    {
        return await RunAsync(timeout, null, args);
    }

    /// <summary>
    /// Runs the CLI with specified timeout, environment variables, and arguments
    /// </summary>
    public async Task<CliTestResult> RunAsync(TimeSpan timeout, Dictionary<string, string>? environmentVariables, params string[] args)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = _executablePath,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            CreateNoWindow = true,
            WorkingDirectory = Path.GetDirectoryName(_executablePath)
        };

        // Add arguments to the process
        foreach (var arg in args)
        {
            process.StartInfo.ArgumentList.Add(arg);
        }

        // Add environment variables from the runner instance first
        foreach (var (name, value) in _environmentVariables)
        {
            process.StartInfo.EnvironmentVariables[name] = value;
        }

        // Add environment variables from the parameter (these override instance variables)
        if (environmentVariables != null)
        {
            foreach (var (name, value) in environmentVariables)
            {
                process.StartInfo.EnvironmentVariables[name] = value;
            }
        }

        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();
        var outputCompleted = new TaskCompletionSource<bool>();
        var errorCompleted = new TaskCompletionSource<bool>();

        process.OutputDataReceived += (sender, e) =>
        {
            if (e.Data == null)
            {
                outputCompleted.SetResult(true);
            }
            else
            {
                outputBuilder.AppendLine(e.Data);
                if (_verbose) Console.WriteLine($"[OUT] {e.Data}");
            }
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (e.Data == null)
            {
                errorCompleted.SetResult(true);
            }
            else
            {
                errorBuilder.AppendLine(e.Data);
                if (_verbose) Console.WriteLine($"[ERR] {e.Data}");
            }
        };

        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            // Wait for process to complete with timeout
            using var cts = new CancellationTokenSource(timeout);
            
            try
            {
                await process.WaitForExitAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                // Process timed out, kill it
                try 
                { 
                    if (!process.HasExited)
                    {
                        process.Kill(entireProcessTree: true);
                        await process.WaitForExitAsync(); // Wait for graceful shutdown
                    }
                } 
                catch { /* Ignore errors during cleanup */ }
                
                throw new TimeoutException($"CLI process timed out after {timeout.TotalSeconds} seconds");
            }

            // Wait for output streams to complete
            await Task.WhenAll(outputCompleted.Task, errorCompleted.Task);
        }
        finally
        {
            stopwatch.Stop();
        }

        // Combine all environment variables for the result
        var allEnvironmentVariables = new Dictionary<string, string>(_environmentVariables);
        if (environmentVariables != null)
        {
            foreach (var (name, value) in environmentVariables)
            {
                allEnvironmentVariables[name] = value;
            }
        }

        return new CliTestResult
        {
            ExitCode = process.ExitCode,
            StandardOutput = outputBuilder.ToString().TrimEnd(),
            StandardError = errorBuilder.ToString().TrimEnd(),
            ExecutionTime = stopwatch.Elapsed,
            Command = $"{_executablePath} {string.Join(" ", args)}",
            EnvironmentVariables = allEnvironmentVariables
        };
    }

    /// <summary>
    /// Runs the CLI and expects it to fail with a specific exit code
    /// </summary>
    public async Task<CliTestResult> RunExpectingFailureAsync(int expectedExitCode, params string[] args)
    {
        return await RunExpectingFailureAsync(expectedExitCode, null, args);
    }

    /// <summary>
    /// Runs the CLI with environment variables and expects it to fail with a specific exit code
    /// </summary>
    public async Task<CliTestResult> RunExpectingFailureAsync(int expectedExitCode, Dictionary<string, string>? environmentVariables, params string[] args)
    {
        var result = await RunAsync(_defaultTimeout, environmentVariables, args);
        if (result.ExitCode != expectedExitCode)
        {
            throw new InvalidOperationException($"Expected exit code {expectedExitCode} but got {result.ExitCode}");
        }
        return result;
    }

    /// <summary>
    /// Runs multiple CLI commands in sequence and returns all results
    /// </summary>
    public async Task<List<CliTestResult>> RunSequenceAsync(params string[][] commandSequence)
    {
        return await RunSequenceAsync(null, commandSequence);
    }

    /// <summary>
    /// Runs multiple CLI commands in sequence with environment variables and returns all results
    /// </summary>
    public async Task<List<CliTestResult>> RunSequenceAsync(Dictionary<string, string>? environmentVariables, params string[][] commandSequence)
    {
        var results = new List<CliTestResult>();
        
        foreach (var args in commandSequence)
        {
            var result = await RunAsync(_defaultTimeout, environmentVariables, args);
            results.Add(result);
            
            // If any command fails, stop the sequence
            if (!result.IsSuccess)
            {
                break;
            }
        }
        
        return results;
    }

    /// <summary>
    /// Validates that the test executable exists and is accessible
    /// </summary>
    public async Task<bool> ValidateExecutableAsync()
    {
        try
        {
            var result = await RunAsync(TimeSpan.FromSeconds(5), "--version");
            return result.IsSuccess;
        }
        catch
        {
            return false;
        }
    }

    public string ExecutablePath => _executablePath;
    public bool IsVerbose => _verbose;
    public TimeSpan DefaultTimeout => _defaultTimeout;
}

public class CliTestResult
{
    public int ExitCode { get; set; }
    public string StandardOutput { get; set; } = "";
    public string StandardError { get; set; } = "";
    public TimeSpan ExecutionTime { get; set; }
    public string Command { get; set; } = "";
    public Dictionary<string, string> EnvironmentVariables { get; set; } = [];

    public bool IsSuccess => ExitCode == 0;
    public bool HasOutput => !string.IsNullOrWhiteSpace(StandardOutput);
    public bool HasError => !string.IsNullOrWhiteSpace(StandardError);
    
    /// <summary>
    /// Gets the combined output (stdout + stderr)
    /// </summary>
    public string CombinedOutput => $"{StandardOutput}\n{StandardError}".Trim();
    
    /// <summary>
    /// Gets output lines for easier assertion testing
    /// </summary>
    public string[] OutputLines => StandardOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries);
    
    /// <summary>
    /// Gets error lines for easier assertion testing
    /// </summary>
    public string[] ErrorLines => StandardError.Split('\n', StringSplitOptions.RemoveEmptyEntries);

    /// <summary>
    /// Returns a formatted string representation of the test result
    /// </summary>
    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Command: {Command}");
        sb.AppendLine($"Exit Code: {ExitCode}");
        sb.AppendLine($"Execution Time: {ExecutionTime.TotalMilliseconds:F0}ms");
        
        if (HasOutput)
        {
            sb.AppendLine("Output:");
            sb.AppendLine(StandardOutput);
        }
        
        if (HasError)
        {
            sb.AppendLine("Error:");
            sb.AppendLine(StandardError);
        }
        
        return sb.ToString();
    }
}