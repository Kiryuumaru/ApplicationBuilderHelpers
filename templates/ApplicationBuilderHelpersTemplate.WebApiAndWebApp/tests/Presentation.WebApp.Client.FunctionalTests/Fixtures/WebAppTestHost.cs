using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text.Json;

namespace Presentation.WebApp.Client.FunctionalTests.Fixtures;

/// <summary>
/// Test host that runs the Blazor WebApp using ASP.NET Core.
/// Serves static files and provides dynamic configuration for the API base URL.
/// </summary>
public class WebAppTestHost : IAsyncDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly int _port;
    private readonly string _webApiUrl;
    private WebApplication? _app;

    public string BaseUrl => $"http://127.0.0.1:{_port}";

    public WebAppTestHost(ITestOutputHelper output, string webApiUrl, int port = 0)
    {
        _output = output;
        _webApiUrl = webApiUrl;
        _port = port == 0 ? GetAvailablePort() : port;
    }

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

        _output.WriteLine($"[WEBAPP] Starting WebApp on {BaseUrl}...");

        var basePath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "src", "Presentation.WebApp.Client"));

        // Try Release first, then Debug
        var releaseDir = Path.Combine(basePath, "bin", "Release", "net10.0", "wwwroot");
        var debugDir = Path.Combine(basePath, "bin", "Debug", "net10.0", "wwwroot");
        
        var webAppOutputDir = Directory.Exists(releaseDir) ? releaseDir : debugDir;

        var webAppSourceDir = Path.Combine(basePath, "wwwroot");

        _output.WriteLine($"[WEBAPP] Output wwwroot dir: {webAppOutputDir}");
        _output.WriteLine($"[WEBAPP] Source wwwroot dir: {webAppSourceDir}");

        if (!Directory.Exists(webAppOutputDir))
        {
            throw new DirectoryNotFoundException($"WebApp output directory not found at {releaseDir} or {debugDir}. Ensure Presentation.WebApp.Client is built.");
        }

        if (!Directory.Exists(webAppSourceDir))
        {
            throw new DirectoryNotFoundException($"WebApp source directory not found at {webAppSourceDir}. Ensure Presentation.WebApp project exists.");
        }

        CopySourceFilesToOutput(webAppSourceDir, webAppOutputDir);

        var indexHtml = Path.Combine(webAppOutputDir, "index.html");
        if (!File.Exists(indexHtml))
        {
            throw new FileNotFoundException($"index.html not found at {indexHtml}");
        }

        // Create dynamic appsettings.json with the WebApi URL
        var appSettingsPath = Path.Combine(webAppOutputDir, "appsettings.json");
        var appSettings = new { ApiBaseAddress = _webApiUrl };
        var appSettingsJson = JsonSerializer.Serialize(appSettings);
        await File.WriteAllTextAsync(appSettingsPath, appSettingsJson);
        _output.WriteLine($"[WEBAPP] Created appsettings.json: {appSettingsJson}");

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls(BaseUrl);
        builder.Logging.ClearProviders();
        builder.Logging.AddProvider(new XUnitLoggerProvider(_output, "[WEBAPP]"));
        
        // Add HttpClient for proxying API requests
        builder.Services.AddHttpClient("ApiProxy", client =>
        {
            client.BaseAddress = new Uri(_webApiUrl);
        });

        _app = builder.Build();

        var fileProvider = new PhysicalFileProvider(webAppOutputDir);

        // Proxy /api/* requests to the WebAPI
        _app.Use(async (context, next) =>
        {
            if (context.Request.Path.StartsWithSegments("/api", out var remainingPath))
            {
                var httpClientFactory = context.RequestServices.GetRequiredService<IHttpClientFactory>();
                var client = httpClientFactory.CreateClient("ApiProxy");
                var targetPath = "/api" + remainingPath + context.Request.QueryString;
                _output.WriteLine($"[WEBAPP] Proxying {context.Request.Method} {context.Request.Path} -> {_webApiUrl}{targetPath}");

                var requestMessage = new HttpRequestMessage
                {
                    Method = new HttpMethod(context.Request.Method),
                    RequestUri = new Uri($"{_webApiUrl}{targetPath}")
                };

                // Copy request headers
                foreach (var header in context.Request.Headers)
                {
                    if (!header.Key.StartsWith("Host", StringComparison.OrdinalIgnoreCase) &&
                        !header.Key.StartsWith("Content-Length", StringComparison.OrdinalIgnoreCase) &&
                        !header.Key.StartsWith("Content-Type", StringComparison.OrdinalIgnoreCase))
                    {
                        requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
                    }
                }

                // Copy request body for POST/PUT/PATCH
                if (context.Request.ContentLength > 0 || context.Request.Headers.ContainsKey("Transfer-Encoding"))
                {
                    requestMessage.Content = new StreamContent(context.Request.Body);
                    if (context.Request.ContentType != null)
                    {
                        requestMessage.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(
                            context.Request.ContentType.Split(';')[0].Trim());
                    }
                }

                var response = await client.SendAsync(requestMessage);

                context.Response.StatusCode = (int)response.StatusCode;

                // Copy response headers
                foreach (var header in response.Headers)
                {
                    context.Response.Headers[header.Key] = header.Value.ToArray();
                }
                foreach (var header in response.Content.Headers)
                {
                    context.Response.Headers[header.Key] = header.Value.ToArray();
                }

                // Remove headers that would cause issues
                context.Response.Headers.Remove("Transfer-Encoding");

                await response.Content.CopyToAsync(context.Response.Body);
                return; // Short-circuit the pipeline
            }

            await next();
        });

        // Logging middleware for debugging (after API proxy)
        _app.Use(async (context, next) =>
        {
            _output.WriteLine($"[WEBAPP] {context.Request.Method} {context.Request.Path}");
            await next();
        });

        // Serve static files from the Blazor WASM build output with no caching
        _app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = fileProvider,
            ServeUnknownFileTypes = true,
            OnPrepareResponse = ctx =>
            {
                // Disable caching for all files during tests
                ctx.Context.Response.Headers.CacheControl = "no-cache, no-store, must-revalidate";
                ctx.Context.Response.Headers.Pragma = "no-cache";
                ctx.Context.Response.Headers.Expires = "0";
            }
        });

        // SPA fallback: serve index.html for unknown routes
        _app.MapFallbackToFile("index.html", new StaticFileOptions
        {
            FileProvider = fileProvider,
            OnPrepareResponse = ctx =>
            {
                ctx.Context.Response.Headers.CacheControl = "no-cache, no-store, must-revalidate";
            }
        });

        // Start the server
        await _app.StartAsync();

        // Wait for server to be ready
        await WaitForServerReady(timeout.Value);

        _output.WriteLine("[WEBAPP] Started successfully");
    }

    private void CopySourceFilesToOutput(string sourceDir, string outputDir)
    {
        foreach (var sourceFile in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceDir, sourceFile);
            var destFile = Path.Combine(outputDir, relativePath);
            var destDir = Path.GetDirectoryName(destFile);

            if (destDir != null && !Directory.Exists(destDir))
            {
                Directory.CreateDirectory(destDir);
            }

            if (!File.Exists(destFile) || File.GetLastWriteTimeUtc(sourceFile) > File.GetLastWriteTimeUtc(destFile))
            {
                File.Copy(sourceFile, destFile, overwrite: true);
                _output.WriteLine($"[WEBAPP] Copied {relativePath}");
            }
        }
    }

    private async Task WaitForServerReady(TimeSpan timeout)
    {
        var sw = Stopwatch.StartNew();
        using var client = new HttpClient();

        while (sw.Elapsed < timeout)
        {
            try
            {
                var response = await client.GetAsync(BaseUrl);
                if (response.IsSuccessStatusCode || (int)response.StatusCode < 500)
                {
                    _output.WriteLine($"[WEBAPP] Server responded with {response.StatusCode} after {sw.ElapsedMilliseconds}ms");
                    return;
                }
            }
            catch (HttpRequestException)
            {
                // Server not ready yet
            }

            await Task.Delay(100);
        }

        throw new TimeoutException($"WebApp server did not start within {timeout.TotalSeconds}s");
    }

    public async ValueTask DisposeAsync()
    {
        _output.WriteLine("[WEBAPP] Shutting down...");

        if (_app != null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }

        _output.WriteLine("[WEBAPP] Stopped");
    }
}

/// <summary>
/// Simple logger provider for xUnit output.
/// </summary>
file class XUnitLoggerProvider(ITestOutputHelper output, string prefix) : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName) => new XUnitLogger(output, prefix);
    public void Dispose() { }
}

file class XUnitLogger(ITestOutputHelper output, string prefix) : ILogger
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (IsEnabled(logLevel))
        {
            output.WriteLine($"{prefix} {formatter(state, exception)}");
        }
    }
}
