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
    private const string EmbeddedConfigKey = "RUNTIME_EMBEDDED_CONFIG";

    extension(IConfiguration configuration)
    {
        public JsonObject EmbeddedConfig
        {
            get
            {
                var jsonString = configuration.GetRefValue(EmbeddedConfigKey);
                return JsonNode.Parse(jsonString)?.AsObject()
                    ?? throw new InvalidOperationException("EmbeddedConfig value could not be parsed as JSON.");
            }
            set => configuration[EmbeddedConfigKey] = value.ToJsonString();
        }
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

    public static JsonObject GetEmbeddedConfig(this IConfiguration configuration, params string[] path)
    {
        var config = configuration.EmbeddedConfig;
        return config.GetValueOrThrow<JsonObject>(path);
    }

    public static void SetEmbeddedConfig(this IConfiguration configuration, JsonObject config, bool mergeExisting = false)
    {
        if (mergeExisting && configuration.TryGetRefValue(EmbeddedConfigKey, out _))
        {
            try
            {
                var existingConfig = configuration.EmbeddedConfig;
                config = existingConfig.Merge(config).DeepClone().AsObject();
            }
            catch (InvalidOperationException)
            {
                // Existing config could not be parsed; overwrite with new config
            }
        }
        configuration.EmbeddedConfig = config;
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
}
