using System.Text.Json.Serialization;
using Infrastructure.EFCore.Server.Identity.Models;

namespace Infrastructure.EFCore.Server.Identity.Serialization;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(List<ScopeTemplateDto>))]
[JsonSerializable(typeof(Dictionary<string, string?>))]
internal partial class EFCoreServerIdentityJsonContext : JsonSerializerContext
{
}
