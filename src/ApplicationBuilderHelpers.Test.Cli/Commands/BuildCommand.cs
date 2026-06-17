using ApplicationBuilderHelpers.Attributes;
using Microsoft.Extensions.Hosting;

namespace ApplicationBuilderHelpers.Test.Cli.Commands;

[Command("build", "Build the project with optional deployment")]
internal class BuildCommand : BaseCommand
{
    [CommandOption('r', "release", Description = "Build in release mode")]
    public bool Release { get; set; }

    [CommandOption('o', "output", Description = "Output directory", EnvironmentVariable = "BUILD_OUTPUT")]
    public string? OutputPath { get; set; } = "bin/Debug";

    [CommandOption("target", Description = "Build target", FromAmong = ["Debug", "Release", "Test"])]
    public string Target { get; set; } = "Debug";

    [CommandOption('d', "define", Description = "Define preprocessor symbols")]
    public string[] Defines { get; set; } = [];

    [CommandOption("framework", Description = "Target framework", FromAmong = ["net6.0", "net7.0", "net8.0", "net9.0"])]
    public string? Framework { get; set; }

    [CommandOption("arch", Description = "Target architecture", FromAmong = ["x86", "x64", "arm", "arm64"])]
    public string? Architecture { get; set; }

    [CommandOption("restore", Description = "Restore packages before building")]
    public bool Restore { get; set; }

    [CommandOption("no-build", Description = "Skip building the project")]
    public bool NoBuild { get; set; }

    [CommandOption("verbosity", Description = "Build verbosity level", FromAmong = ["quiet", "minimal", "normal", "detailed", "diagnostic"])]
    public string Verbosity { get; set; } = "normal";

    [CommandArgument("project", Description = "Project file to build", Position = 0, Required = true)]
    public required string ProjectFile { get; set; }

    protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
    {
        // Print debug info if requested
        PrintDebugInfo();

        Console.WriteLine($"Building project: {ProjectFile}");
        Console.WriteLine($"Target: {Target}");
        Console.WriteLine($"Release mode: {Release}");
        Console.WriteLine($"Output: {OutputPath}");
        Console.WriteLine($"Restore: {Restore}");
        Console.WriteLine($"No Build: {NoBuild}");
        Console.WriteLine($"Verbosity: {Verbosity}");
        
        if (!string.IsNullOrEmpty(Framework))
        {
            Console.WriteLine($"Framework: {Framework}");
        }
        
        if (!string.IsNullOrEmpty(Architecture))
        {
            Console.WriteLine($"Architecture: {Architecture}");
        }
        
        if (Defines.Length > 0)
        {
            Console.WriteLine($"Defines: {string.Join(", ", Defines)}");
        }

        if (!Quiet)
        {
            Console.WriteLine("Build completed successfully!");
        }

        cancellationTokenSource.Cancel(); // Cancel the application host to stop further processing
        return ValueTask.CompletedTask;
    }
}