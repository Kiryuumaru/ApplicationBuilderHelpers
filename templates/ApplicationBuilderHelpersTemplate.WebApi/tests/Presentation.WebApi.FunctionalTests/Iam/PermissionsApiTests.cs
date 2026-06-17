using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Presentation.WebApi.FunctionalTests.Fixtures;

namespace Presentation.WebApi.FunctionalTests.Iam;

/// <summary>
/// Functional tests for IAM Permissions API endpoints.
/// Tests permission grant and revoke operations.
/// </summary>
public sealed class PermissionsApiTests : WebApiTestBase
{
    public PermissionsApiTests(ITestOutputHelper output) : base(output)
    {
    }

    #region Permission Grant/Revoke Tests

    [Fact]
    public async Task GrantPermission_AsRegularUser_Returns403()
    {
        Output.WriteLine("[TEST] GrantPermission_AsRegularUser_Returns403");

        var userAuth = await RegisterUserAsync();
        Assert.NotNull(userAuth);
        var userId = userAuth!.User!.Id;

        var grantRequest = new { UserId = userId, PermissionIdentifier = "api:iam:users:read", Description = "Test grant" };

        Output.WriteLine("[STEP] POST /api/v1/iam/permissions/grant...");
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/iam/permissions/grant");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userAuth.AccessToken);
        request.Content = JsonContent.Create(grantRequest);
        var response = await HttpClient.SendAsync(request);

        Output.WriteLine($"[RECEIVED] Status: {(int)response.StatusCode} {response.StatusCode}");

        // Regular users don't have iam:permissions:grant permission
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Output.WriteLine("[PASS] Cannot grant permissions without admin access");
    }

    [Fact]
    public async Task RevokePermission_AsRegularUser_Returns403()
    {
        Output.WriteLine("[TEST] RevokePermission_AsRegularUser_Returns403");

        var userAuth = await RegisterUserAsync();
        Assert.NotNull(userAuth);
        var userId = userAuth!.User!.Id;

        var revokeRequest = new { UserId = userId, PermissionIdentifier = "api:iam:users:read" };

        Output.WriteLine("[STEP] POST /api/v1/iam/permissions/revoke...");
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/iam/permissions/revoke");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userAuth.AccessToken);
        request.Content = JsonContent.Create(revokeRequest);
        var response = await HttpClient.SendAsync(request);

        Output.WriteLine($"[RECEIVED] Status: {(int)response.StatusCode} {response.StatusCode}");

        // Regular users don't have iam:permissions:revoke permission
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Output.WriteLine("[PASS] Cannot revoke permissions without admin access");
    }

    #endregion
}
