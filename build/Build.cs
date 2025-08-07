using Nuke.Common.IO;
using Nuke.Common.Tools.DotNet;
using NukeBuildHelpers;
using NukeBuildHelpers.Common.Attributes;
using NukeBuildHelpers.Entry;
using NukeBuildHelpers.Entry.Extensions;
using NukeBuildHelpers.Runner.Abstraction;
using NukeBuildHelpers.RunContext.Extensions;
using NukeBuildHelpers.Common.Enums;

class Build : BaseNukeBuildHelpers
{
    public static int Main () => Execute<Build>(x => x.Interactive);

    public override string[] EnvironmentBranches { get; } = ["prerelease", "master"];

    public override string MainEnvironmentBranch { get; } = "master";

    [SecretVariable("NUGET_AUTH_TOKEN")]
    readonly string? NuGetAuthToken;

    [SecretVariable("GITHUB_TOKEN")]
    readonly string? GithubToken;

    public TestEntry ApplicationBuilderHelpersTest => _ => _
        .AppId("application_builder_helpers")
        .RunnerOS(RunnerOS.Windows2022)
        .ExecuteBeforeBuild(true)
        .Execute(context =>
        {
            DotNetTasks.DotNetBuild(_ => _
                .SetProjectFile(RootDirectory / "ApplicationBuilderHelpers.Test.Cli" / "ApplicationBuilderHelpers.Test.Cli.csproj")
                .SetConfiguration("Release"));
            DotNetTasks.DotNetBuild(_ => _
                .SetProjectFile(RootDirectory / "ApplicationBuilderHelpers.Test.Cli.UnitTest" / "ApplicationBuilderHelpers.Test.Cli.UnitTest.csproj")
                .SetConfiguration("Release"));
            DotNetTasks.DotNetTest(_ => _
                .SetProjectFile(RootDirectory / "ApplicationBuilderHelpers.Test.Cli.UnitTest" / "ApplicationBuilderHelpers.Test.Cli.UnitTest.csproj")
                .SetConfiguration("Release")
                .EnableNoLogo()
                .EnableNoRestore()
                .EnableNoBuild());
        });

    public BuildEntry ApplicationBuilderHelpersBuild => _ => _
        .AppId("application_builder_helpers")
        .RunnerOS(RunnerOS.Ubuntu2204)
        .Execute(context =>
        {
            string version = "0.0.0";
            string? releaseNotes = null;
            if (context.TryGetBumpContext(out var bumpContext))
            {
                version = bumpContext.AppVersion.Version.ToString();
                releaseNotes = bumpContext.AppVersion.ReleaseNotes;
            }
            else if (context.TryGetPullRequestContext(out var pullRequestContext))
            {
                version = pullRequestContext.AppVersion.Version.ToString();
            }
            OutputDirectory.DeleteDirectory();
            DotNetTasks.DotNetClean(_ => _
                .SetProject(RootDirectory / "ApplicationBuilderHelpers" / "ApplicationBuilderHelpers.csproj"));
            DotNetTasks.DotNetBuild(_ => _
                .SetProjectFile(RootDirectory / "ApplicationBuilderHelpers" / "ApplicationBuilderHelpers.csproj")
                .SetConfiguration("Release"));
            DotNetTasks.DotNetPack(_ => _
                .SetProject(RootDirectory / "ApplicationBuilderHelpers" / "ApplicationBuilderHelpers.csproj")
                .SetConfiguration("Release")
                .SetNoRestore(true)
                .SetNoBuild(true)
                .SetIncludeSymbols(true)
                .SetSymbolPackageFormat("snupkg")
                .SetVersion(version)
                .SetPackageReleaseNotes(NormalizeReleaseNotes(releaseNotes))
                .SetOutputDirectory(OutputDirectory));
        });

    public PublishEntry ApplicationBuilderHelpersPublish => _ => _
        .AppId("application_builder_helpers")
        .RunnerOS(RunnerOS.Ubuntu2204)
        .ReleaseCommonAsset(OutputDirectory)
        .Execute(context =>
        {
            if (context.RunType == RunType.Bump)
            {
                DotNetTasks.DotNetNuGetPush(_ => _
                    .SetSource("https://nuget.pkg.github.com/kiryuumaru/index.json")
                    .SetApiKey(GithubToken)
                    .SetTargetPath(OutputDirectory / "**"));
                DotNetTasks.DotNetNuGetPush(_ => _
                    .SetSource("https://api.nuget.org/v3/index.json")
                    .SetApiKey(NuGetAuthToken)
                    .SetTargetPath(OutputDirectory / "**"));
            }
        });

    private string? NormalizeReleaseNotes(string? releaseNotes)
    {
        return releaseNotes?
            .Replace(",", "%2C")?
            .Replace(":", "%3A")?
            .Replace(";", "%3B");
    }
}
