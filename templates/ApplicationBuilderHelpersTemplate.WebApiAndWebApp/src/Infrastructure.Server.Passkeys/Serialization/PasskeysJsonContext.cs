using System.Text.Json.Serialization;
using Fido2NetLib;

namespace Infrastructure.Server.Passkeys.Serialization;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(AuthenticatorAttestationRawResponse))]
[JsonSerializable(typeof(AuthenticatorAssertionRawResponse))]
[JsonSerializable(typeof(HashSet<string>))]
internal partial class PasskeysJsonContext : JsonSerializerContext
{
}
