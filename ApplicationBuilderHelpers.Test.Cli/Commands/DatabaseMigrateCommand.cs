using ApplicationBuilderHelpers.Attributes;
using Microsoft.Extensions.Hosting;

namespace ApplicationBuilderHelpers.Test.Cli.Commands;

[Command("database migrate", "Run database migrations")]
internal class DatabaseMigrateCommand : BaseCommand
{
    [CommandOption("connection-string", Description = "Database connection string")]
    public string? ConnectionString { get; set; }

    [CommandOption("target-version", Description = "Target migration version")]
    public string? TargetVersion { get; set; }

    [CommandOption('s', "script", Description = "Generate migration script instead of applying")]
    public bool GenerateScript { get; set; }

    [CommandOption("dry-run", Description = "Show what migrations would be applied")]
    public bool DryRun { get; set; }

    [CommandOption('f', "force", Description = "Force migration even if data loss is possible")]
    public bool Force { get; set; }

    [CommandOption("backup", Description = "Create backup before migration")]
    public bool CreateBackup { get; set; } = true;

    [CommandOption("timeout", Description = "Command timeout in seconds")]
    public int CommandTimeout { get; set; } = 300;

    protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
    {
        // Print debug info if requested
        PrintDebugInfo();

        Console.WriteLine("Running database migration");
        Console.WriteLine($"Connection: {ConnectionString ?? "default"}");
        Console.WriteLine($"Target Version: {TargetVersion ?? "latest"}");
        Console.WriteLine($"Generate Script: {GenerateScript}");
        Console.WriteLine($"Dry Run: {DryRun}");
        Console.WriteLine($"Force: {Force}");
        Console.WriteLine($"Create Backup: {CreateBackup}");
        Console.WriteLine($"Timeout: {CommandTimeout}s");

        if (!Quiet)
        {
            Console.WriteLine("Database migration completed successfully!");
        }

        cancellationTokenSource.Cancel();
        return ValueTask.CompletedTask;
    }
}