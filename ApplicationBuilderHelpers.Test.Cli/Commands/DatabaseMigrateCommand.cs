using ApplicationBuilderHelpers.Attributes;
using Microsoft.Extensions.Hosting;

namespace ApplicationBuilderHelpers.Test.Cli.Commands;

[Command("database migrate", "Run database migrations")]
internal class DatabaseMigrateCommand : BaseCommand
{
    [CommandArgument("connection", Description = "Database connection string or name", Position = 0, Required = false)]
    public string? ConnectionString { get; set; }

    [CommandOption('t', "target", Description = "Target migration version")]
    public string? TargetMigration { get; set; }

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
        Console.WriteLine("Running database migrations");
        Console.WriteLine($"Connection: {ConnectionString ?? "default"}");
        Console.WriteLine($"Target Migration: {TargetMigration ?? "latest"}");
        Console.WriteLine($"Generate Script: {GenerateScript}");
        Console.WriteLine($"Dry Run: {DryRun}");
        Console.WriteLine($"Force: {Force}");
        Console.WriteLine($"Create Backup: {CreateBackup}");
        Console.WriteLine($"Timeout: {CommandTimeout}s");

        cancellationTokenSource.Cancel();
        return ValueTask.CompletedTask;
    }
}