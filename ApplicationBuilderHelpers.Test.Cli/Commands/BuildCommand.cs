using ApplicationBuilderHelpers.Attributes;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApplicationBuilderHelpers.Test.Cli.Commands;

[Command("build", "Build the project with optional deployment")]
internal class BuildCommand : BaseCommand
{
    [CommandOption('r', "release", Description = "Build in release mode")]
    public bool Release { get; set; }

    [CommandOption('o', "output", Description = "Output directory")]
    public string? OutputPath { get; set; }

    [CommandOption("target", Description = "Build target", FromAmong = ["Debug", "Release", "Test"])]
    public string Target { get; set; } = "Debug";

    [CommandArgument("project", Description = "Project file to build", Position = 0, Required = true)]
    public required string ProjectFile { get; set; }

    protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
    {
        Console.WriteLine($"Building project: {ProjectFile}");
        Console.WriteLine($"Target: {Target}");
        Console.WriteLine($"Release mode: {Release}");
        if (!string.IsNullOrEmpty(OutputPath))
        {
            Console.WriteLine($"Output: {OutputPath}");
        }

        cancellationTokenSource.Cancel(); // Cancel the application host to stop further processing
        return ValueTask.CompletedTask;
    }
}