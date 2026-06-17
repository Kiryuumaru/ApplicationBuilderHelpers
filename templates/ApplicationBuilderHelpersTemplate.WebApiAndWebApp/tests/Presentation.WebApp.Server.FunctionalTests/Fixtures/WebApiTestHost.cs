using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using Microsoft.AspNetCore.SignalR.Client;

namespace Presentation.WebApp.Server.FunctionalTests.Fixtures;

/// <summary>
/// Test host that runs the WebApi application as a subprocess for functional testing.
/// Since WebApi uses ApplicationBuilderHelpers CLI pattern, WebApplicationFactory doesn't work.
/// Instead, we start the actual application and test against it via HTTP.
/// Each instance gets:
/// - A unique random port (isolated network)
/// - A unique file-based SQLite database (isolated data)
/// </summary>
public class WebApiTestHost : IAsyncDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly HttpClient _httpClient;
    private readonly int _port;
    private readonly string _dbPath;
    private Process? _process;

    public string BaseUrl => $"http://localhost:{_port}";
    public HttpClient HttpClient => _httpClient;

    public WebApiTestHost(ITestOutputHelper output, int port = 0)
    {
        _output = output;
        _port = port == 0 ? GetAvailablePort() : port;
        _httpClient = new HttpClient { BaseAddress = new Uri(BaseUrl) };
        
        // Create a unique temp database file for complete isolation
        // File-based ensures no cross-process memory sharing issues
        _dbPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid():N}.db");
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

    /// <summary>
    /// Starts the WebApi application as a subprocess.
    /// </summary>
    public async Task StartAsync(TimeSpan? timeout = null)
    {
        timeout ??= TimeSpan.FromSeconds(60);

        _output.WriteLine($"[HOST] Starting WebApi on {BaseUrl}...");
        _output.WriteLine($"[HOST] AppContext.BaseDirectory: {AppContext.BaseDirectory}");

        // Detect configuration from current test output directory (contains Debug or Release)
        var configuration = AppContext.BaseDirectory.Contains("Release", StringComparison.OrdinalIgnoreCase)
            ? "Release"
            : "Debug";

        // Find the WebApi output directory
        var webApiOutputDir = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "src", "Presentation.WebApp.Server", "bin", configuration, "net10.0"));

        _output.WriteLine($"[HOST] WebApi output dir: {webApiOutputDir}");

        if (!Directory.Exists(webApiOutputDir))
        {
            throw new DirectoryNotFoundException($"WebApi output directory not found at {webApiOutputDir}. Build the WebApi project first.");
        }

        // Find the executable dynamically by looking for .runtimeconfig.json files
        // The main application executable has a matching .runtimeconfig.json (dependency dlls do not)
        // File is like "sampleapp.runtimeconfig.json", we need to extract "sampleapp"
        var exePath = Directory.GetFiles(webApiOutputDir, "*.runtimeconfig.json")
            .Select(rc => Path.GetFileName(rc).Replace(".runtimeconfig.json", ""))
            .Select(name => Path.Combine(webApiOutputDir, name + ".exe"))
            .FirstOrDefault(File.Exists);

        // Fallback to dll if exe doesn't exist (Linux/macOS)
        if (exePath == null)
        {
            exePath = Directory.GetFiles(webApiOutputDir, "*.runtimeconfig.json")
                .Select(rc => Path.GetFileName(rc).Replace(".runtimeconfig.json", ""))
                .Select(name => Path.Combine(webApiOutputDir, name + ".dll"))
                .FirstOrDefault(File.Exists);
            _output.WriteLine($"[HOST] No .exe found, trying DLL: {exePath}");
        }

        _output.WriteLine($"[HOST] Using executable: {exePath}");

        if (exePath == null || !File.Exists(exePath))
        {
            throw new FileNotFoundException($"WebApi executable not found in {webApiOutputDir}. Build the WebApi project first.");
        }

        var isExe = exePath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase);
        var workDir = Path.GetDirectoryName(exePath);
        // Use a unique file-based database for complete test isolation
        var connectionString = $"Data Source={_dbPath}";
        var args = isExe 
            ? $"--urls {BaseUrl}"
            : $"\"{exePath}\" --urls {BaseUrl}";
        var fileName = isExe ? exePath : "dotnet";

        _output.WriteLine($"[HOST] FileName: {fileName}");
        _output.WriteLine($"[HOST] Arguments: {args}");
        _output.WriteLine($"[HOST] WorkingDirectory: {workDir}");
        _output.WriteLine($"[HOST] Database file: {_dbPath}");

        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = args,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = workDir
        };

        // Set environment variables for the child process
        startInfo.Environment["ASPNETCORE_URLS"] = BaseUrl;
        startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
        // Disable launch settings to avoid conflicts
        startInfo.Environment["DOTNET_LAUNCH_PROFILE"] = "";
        // SQLite connection string via environment variable
        startInfo.Environment["SQLITE_CONNECTION_STRING"] = connectionString;

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

        _output.WriteLine($"[HOST] Process started (PID: {_process.Id})");

        // Wait for the server to be ready
        await WaitForServerReady(timeout.Value);

        _output.WriteLine("[HOST] WebApi started successfully");
    }

    private async Task WaitForServerReady(TimeSpan timeout)
    {
        var sw = Stopwatch.StartNew();

        while (sw.Elapsed < timeout)
        {
            // Check if process has exited unexpectedly
            if (_process != null && _process.HasExited)
            {
                throw new InvalidOperationException($"WebApi process exited unexpectedly with code {_process.ExitCode}. Check the output logs above for errors.");
            }

            try
            {
                var response = await _httpClient.GetAsync("/");
                if (response.IsSuccessStatusCode || (int)response.StatusCode < 500)
                {
                    _output.WriteLine($"[HOST] Server responded with {response.StatusCode} after {sw.ElapsedMilliseconds}ms");
                    return;
                }
            }
            catch (HttpRequestException ex)
            {
                // Server not ready yet - log every 5 seconds
                if (sw.ElapsedMilliseconds % 5000 < 100)
                {
                    _output.WriteLine($"[HOST] Waiting for server... ({sw.ElapsedMilliseconds}ms elapsed, error: {ex.Message})");
                }
            }

            await Task.Delay(100);
        }

        throw new TimeoutException($"WebApi did not start within {timeout.TotalSeconds}s");
    }

    /// <summary>
    /// Creates a SignalR HubConnection to the specified hub.
    /// </summary>
    public HubConnection CreateHubConnection(string hubPath)
    {
        _output.WriteLine($"[HOST] Creating SignalR connection to {hubPath}");

        var hubUrl = new Uri(new Uri(BaseUrl), hubPath);

        return new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .Build();
    }

    /// <summary>
    /// Creates an HTTP client with authentication.
    /// </summary>
    public HttpClient CreateAuthenticatedClient(string token)
    {
        var client = new HttpClient { BaseAddress = new Uri(BaseUrl) };
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public async ValueTask DisposeAsync()
    {
        _output.WriteLine("[HOST] Shutting down WebApi...");

        if (_process != null && !_process.HasExited)
        {
            try
            {
                _process.Kill(entireProcessTree: true);
                await _process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(5));
            }
            catch (Exception ex)
            {
                _output.WriteLine($"[HOST] Error killing process: {ex.Message}");
            }
        }

        _process?.Dispose();
        _httpClient.Dispose();

        // Clean up the temp database file
        try
        {
            if (File.Exists(_dbPath))
            {
                File.Delete(_dbPath);
                _output.WriteLine($"[HOST] Deleted temp database: {_dbPath}");
            }
            // Also clean up WAL and SHM files if they exist
            var walPath = _dbPath + "-wal";
            var shmPath = _dbPath + "-shm";
            if (File.Exists(walPath)) File.Delete(walPath);
            if (File.Exists(shmPath)) File.Delete(shmPath);
        }
        catch (Exception ex)
        {
            _output.WriteLine($"[HOST] Warning: Could not delete temp database: {ex.Message}");
        }

        _output.WriteLine("[HOST] WebApi stopped");
    }
}
