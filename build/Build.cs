using System;
using System.Linq;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.Execution;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Utilities.Collections;
using NukeBuildHelpers;

class Build : BaseNukeBuildHelpers
{
    public static int Main () => Execute<Build>(x => x.Version);

    [Solution(GenerateProjects = true)]
    internal readonly Solution Solution;

    public override string[] EnvironmentBranches { get; } = ["prerelease", "master"];

    public override string MainEnvironmentBranch { get; } = "master";
}
