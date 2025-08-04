using ApplicationBuilderHelpers.Attributes;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApplicationBuilderHelpers.Test.Cli.Commands;

[Command("deploy", "Deploy applications to various environments")]
internal class DeployCommand : BaseCommand
{
    [CommandArgument("environment", Description = "Target environment for deployment", Position = 0, Required = true)]
    public required string Environment { get; set; }

    [CommandArgument("version", Description = "Version to deploy", Position = 1, Required = false)]
    public string? Version { get; set; }

    [CommandOption('f', "force", Description = "Force deployment even if environment is not empty")]
    public bool Force { get; set; }

    [CommandOption('d', "dry-run", Description = "Perform a dry run without actual deployment")]
    public bool DryRun { get; set; }

    [CommandOption("strategy", Description = "Deployment strategy", FromAmong = ["blue-green", "rolling", "recreate", "canary"])]
    public string Strategy { get; set; } = "rolling";

    [CommandOption('r', "replicas", Description = "Number of replicas to deploy")]
    public int Replicas { get; set; } = 1;

    [CommandOption("timeout", Description = "Deployment timeout in minutes")]
    public int TimeoutMinutes { get; set; } = 30;

    [CommandOption('s', "services", Description = "Specific services to deploy")]
    public string[] Services { get; set; } = [];

    [CommandOption('e', "env-vars", Description = "Environment variables to set")]
    public string[] EnvironmentVariables { get; set; } = [];

    [CommandOption("health-check-url", Description = "URL for health checks", EnvironmentVariable = "HEALTH_CHECK_URL")]
    public string? HealthCheckUrl { get; set; }

    [CommandOption("rollback-on-failure", Description = "Automatically rollback on deployment failure")]
    public bool RollbackOnFailure { get; set; } = true;

    protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
    {
        Console.WriteLine($"Deploying to environment: {Environment}");
        Console.WriteLine($"Version: {Version ?? "latest"}");
        Console.WriteLine($"Strategy: {Strategy}");
        Console.WriteLine($"Replicas: {Replicas}");
        Console.WriteLine($"Timeout: {TimeoutMinutes} minutes");
        Console.WriteLine($"Force: {Force}");
        Console.WriteLine($"Dry Run: {DryRun}");
        Console.WriteLine($"Rollback on Failure: {RollbackOnFailure}");

        if (Services.Length > 0)
        {
            Console.WriteLine($"Services: {string.Join(", ", Services)}");
        }

        if (EnvironmentVariables.Length > 0)
        {
            Console.WriteLine($"Environment Variables: {string.Join(", ", EnvironmentVariables)}");
        }

        if (!string.IsNullOrEmpty(HealthCheckUrl))
        {
            Console.WriteLine($"Health Check URL: {HealthCheckUrl}");
        }

        cancellationTokenSource.Cancel();
        return ValueTask.CompletedTask;
    }
}