using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using NukeBuildHelpers;
using NukeBuildHelpers.Common.Attributes;
using NukeBuildHelpers.Common.Enums;
using NukeBuildHelpers.Entry;
using NukeBuildHelpers.Entry.Extensions;
using NukeBuildHelpers.Runner.Abstraction;
using System.Linq;

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
                .SetProcessAdditionalArguments(
                    "--logger \"GitHubActions;summary.includePassedTests=true;summary.includeSkippedTests=true\" " +
                    "-- " +
                    "RunConfiguration.CollectSourceInformation=true " +
                    "DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=opencovere ")
                .SetProjectFile(RootDirectory / "ApplicationBuilderHelpers.Test.Cli.UnitTest" / "ApplicationBuilderHelpers.Test.Cli.UnitTest.csproj")
                .SetConfiguration("Release"));
        });

    public BuildEntry ApplicationBuilderHelpersBuild => _ => _
        .AppId("application_builder_helpers")
        .RunnerOS(RunnerOS.Ubuntu2204)
        .Execute(context =>
        {
            var app = context.Apps.Values.First();
            string version = app.AppVersion.ToString()!;
            string? releaseNotes = null;
            if (app.BumpVersion != null)
            {
                version = app.BumpVersion.Version.ToString();
                releaseNotes = app.BumpVersion.ReleaseNotes;
            }
            else if(app.PullRequestVersion != null)
            {
                version = app.PullRequestVersion.Version.ToString();
            }
            app.OutputDirectory.DeleteDirectory();
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
                .SetOutputDirectory(app.OutputDirectory));
        });

    public PublishEntry ApplicationBuilderHelpersPublish => _ => _
        .AppId("application_builder_helpers")
        .RunnerOS(RunnerOS.Ubuntu2204)
        .Execute(async context =>
        {
            var app = context.Apps.Values.First();
            if (app.RunType == RunType.Bump)
            {
                DotNetTasks.DotNetNuGetPush(_ => _
                    .SetSource("https://nuget.pkg.github.com/kiryuumaru/index.json")
                    .SetApiKey(GithubToken)
                    .SetTargetPath(app.OutputDirectory / "**"));
                DotNetTasks.DotNetNuGetPush(_ => _
                    .SetSource("https://api.nuget.org/v3/index.json")
                    .SetApiKey(NuGetAuthToken)
                    .SetTargetPath(app.OutputDirectory / "**"));
                await AddReleaseAsset(app.OutputDirectory, app.AppId);
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
