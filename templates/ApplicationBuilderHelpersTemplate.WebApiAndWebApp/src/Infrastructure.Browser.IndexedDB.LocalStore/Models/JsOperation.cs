namespace Infrastructure.Browser.IndexedDB.LocalStore.Models;

internal sealed record JsOperation(string type, string group, string id, string? data);
