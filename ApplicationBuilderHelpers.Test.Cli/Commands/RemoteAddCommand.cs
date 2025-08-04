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
    public bool ImportTags { get; set; } = true;

    [CommandOption("mirror", Description = "Set up remote as a mirror", FromAmong = ["fetch", "push"])]
    public string? MirrorType { get; set; }

    [CommandOption("set-head", Description = "Set default branch for remote")]
    public string? DefaultBranch { get; set; }

    protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
    {
        Console.WriteLine($"Adding remote '{Name}' with URL: {Url}");
        Console.WriteLine($"Fetch immediately: {Fetch}");
        Console.WriteLine($"Import tags: {ImportTags}");

        if (TrackBranches.Length > 0)
        {
            Console.WriteLine($"Track branches: {string.Join(", ", TrackBranches)}");
        }

        if (!string.IsNullOrEmpty(MirrorType))
        {
            Console.WriteLine($"Mirror type: {MirrorType}");
        }

        if (!string.IsNullOrEmpty(DefaultBranch))
        {
            Console.WriteLine($"Default branch: {DefaultBranch}");
        }

        cancellationTokenSource.Cancel();
        return ValueTask.CompletedTask;
    }
}