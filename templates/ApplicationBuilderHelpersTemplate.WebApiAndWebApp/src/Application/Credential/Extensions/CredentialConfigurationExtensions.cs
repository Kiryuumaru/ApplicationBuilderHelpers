using ApplicationBuilderHelpers.Extensions;
using Domain.Shared.Extensions;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Application.Credential.Extensions;

public static class CredentialConfigurationExtensions
{
    private const string CredentialsKey = "VEG_RUNTIME_CREDENTIALS";

    public static JsonObject GetCredentials(this IConfiguration configuration)
    {
        var jsonStringCred = configuration.GetRefValue(CredentialsKey);
        return JsonNode.Parse(jsonStringCred)?.AsObject()
            ?? throw new InvalidOperationException("Credentials was not set.");
    }

    public static TValue GetCredentials<TValue>(this IConfiguration configuration, params string[] path)
    {
        var creds = GetCredentials(configuration);
        return creds.GetValueOrThrow<TValue>(path);
    }

    public static JsonObject GetCredentials(this IConfiguration configuration, string env)
    {
        var creds = GetCredentials(configuration);
        return creds.GetValueOrThrow<JsonObject>(env);
    }

    public static void SetCredentials(this IConfiguration configuration, JsonObject credentials, bool mergeExisting = false)
    {
        if (mergeExisting)
        {
            JsonObject? existingCreds = null;
            try
            {
                existingCreds = configuration.GetCredentials();
            }
            catch
            {
                // Intentionally empty: no existing credentials is valid, will use new credentials only
            }
            if (existingCreds != null)
            {
                credentials = existingCreds.Merge(credentials).DeepClone().AsObject();
            }
        }
        configuration[CredentialsKey] = credentials.ToJsonString();
    }

    public static void SetCredentials(this IConfiguration configuration, string credentialPathOrJsonString, bool mergeExisting = false)
    {
        try
        {
            if (File.Exists(credentialPathOrJsonString))
            {
                var fileContent = File.ReadAllText(credentialPathOrJsonString);
                var parsedJson = JsonNode.Parse(fileContent)?.AsObject();
                ArgumentNullException.ThrowIfNull(parsedJson, nameof(parsedJson));
                SetCredentials(configuration, parsedJson, mergeExisting);
                return;
            }
            else
            {
                var parsedJson = JsonNode.Parse(credentialPathOrJsonString)?.AsObject();
                ArgumentNullException.ThrowIfNull(parsedJson, nameof(parsedJson));
                SetCredentials(configuration, parsedJson, mergeExisting);
                return;
            }
        }
        catch
        {
            // Intentionally empty: fall through to throw ArgumentException below
        }
        throw new ArgumentException("Invalid credential path or JSON string.", nameof(credentialPathOrJsonString));
    }
}
