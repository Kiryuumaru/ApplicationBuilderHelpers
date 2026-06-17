using Application.EmbeddedConfig.Utilities;
using Application.Shared.Interfaces.Inbound;
using ApplicationBuilderHelpers.Extensions;
using Domain.Shared.Extensions;
using Microsoft.Extensions.Configuration;
using System.Text.Json.Nodes;

namespace Application.EmbeddedConfig.Extensions;

/// <summary>
/// Configuration extensions for embedded config storage and retrieval.
/// Supports loading from JSON strings, file paths, and navigating nested config structures.
/// </summary>
public static class EmbeddedConfigConfigurationExtensions
{
    private const string EmbeddedConfigKey = "VEG_RUNTIME_EMBEDDED_CONFIG";

    public static JsonObject GetEmbeddedConfig(this IConfiguration configuration)
    {
        var jsonString = configuration.GetRefValue(EmbeddedConfigKey);
        return JsonNode.Parse(jsonString)?.AsObject()
            ?? throw new InvalidOperationException("EmbeddedConfig value could not be parsed as JSON.");
    }

    public static TValue GetEmbeddedConfig<TValue>(this IConfiguration configuration, params string[] path)
    {
        var config = GetEmbeddedConfig(configuration);
        return config.GetValueOrThrow<TValue>(path);
    }

    public static JsonObject GetEmbeddedConfig(this IConfiguration configuration, string env)
    {
        var config = GetEmbeddedConfig(configuration);
        return config.GetValueOrThrow<JsonObject>(env);
    }

    public static void SetEmbeddedConfig(this IConfiguration configuration, JsonObject config, bool mergeExisting = false)
    {
        if (mergeExisting)
        {
            JsonObject? existingConfig = null;
            try
            {
                existingConfig = configuration.GetEmbeddedConfig();
            }
            catch { /* No existing config to merge; proceed with new config only */ }
            if (existingConfig != null)
            {
                config = existingConfig.Merge(config).DeepClone().AsObject();
            }
        }
        configuration[EmbeddedConfigKey] = config.ToJsonString();
    }

    public static void SetEmbeddedConfig(this IConfiguration configuration, string configPathOrJsonString, bool mergeExisting = false)
    {
        JsonObject? parsedJson = null;

        if (File.Exists(configPathOrJsonString))
        {
            var fileContent = File.ReadAllText(configPathOrJsonString);
            parsedJson = JsonNode.Parse(fileContent)?.AsObject();
        }
        else
        {
            parsedJson = JsonNode.Parse(configPathOrJsonString)?.AsObject();
        }

        if (parsedJson is null)
        {
            throw new ArgumentException("Invalid config path or JSON string.", nameof(configPathOrJsonString));
        }

        SetEmbeddedConfig(configuration, parsedJson, mergeExisting);
    }

    /// <summary>
    /// Decrypts the encrypted build payload from application constants and loads it into configuration.
    /// </summary>
    public static void LoadEncryptedEmbeddedConfig(this IConfiguration configuration, IApplicationConstants constants)
    {
        if (string.IsNullOrEmpty(constants.BuildPayload))
        {
            return;
        }

        var json = EmbeddedConfigDecryptor.Decrypt(
            constants.BuildPayload,
            constants.AppName,
            constants.Version,
            constants.AppTag);

        SetEmbeddedConfig(configuration, json);
    }
}
