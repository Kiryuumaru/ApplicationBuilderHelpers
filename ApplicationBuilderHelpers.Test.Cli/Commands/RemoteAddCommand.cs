using ApplicationBuilderHelpers.Attributes;
using Microsoft.Extensions.Hosting;

namespace ApplicationBuilderHelpers.Test.Cli.Commands;

[Command("remote add", "Add a remote repository")]
internal class RemoteAddCommand : BaseCommand
{
    [CommandArgument("name", Description = "Remote name", Position = 0, Required = true)]
    public required string Name { get; set; }

    [CommandArgument("url", Description = "Remote URL", Position = 1, Required = true)]
    public required string Url { get; set; }

    [CommandOption('f', "fetch", Description = "Fetch the remote immediately after adding")]
    public bool Fetch { get; set; }

    [CommandOption('t', "track", Description = "Set up tracking for specific branches")]
    public string[] TrackBranches { get; set; } = [];

    [CommandOption("tags", Description = "Import tags from remote")]
    public bool Tags { get; set; } = true;

    [CommandOption("mirror", Description = "Set up remote as a mirror")]
    public bool Mirror { get; set; }

    [CommandOption("set-head", Description = "Set default branch for remote")]
    public string? DefaultBranch { get; set; }

    protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
    {
        // Print debug info if requested
        PrintDebugInfo();

        Console.WriteLine("Adding remote repository");
        Console.WriteLine($"Name: {Name}");
        Console.WriteLine($"URL: {Url}");
        Console.WriteLine($"Fetch: {Fetch}");
        Console.WriteLine($"Tags: {Tags}");
        Console.WriteLine($"Mirror: {Mirror}");

        if (TrackBranches.Length > 0)
        {
            Console.WriteLine($"Track branches: {string.Join(", ", TrackBranches)}");
        }

        if (!string.IsNullOrEmpty(DefaultBranch))
        {
            Console.WriteLine($"Default branch: {DefaultBranch}");
        }

        if (!Quiet)
        {
            Console.WriteLine("Remote repository added successfully!");
        }

        cancellationTokenSource.Cancel();
        return ValueTask.CompletedTask;
    }
}