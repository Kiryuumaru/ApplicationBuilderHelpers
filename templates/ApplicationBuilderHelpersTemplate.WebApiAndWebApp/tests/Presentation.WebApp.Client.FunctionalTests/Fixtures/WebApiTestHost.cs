using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace Presentation.WebApp.Client.FunctionalTests.Fixtures;

/// <summary>
/// Test host that runs the WebApi application as a subprocess for functional testing.
/// </summary>
public class WebApiTestHost : IAsyncDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly HttpClient _httpClient;
    private readonly int _port;
    private Process? _process;
    private string? _publishDir;

    public string BaseUrl => $"http://localhost:{_port}";
    public HttpClient HttpClient => _httpClient;

    public WebApiTestHost(ITestOutputHelper output, int port = 0)
    {
        _output = output;
        _port = port == 0 ? GetAvailablePort() : port;
        _httpClient = new HttpClient { BaseAddress = new Uri(BaseUrl) };
    }

    /// <summary>
    /// Gets an available port by binding to port 0 and letting the OS assign one.
    /// This ensures no port collisions when running tests in parallel.
    /// </summary>
    private static int GetAvailablePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    public async Task StartAsync(TimeSpan? timeout = null)
    {
        timeout ??= TimeSpan.FromSeconds(60);

        _output.WriteLine($"[WEBAPI] Starting WebApi on {BaseUrl}...");

        // Find the WebApi server project directory
        var webApiProjectDir = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "src", "Presentation.WebApp.Server"));

        if (!Directory.Exists(webApiProjectDir))
        {
            throw new DirectoryNotFoundException($"WebApi project directory not found at {webApiProjectDir}.");
        }

        // Publish to a unique temp directory for this test run
        // Published output has all static assets bundled correctly
        _publishDir = Path.Combine(Path.GetTempPath(), $"FunctionalTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_publishDir);

        _output.WriteLine($"[WEBAPI] Publishing to: {_publishDir}");

        var publishProcess = Process.Start(new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"publish \"{webApiProjectDir}\" -o \"{_publishDir}\" -c Release",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        });

        if (publishProcess == null)
        {
            throw new InvalidOperationException("Failed to start dotnet publish process");
        }

        // Capture publish output for debugging
        var publishOutput = await publishProcess.StandardOutput.ReadToEndAsync();
        var publishError = await publishProcess.StandardError.ReadToEndAsync();
        await publishProcess.WaitForExitAsync();

        if (publishProcess.ExitCode != 0)
        {
            _output.WriteLine($"[WEBAPI] Publish stdout: {publishOutput}");
            _output.WriteLine($"[WEBAPI] Publish stderr: {publishError}");
            throw new InvalidOperationException($"dotnet publish failed with exit code {publishProcess.ExitCode}");
        }

        _output.WriteLine("[WEBAPI] Publish completed");

        // Find the exe file in publish output
        var exePath = Directory.GetFiles(_publishDir, "*.exe")
            .FirstOrDefault(f => !Path.GetFileName(f).StartsWith("createdump", StringComparison.OrdinalIgnoreCase));

        if (exePath == null || !File.Exists(exePath))
        {
            throw new FileNotFoundException($"WebApi executable not found in {_publishDir}. Publish failed.");
        }

        // Per build-commands.instructions.md:
        // Always cd to the directory AND run the executable with absolute path
        var args = $"--urls {BaseUrl}";

        _output.WriteLine($"[WEBAPI] Starting: {exePath} {args}");
        _output.WriteLine($"[WEBAPI] Working directory: {_publishDir}");

        var startInfo = new ProcessStartInfo
        {
            FileName = exePath,
            Arguments = args,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = _publishDir  // Critical: Must run FROM the publish directory
        };

        startInfo.Environment["ASPNETCORE_URLS"] = BaseUrl;
        startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
        startInfo.Environment["DOTNET_LAUNCH_PROFILE"] = "";
        startInfo.Environment["SQLITE_CONNECTION_STRING"] = $"Data Source={Guid.NewGuid()};Mode=Memory;Cache=Shared";

        _process = new Process { StartInfo = startInfo };

        _process.OutputDataReceived += (_, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                _output.WriteLine($"[WEBAPI] {e.Data}");
            }
        };

        _process.ErrorDataReceived += (_, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                _output.WriteLine($"[WEBAPI ERR] {e.Data}");
            }
        };

        _process.Start();
        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();

        _output.WriteLine($"[WEBAPI] Process started (PID: {_process.Id})");

        await WaitForServerReady(timeout.Value);

        _output.WriteLine("[WEBAPI] Started successfully");
    }

    private async Task WaitForServerReady(TimeSpan timeout)
    {
        var sw = Stopwatch.StartNew();

        while (sw.Elapsed < timeout)
        {
            if (_process != null && _process.HasExited)
            {
                throw new InvalidOperationException($"WebApi process exited unexpectedly with code {_process.ExitCode}.");
            }

            try
            {
                var response = await _httpClient.GetAsync("/");
                if (response.IsSuccessStatusCode || (int)response.StatusCode < 500)
                {
                    _output.WriteLine($"[WEBAPI] Server responded with {response.StatusCode} after {sw.ElapsedMilliseconds}ms");
                    return;
                }
            }
            catch (HttpRequestException)
            {
                // Server not ready yet
            }

            await Task.Delay(100);
        }

        throw new TimeoutException($"WebApi did not start within {timeout.TotalSeconds}s");
    }

    public async ValueTask DisposeAsync()
    {
        _output.WriteLine("[WEBAPI] Shutting down...");

        if (_process != null && !_process.HasExited)
        {
            try
            {
                _process.Kill(entireProcessTree: true);
                await _process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(5));
            }
            catch (Exception ex)
            {
                _output.WriteLine($"[WEBAPI] Error killing process: {ex.Message}");
            }
        }

        _process?.Dispose();
        _httpClient.Dispose();

        // Clean up publish directory
        if (_publishDir != null && Directory.Exists(_publishDir))
        {
            try
            {
                Directory.Delete(_publishDir, recursive: true);
                _output.WriteLine($"[WEBAPI] Cleaned up publish directory: {_publishDir}");
            }
            catch (Exception ex)
            {
                _output.WriteLine($"[WEBAPI] Error cleaning up publish directory: {ex.Message}");
            }
        }

        _output.WriteLine("[WEBAPI] Stopped");
    }
}
