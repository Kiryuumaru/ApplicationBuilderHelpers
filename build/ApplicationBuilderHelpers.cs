using Nuke.Common.IO;
using Nuke.Common.Tools.DotNet;
using NukeBuildHelpers;
using NukeBuildHelpers.Attributes;
using NukeBuildHelpers.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _build;

class ApplicationBuilderHelpers : AppEntry<Build>
{
    public override RunsOnType BuildRunsOn => RunsOnType.Ubuntu2204;

    public override RunsOnType PublishRunsOn => RunsOnType.Ubuntu2204;

    [SecretHelper("NUGET_AUTH_TOKEN")]
    readonly string NuGetAuthToken;

    [SecretHelper("GITHUB_TOKEN")]
    readonly string GithubToken;

    public override void Build()
    {
        OutputDirectory.DeleteDirectory();
        DotNetTasks.DotNetClean(_ => _
            .SetProject(NukeBuild.Solution.ApplicationBuilderHelpers));
        DotNetTasks.DotNetBuild(_ => _
            .SetProjectFile(NukeBuild.Solution.ApplicationBuilderHelpers)
            .SetConfiguration("Release"));
        DotNetTasks.DotNetPack(_ => _
            .SetProject(NukeBuild.Solution.ApplicationBuilderHelpers)
            .SetConfiguration("Release")
            .SetNoRestore(true)
            .SetNoBuild(true)
            .SetIncludeSymbols(true)
            .SetSymbolPackageFormat("snupkg")
            .SetVersion(NewVersion?.Version?.ToString() ?? "0.0.0")
            .SetPackageReleaseNotes(NewVersion?.ReleaseNotes)
            .SetOutputDirectory(OutputDirectory));
    }

    public override void Publish()
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
}
