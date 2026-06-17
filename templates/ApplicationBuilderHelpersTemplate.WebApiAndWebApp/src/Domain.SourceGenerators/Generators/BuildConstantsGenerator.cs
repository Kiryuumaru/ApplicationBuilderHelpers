using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Domain.SourceGenerators.Generators;

[Generator(LanguageNames.CSharp)]
public sealed class BuildConstantsGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var globalOptions = context.AnalyzerConfigOptionsProvider
            .Select(static (p, _) => p.GlobalOptions);

        var additionalFiles = context.AdditionalTextsProvider.Collect();

        var inputs = globalOptions.Combine(additionalFiles);

        context.RegisterSourceOutput(inputs, static (spc, input) =>
        {
            var options = input.Left;

            if (!IsBuildConstantsEnabled(options))
            {
                return;
            }

            var files = input.Right;

            var errors = ImmutableArray.CreateBuilder<string>();

            var appName = GetRequiredProperty(options, errors, "AssemblyName");
            var appTitle = GetRequiredProperty(options, errors, "AssemblyTitle");
            var appDescription = GetRequiredProperty(options, errors, "Description");
            var version = GetRequiredProperty(options, errors, "FullVersion");
            var appTag = GetRequiredProperty(options, errors, "AppTag");

            var baseCommandType = GetOptionalProperty(options, "BaseCommandType");

            if (!TryReadBuildPayload(files, out var buildPayload, out var payloadError))
            {
                errors.Add(payloadError);
            }

            if (errors.Count != 0)
            {
                AddErrorSource(spc, errors.ToImmutable());

                // Keep the Build.* surface area present so the build fails with the explicit #error above,
                // not with secondary missing-type errors.
                spc.AddSource("BuildConstants.Generated.g.cs", EmitFallbackBuildConstants(baseCommandType));
                return;
            }

            var source = EmitBuildConstants(
                appName: appName,
                appTitle: appTitle,
                appDescription: appDescription,
                version: version,
                appTag: appTag,
                buildPayload: buildPayload,
                baseCommandType: baseCommandType);

            spc.AddSource("BuildConstants.Generated.g.cs", source);
        });
    }

    private static bool IsBuildConstantsEnabled(AnalyzerConfigOptions options)
    {
        if (!TryGetBuildProperty(options, "GenerateBuildConstants", out var value))
        {
            return false;
        }

        return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryGetBuildProperty(AnalyzerConfigOptions options, string propertyName, out string? value)
    {
        return options.TryGetValue("build_property." + propertyName, out value);
    }

    private static string GetRequiredProperty(AnalyzerConfigOptions options, ImmutableArray<string>.Builder errors, string propertyName)
    {
        if (!TryGetBuildProperty(options, propertyName, out var value))
        {
            errors.Add("Build constants generation requires MSBuild property '" + propertyName + "' to be set.");
            return string.Empty;
        }

        if (value is null || string.IsNullOrWhiteSpace(value))
        {
            errors.Add("Build constants generation requires MSBuild property '" + propertyName + "' to be set.");
            return string.Empty;
        }

        return value;
    }

    private static string GetOptionalProperty(AnalyzerConfigOptions options, string propertyName)
    {
        if (!TryGetBuildProperty(options, propertyName, out var value))
        {
            return string.Empty;
        }

        return value ?? string.Empty;
    }

    private static bool TryReadBuildPayload(ImmutableArray<AdditionalText> files, out string buildPayload, out string error)
    {
        for (var i = 0; i < files.Length; i++)
        {
            var file = files[i];
            var path = file.Path;

            if (path is null)
            {
                continue;
            }

            if (!path.EndsWith("creds.json", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            SourceText? text;
            try
            {
                text = file.GetText();
            }
            catch (Exception ex)
            {
                buildPayload = string.Empty;
                error = "Build constants generation failed to read creds.json AdditionalFile: " + ex.Message;
                return false;
            }

            if (text is null)
            {
                buildPayload = string.Empty;
                error = "Build constants generation requires creds.json to be readable as an MSBuild AdditionalFile.";
                return false;
            }

            buildPayload = text.ToString();
            buildPayload = Convert.ToBase64String(Encoding.UTF8.GetBytes(buildPayload));
            error = string.Empty;
            return true;
        }

        buildPayload = string.Empty;
        error = "Build constants generation requires creds.json to be included as an MSBuild AdditionalFile. Ensure GenerateBuildConstants=true and CredsJsonPath points to creds.json.";
        return false;
    }

    private static SourceText EmitFallbackBuildConstants(string baseCommandType)
    {
        var resolvedBaseCommandType = string.IsNullOrWhiteSpace(baseCommandType)
            ? "global::Presentation.Commands.BaseCommand"
            : "global::" + baseCommandType;

        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine("namespace Build");
        sb.AppendLine("{");
        sb.AppendLine("    internal static class Constants");
        sb.AppendLine("    {");
        sb.AppendLine("        public const string AppName = \"\";");
        sb.AppendLine("        public const string AppTitle = \"\";");
        sb.AppendLine("        public const string AppDescription = \"\";");
        sb.AppendLine("        public const string Version = \"\";");
        sb.AppendLine("        public const string AppTag = \"\";");
        sb.AppendLine("        public const string BuildPayload = \"\";");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    internal class ApplicationConstants : global::Application.Shared.Interfaces.Inbound.IApplicationConstants");
        sb.AppendLine("    {");
        sb.AppendLine("        public static global::Application.Shared.Interfaces.Inbound.IApplicationConstants Instance { get; } = new global::Build.ApplicationConstants();");
        sb.AppendLine("        public string AppName { get; } = global::Build.Constants.AppName;");
        sb.AppendLine("        public string AppTitle { get; } = global::Build.Constants.AppTitle;");
        sb.AppendLine("        public string AppDescription { get; } = global::Build.Constants.AppDescription;");
        sb.AppendLine("        public string Version { get; } = global::Build.Constants.Version;");
        sb.AppendLine("        public string AppTag { get; } = global::Build.Constants.AppTag;");
        sb.AppendLine("        public string BuildPayload { get; } = global::Build.Constants.BuildPayload;");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    internal abstract class BaseCommand<[global::System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(global::System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All)] THostApplicationBuilder> : " + resolvedBaseCommandType + "<THostApplicationBuilder>");
        sb.AppendLine("        where THostApplicationBuilder : global::Microsoft.Extensions.Hosting.IHostApplicationBuilder");
        sb.AppendLine("    {");
        sb.AppendLine("        public override global::Application.Shared.Interfaces.Inbound.IApplicationConstants ApplicationConstants { get; } = global::Build.ApplicationConstants.Instance;");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return SourceText.From(sb.ToString(), Encoding.UTF8);
    }

    private static void AddErrorSource(SourceProductionContext spc, ImmutableArray<string> errors)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated />");
        sb.Append("#error Build constants generation failed: ");

        if (errors.Length == 0)
        {
            sb.AppendLine("Unknown error.");
        }
        else
        {
            for (var i = 0; i < errors.Length; i++)
            {
                if (i != 0)
                {
                    sb.Append(" | ");
                }

                sb.Append(NormalizeError(errors[i]));
            }

            sb.AppendLine();
        }

        spc.AddSource("BuildConstants.Errors.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
    }

    private static string NormalizeError(string error)
    {
        if (string.IsNullOrWhiteSpace(error))
        {
            return "Unknown error.";
        }

        return error.Replace("\r", " ").Replace("\n", " ").Trim();
    }

    private static SourceText EmitBuildConstants(
        string appName,
        string appTitle,
        string appDescription,
        string version,
        string appTag,
        string buildPayload,
        string baseCommandType)
    {
        var sb = new StringBuilder();

        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine("namespace Build");
        sb.AppendLine("{");
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// Contains application build constants.");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    internal static class Constants");
        sb.AppendLine("    {");

        AppendConstant(sb, "AppName", appName, "The application name.");
        sb.AppendLine();
        AppendConstant(sb, "AppTitle", appTitle, "The application title.");
        sb.AppendLine();
        AppendConstant(sb, "AppDescription", appDescription, "The application description.");
        sb.AppendLine();
        AppendConstant(sb, "Version", version, "The application version.");
        sb.AppendLine();
        AppendConstant(sb, "AppTag", appTag, "The application AppTag.");
        sb.AppendLine();
        AppendConstant(sb, "BuildPayload", buildPayload, "The application build payload.");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    /// <inheritdoc/>");
        sb.AppendLine("    internal class ApplicationConstants : global::Application.Shared.Interfaces.Inbound.IApplicationConstants");
        sb.AppendLine("    {");
        sb.AppendLine("        /// <inheritdoc/>");
        sb.AppendLine("        public static global::Application.Shared.Interfaces.Inbound.IApplicationConstants Instance { get; } = new global::Build.ApplicationConstants();");
        sb.AppendLine();
        sb.AppendLine("        /// <inheritdoc/>");
        sb.AppendLine("        public string AppName { get; } = global::Build.Constants.AppName;");
        sb.AppendLine();
        sb.AppendLine("        /// <inheritdoc/>");
        sb.AppendLine("        public string AppTitle { get; } = global::Build.Constants.AppTitle;");
        sb.AppendLine();
        sb.AppendLine("        /// <inheritdoc/>");
        sb.AppendLine("        public string AppDescription { get; } = global::Build.Constants.AppDescription;");
        sb.AppendLine();
        sb.AppendLine("        /// <inheritdoc/>");
        sb.AppendLine("        public string Version { get; } = global::Build.Constants.Version;");
        sb.AppendLine();
        sb.AppendLine("        /// <inheritdoc/>");
        sb.AppendLine("        public string AppTag { get; } = global::Build.Constants.AppTag;");
        sb.AppendLine();
        sb.AppendLine("        /// <inheritdoc/>");
        sb.AppendLine("        public string BuildPayload { get; } = global::Build.Constants.BuildPayload;");
        sb.AppendLine("    }");
        sb.AppendLine();

        var resolvedBaseCommandType = string.IsNullOrWhiteSpace(baseCommandType)
            ? "global::Presentation.Commands.BaseCommand"
            : "global::" + baseCommandType;

        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// Base command class with application constants support.");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    /// <typeparam name=\"THostApplicationBuilder\">The type of host application builder.</typeparam>");
        sb.AppendLine("    internal abstract class BaseCommand<[global::System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(global::System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All)] THostApplicationBuilder> : " + resolvedBaseCommandType + "<THostApplicationBuilder>");
        sb.AppendLine("        where THostApplicationBuilder : global::Microsoft.Extensions.Hosting.IHostApplicationBuilder");
        sb.AppendLine("    {");
        sb.AppendLine("        /// <inheritdoc/>");
        sb.AppendLine("        public override global::Application.Shared.Interfaces.Inbound.IApplicationConstants ApplicationConstants { get; } = global::Build.ApplicationConstants.Instance;");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return SourceText.From(sb.ToString(), Encoding.UTF8);
    }

    private static void AppendConstant(StringBuilder builder, string name, string value, string documentation)
    {
        builder.AppendLine("        /// <summary>");
        builder.Append("        /// ");
        builder.AppendLine(documentation);
        builder.AppendLine("        /// </summary>");
        builder.Append("        public const string ");
        builder.Append(name);
        builder.Append(" = ");
        builder.Append(ToLiteral(value));
        builder.AppendLine(";");
    }

    private static string ToLiteral(string value)
    {
        var builder = new StringBuilder(value.Length + 2);
        builder.Append('"');

        for (var i = 0; i < value.Length; i++)
        {
            var character = value[i];

            builder.Append(character switch
            {
                '"' => "\\\"",
                '\\' => "\\\\",
                '\0' => "\\0",
                '\a' => "\\a",
                '\b' => "\\b",
                '\f' => "\\f",
                '\n' => "\\n",
                '\r' => "\\r",
                '\t' => "\\t",
                '\v' => "\\v",
                _ when char.IsControl(character) => FormatControlCharacter(character),
                _ => character.ToString(),
            });
        }

        builder.Append('"');
        return builder.ToString();
    }

    private static string FormatControlCharacter(char character)
    {
        return string.Format("\\u{0:x4}", (int)character);
    }
}
