using Application.Shared.Extensions;
using Application.Shared.Utilities;
using Domain.AppEnvironment.Constants;
using Nuke.Common;
using Nuke.Common.IO;
using Serilog;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

partial class Build
{
    // ─────────────────────────────────────────────────────────────
    // Base Build Overrides
    // ─────────────────────────────────────────────────────────────

    public override string[] EnvironmentBranches =>
        [.. AppEnvironments.AllValues.Select(e => e.Tag)];

    public override string MainEnvironmentBranch =>
        AppEnvironments.AllValues.Last().Tag;

    // ─────────────────────────────────────────────────────────────
    // Targets
    // ─────────────────────────────────────────────────────────────

    Target Clean => _ => _
        .Executes(() =>
        {
            foreach (var path in RootDirectory.GetFiles("**", 99).Where(i => i.Name.EndsWith(".csproj")))
            {
                if (path.Name == "_build.csproj")
                {
                    continue;
                }
                Log.Information("Cleaning {path}", path);
                (path.Parent / "bin").DeleteDirectory();
                (path.Parent / "obj").DeleteDirectory();
            }
            (RootDirectory / ".vs").DeleteDirectory();
            (RootDirectory / "src" / "Presentation.WebApp" / "app.db").DeleteDirectory();
        });

    Target Init => _ => _
        .Executes(() =>
        {
            GenerateEmbeddedConfig();
        });

    // ─────────────────────────────────────────────────────────────
    // embedded-config.json Generation
    // ─────────────────────────────────────────────────────────────

    void GenerateEmbeddedConfig()
    {
        var configPath = RootDirectory / "embedded-config.json";

        if (File.Exists(configPath))
        {
            Log.Information("embedded-config.json already exists at {path}", configPath);
            return;
        }

        Log.Information("Generating embedded-config.json at {path}", configPath);

        var configObject = new JsonObject();

        foreach (var env in AppEnvironments.AllValues)
        {
            configObject[env.Tag] = new JsonObject
            {
                ["jwt"] = new JsonObject
                {
                    ["secret"] = RandomHelpers.Alphanumeric(64),
                    ["issuer"] = "ApplicationBuilderHelpers",
                    ["audience"] = "ApplicationBuilderHelpers"
                }
            };
        }

        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        File.WriteAllText(configPath, configObject.ToJsonString(options));

        Log.Information("embedded-config.json generated successfully with branches: {branches}", string.Join(", ", AppEnvironments.AllValues.Select(e => e.Tag)));
    }
}
