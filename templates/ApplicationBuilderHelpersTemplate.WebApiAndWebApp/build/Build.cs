using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using NukeBuildHelpers;
using NukeBuildHelpers.Common.Attributes;
using NukeBuildHelpers.Entry;
using NukeBuildHelpers.Entry.Extensions;
using NukeBuildHelpers.Runner.Abstraction;
using Serilog;
using System.Linq;

partial class Build : BaseNukeBuildHelpers
{
    [SecretVariable("GITHUB_TOKEN")]
    readonly string? GithubToken;

    [SecretVariable("APPLICATION_CREDENTIALS")]
    string? applicationCredentials;

    const string AppId = "sample_app";

    TestEntry TestEntry => _ => _
        .AppId(AppId)
        .Matrix([RunnerOS.Windows2022, RunnerOS.Ubuntu2204], (osTest, osId) => osTest
            .Matrix(["Domain.UnitTests", "Application.UnitTests", "Application.IntegrationTests", "Presentation.WebApp.FunctionalTests"], (test, testId) => test
                .DisplayName($"Test {testId} on {osId.Name}")
                .WorkflowId($"test_{osId.Name}_{testId}".Replace(".", "_").Replace("-", "_").ToLowerInvariant())
                .RunnerOS(osId)
                .Execute(async context =>
                {
                    using var appRuntime = await StartApplicationRuntime(context);
                    string projFile = RootDirectory / "tests" / testId / $"{testId}.csproj";
                    DotNetTasks.DotNetClean(_ => _
                        .SetProject(projFile));
                    DotNetTasks.DotNetBuild(_ => _
                        .SetProjectFile(projFile));
                    if (testId == "Presentation.FunctionalTests")
                    {
                        var playwrightScript = (RootDirectory / "tests" / testId)
                            .GetDirectories().First() // bin
                            .GetDirectories().First() // Debug/Release
                            .GetDirectories().First() // netX
                            / "playwright.ps1";
                        ProcessTasks.StartProcess("pwsh", $"{playwrightScript} install --with-deps").AssertZeroExitCode();
                    }
                    DotNetTasks.DotNetTest(_ => _
                        .SetNoBuild(true)
                        .SetProcessAdditionalArguments(
                            "--logger \"GitHubActions;summary.includePassedTests=true;summary.includeSkippedTests=true\" " +
                            "-- " +
                            "RunConfiguration.CollectSourceInformation=true " +
                            "DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=opencovere ")
                        .SetProjectFile(projFile));
                })));

    BuildEntry BuildEntry => _ => _
        .AppId(AppId)
        .RunnerOS(RunnerOS.Windows2022)
        .Execute(async context =>
        {
            using var appRuntime = await StartApplicationRuntime(context);
            // build logic here
        });

    PublishEntry PublishEntry => _ => _
        .AppId(AppId)
        .RunnerOS(RunnerOS.Ubuntu2204)
        .Execute(async context =>
        {
            using var appRuntime = await StartApplicationRuntime(context);
            // publish logic here
        });
}
