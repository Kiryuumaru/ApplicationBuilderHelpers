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
            GenerateCredsJson();
        });

    // ─────────────────────────────────────────────────────────────
    // creds.json Generation
    // ─────────────────────────────────────────────────────────────

    void GenerateCredsJson()
    {
        var credsPath = RootDirectory / "creds.json";

        if (File.Exists(credsPath))
        {
            Log.Information("creds.json already exists at {path}", credsPath);
            return;
        }

        Log.Information("Generating creds.json at {path}", credsPath);

        var credsObject = new JsonObject();

        foreach (var env in AppEnvironments.AllValues)
        {
            credsObject[env.Tag] = new JsonObject
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

        File.WriteAllText(credsPath, credsObject.ToJsonString(options));

        Log.Information("creds.json generated successfully with branches: {branches}", string.Join(", ", AppEnvironments.AllValues.Select(e => e.Tag)));
    }
}
