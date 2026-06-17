using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using NukeBuildHelpers;
using NukeBuildHelpers.Common.Attributes;
using NukeBuildHelpers.Entry;
using NukeBuildHelpers.Entry.Extensions;
using NukeBuildHelpers.Runner.Abstraction;
using System.Threading.Tasks;

partial class Build : BaseNukeBuildHelpers
{
    const string AppId = "sample_app";

    [SecretVariable("GITHUB_TOKEN")]
    readonly string? GithubToken;

    [SecretVariable("APPLICATION_CREDENTIALS")]
    readonly string? ApplicationCredentials;

    static readonly RunnerOS[] TestRunnerOSes = [RunnerOS.Windows2022, RunnerOS.Ubuntu2204];
    static readonly string[] TestProjects = ["Domain.UnitTests", "Application.UnitTests"];

    TestEntry TestEntry => _ => _
        .AppId(AppId)
        .Matrix(TestRunnerOSes, (osTest, osId) => osTest
            .Matrix(TestProjects, (test, testId) => test
                .DisplayName($"Test {testId} on {osId.Name}")
                .WorkflowId($"test_{osId.Name}_{testId}".Replace(".", "_").Replace("-", "_").ToLowerInvariant())
                .RunnerOS(osId)
                .Execute(context =>
                {
                    string projFile = RootDirectory / "tests" / testId / $"{testId}.csproj";
                    DotNetTasks.DotNetClean(_ => _
                        .SetProject(projFile));
                    DotNetTasks.DotNetBuild(_ => _
                        .SetProjectFile(projFile));
                    DotNetTasks.DotNetTest(_ => _
                        .SetNoBuild(true)
                        .SetProcessAdditionalArguments(
                            "--logger \"GitHubActions;summary.includePassedTests=true;summary.includeSkippedTests=true\" " +
                            "-- " +
                            "RunConfiguration.CollectSourceInformation=true " +
                            "DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=opencovere ")
                        .SetProjectFile(projFile));
                    return Task.CompletedTask;
                })));

    BuildEntry BuildEntry => _ => _
        .AppId(AppId)
        .RunnerOS(RunnerOS.Windows2022)
        .Execute(async context =>
        {
            using var appRuntime = await StartApplicationRuntime(context);
            DotNetTasks.DotNetBuild(_ => _
                .SetProjectFile(RootDirectory / "src" / "Presentation.WebApi" / "Presentation.WebApi.csproj"));
        });

    PublishEntry PublishEntry => _ => _
        .AppId(AppId)
        .RunnerOS(RunnerOS.Ubuntu2204)
        .Execute(async context =>
        {
            using var appRuntime = await StartApplicationRuntime(context);
            DotNetTasks.DotNetPublish(_ => _
                .SetProject(RootDirectory / "src" / "Presentation.WebApi" / "Presentation.WebApi.csproj")
                .SetOutput(RootDirectory / "publish"));
        });
}
