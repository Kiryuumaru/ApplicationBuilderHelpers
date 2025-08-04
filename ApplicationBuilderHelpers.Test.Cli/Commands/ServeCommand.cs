using ApplicationBuilderHelpers.Attributes;
using Microsoft.Extensions.Hosting;

namespace ApplicationBuilderHelpers.Test.Cli.Commands;

[Command("serve", "Start a development server")]
internal class ServeCommand : BaseCommand
{
    [CommandOption('p', "port", Description = "Port number to listen on")]
    public int Port { get; set; } = 8080;

    [CommandOption('h', "host", Description = "Host address to bind to")]
    public string Host { get; set; } = "localhost";

    [CommandOption("https", Description = "Enable HTTPS")]
    public bool UseHttps { get; set; }

    [CommandOption("cert", Description = "Path to SSL certificate file")]
    public string? CertificatePath { get; set; }

    [CommandOption('w', "watch", Description = "Enable file watching for auto-reload")]
    public bool Watch { get; set; } = true;

    [CommandOption("environment", Description = "Environment mode", FromAmong = ["development", "staging", "production"])]
    public string Environment { get; set; } = "development";

    [CommandOption('o', "open", Description = "Open browser automatically")]
    public bool OpenBrowser { get; set; }

    [CommandOption("cors", Description = "Enable CORS")]
    public bool EnableCors { get; set; } = true;

    [CommandOption("api-prefix", Description = "API route prefix")]
    public string ApiPrefix { get; set; } = "/api";

    [CommandOption('m', "middleware", Description = "Additional middleware to enable")]
    public string[] Middleware { get; set; } = [];

    protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
    {
        Console.WriteLine($"Starting development server...");
        Console.WriteLine($"Host: {Host}");
        Console.WriteLine($"Port: {Port}");
        Console.WriteLine($"HTTPS: {UseHttps}");
        Console.WriteLine($"Environment: {Environment}");
        Console.WriteLine($"Watch: {Watch}");
        Console.WriteLine($"Open Browser: {OpenBrowser}");
        Console.WriteLine($"CORS: {EnableCors}");
        Console.WriteLine($"API Prefix: {ApiPrefix}");

        if (!string.IsNullOrEmpty(CertificatePath))
        {
            Console.WriteLine($"Certificate: {CertificatePath}");
        }

        if (Middleware.Length > 0)
        {
            Console.WriteLine($"Middleware: {string.Join(", ", Middleware)}");
        }

        Console.WriteLine($"Server would be running at: {(UseHttps ? "https" : "http")}://{Host}:{Port}");

        cancellationTokenSource.Cancel();
        return ValueTask.CompletedTask;
    }
}