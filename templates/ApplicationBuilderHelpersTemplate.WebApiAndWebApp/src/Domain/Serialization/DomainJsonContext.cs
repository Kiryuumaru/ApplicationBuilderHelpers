using System.Text.Json;
using System.Text.Json.Serialization;
using Domain.AppEnvironment.Models;
using Domain.Authorization.Entities;
using Domain.Authorization.Enums;
using Domain.Authorization.Models;
using Domain.Authorization.ValueObjects;
using Domain.Identity.Entities;
using Domain.Identity.Enums;
using Domain.Identity.Models;
using Domain.Identity.ValueObjects;
using Domain.Serialization.Converters;

namespace Domain.Serialization;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DictionaryKeyPolicy = JsonKnownNamingPolicy.CamelCase,
    GenerationMode = JsonSourceGenerationMode.Metadata,
    Converters = new[]
    {
        typeof(PermissionAccessCategoryJsonConverter)
    })]
// AppEnvironment
[JsonSerializable(typeof(global::Domain.AppEnvironment.Models.AppEnvironment))]
// Authorization Enums
[JsonSerializable(typeof(PermissionAccessCategory))]
[JsonSerializable(typeof(ScopeDirectiveType))]
// Authorization Models
[JsonSerializable(typeof(Permission))]
[JsonSerializable(typeof(Permission.ParsedIdentifier))]
[JsonSerializable(typeof(Role))]
// Authorization ValueObjects
[JsonSerializable(typeof(RoleId))]
[JsonSerializable(typeof(RolePermissionTemplate))]
[JsonSerializable(typeof(ScopeDirective))]
[JsonSerializable(typeof(ScopeTemplate))]
// Identity Enums
[JsonSerializable(typeof(ExternalLoginProvider))]
[JsonSerializable(typeof(PasskeyChallengeType))]
[JsonSerializable(typeof(UserStatus))]
// Identity Models
[JsonSerializable(typeof(LoginSession))]
[JsonSerializable(typeof(PasskeyChallenge))]
[JsonSerializable(typeof(PasskeyCredential))]
[JsonSerializable(typeof(User))]
[JsonSerializable(typeof(UserRoleResolution))]
[JsonSerializable(typeof(UserSession))]
// Identity ValueObjects
[JsonSerializable(typeof(UserId))]
[JsonSerializable(typeof(UserIdentityLink))]
[JsonSerializable(typeof(UserPermissionGrant))]
[JsonSerializable(typeof(UserRoleAssignment))]
// Common Types for Serialization
[JsonSerializable(typeof(Dictionary<string, string?>))]
[JsonSerializable(typeof(IReadOnlyDictionary<string, string?>))]
public partial class DomainJsonContext : JsonSerializerContext
{
    public static JsonSerializerOptions CreateOptions(JsonSerializerOptions? baseOptions = null)
    {
        var options = baseOptions is null
            ? new JsonSerializerOptions(JsonSerializerDefaults.Web)
            : new JsonSerializerOptions(baseOptions);

        options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
        AddConverterIfMissing<CamelCaseStringEnumConverter>(options);
        AddConverterIfMissing<PermissionAccessCategoryJsonConverter>(options);

        if (!options.TypeInfoResolverChain.Contains(Default))
        {
            options.TypeInfoResolverChain.Insert(0, Default);
        }

        return options;
    }

    private static void AddConverterIfMissing<TConverter>(JsonSerializerOptions options)
        where TConverter : JsonConverter, new()
    {
        foreach (var converter in options.Converters)
        {
            if (converter is TConverter)
            {
                return;
            }
        }

        options.Converters.Add(new TConverter());
    }
}
