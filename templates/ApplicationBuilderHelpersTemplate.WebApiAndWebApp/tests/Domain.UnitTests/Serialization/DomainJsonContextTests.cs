using System.Text.Json;
using Domain.Authorization.Entities;
using Domain.Authorization.Enums;
using Domain.Authorization.Models;
using Domain.Serialization;

namespace Domain.UnitTests.Serialization;

public class DomainJsonContextTests
{
    [Fact]
    public void Enum_Serializes_As_CamelCase_String()
    {
        // Arrange
        var json = JsonSerializer.Serialize(PermissionAccessCategory.Write, DomainJsonContext.CreateOptions());

        // Assert
        Assert.Equal("\"write\"", json);
    }

    [Fact]
    public void PermissionParsedIdentifier_RoundTrips()
    {
        var parsed = Permission.ParseIdentifier("api:iam:users:update");
        var typeInfo = (System.Text.Json.Serialization.Metadata.JsonTypeInfo<Permission.ParsedIdentifier>)
            DomainJsonContext.Default.GetTypeInfo(typeof(Permission.ParsedIdentifier))!;

        var json = JsonSerializer.Serialize(parsed, typeInfo);
        var roundTripped = JsonSerializer.Deserialize(json, typeInfo);

        Assert.Equal(parsed.Canonical, roundTripped.Canonical);
        Assert.Equal(parsed.Identifier, roundTripped.Identifier);
    }
}
