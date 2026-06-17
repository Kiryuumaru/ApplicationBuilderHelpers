using System.Text.Json.Serialization;
using Infrastructure.Browser.IndexedDB.LocalStore.Models;

namespace Infrastructure.Browser.IndexedDB.LocalStore.Serialization;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(JsOperation))]
[JsonSerializable(typeof(JsOperation[]))]
internal partial class IndexedDBJsonContext : JsonSerializerContext
{
}
