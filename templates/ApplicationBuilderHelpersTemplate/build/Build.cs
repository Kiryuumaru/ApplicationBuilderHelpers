using Application.Logger.Extensions;
using Application.Shared.Interfaces.Inbound;
using ApplicationBuilderHelpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using NukeBuildHelpers;
using NukeBuildHelpers.Common.Attributes;
using NukeBuildHelpers.Entry;
using NukeBuildHelpers.Entry.Extensions;
using NukeBuildHelpers.RunContext.Interfaces;
using NukeBuildHelpers.RunContext.Models;
using NukeBuildHelpers.Runner.Abstraction;
using Presentation.Commands;
using Semver;
using Serilog;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

partial class Build : BaseNukeBuildHelpers
{
    const string AppId = "sample_app";

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
                        // MTP mode (see global.json test.runner): GitHubActionsTestLogger
                        // auto-registers via MSBuild and Microsoft.Testing.Extensions.CodeCoverage
                        // provides --coverage. VSTest-only flags (--logger, RunConfiguration,
                        // DataCollector) exit 5 under MTP and must not be restored.
                        .SetProcessAdditionalArguments("--report-github --coverage --coverage-output-format cobertura")
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
                .SetProjectFile(RootDirectory / "src" / "Presentation.Cli" / "Presentation.Cli.csproj"));
        });

    PublishEntry PublishEntry => _ => _
        .AppId(AppId)
        .RunnerOS(RunnerOS.Ubuntu2204)
        .Execute(async context =>
        {
            using var appRuntime = await StartApplicationRuntime(context);
            DotNetTasks.DotNetPublish(_ => _
                .SetProject(RootDirectory / "src" / "Presentation.Cli" / "Presentation.Cli.csproj")
                .SetOutput(RootDirectory / "publish"));
        });
}
