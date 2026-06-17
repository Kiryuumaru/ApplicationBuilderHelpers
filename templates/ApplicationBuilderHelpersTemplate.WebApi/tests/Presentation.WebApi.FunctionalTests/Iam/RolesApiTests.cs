using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Presentation.WebApi.FunctionalTests.Fixtures;

namespace Presentation.WebApi.FunctionalTests.Iam;

/// <summary>
/// Functional tests for IAM Roles API endpoints.
/// Tests role assignment and removal operations.
/// </summary>
public sealed class RolesApiTests : WebApiTestBase
{
    public RolesApiTests(ITestOutputHelper output) : base(output)
    {
    }

    #region Role Management Tests

    [Fact]
    public async Task AssignRole_AsRegularUser_Returns403()
    {
        Output.WriteLine("[TEST] AssignRole_AsRegularUser_Returns403");

        var userAuth = await RegisterUserAsync();
        Assert.NotNull(userAuth);
        var userId = userAuth!.User!.Id;

        var roleRequest = new { UserId = userId, RoleCode = "ADMIN" };

        Output.WriteLine("[STEP] POST /api/v1/iam/roles/assign...");
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/iam/roles/assign");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userAuth.AccessToken);
        request.Content = JsonContent.Create(roleRequest);
        var response = await HttpClient.SendAsync(request);

        Output.WriteLine($"[RECEIVED] Status: {(int)response.StatusCode} {response.StatusCode}");

        // Regular users don't have iam:roles:assign permission
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Output.WriteLine("[PASS] Cannot assign roles without admin access");
    }

    [Fact]
    public async Task RemoveRole_AsRegularUser_Returns403()
    {
        Output.WriteLine("[TEST] RemoveRole_AsRegularUser_Returns403");

        var userAuth = await RegisterUserAsync();
        Assert.NotNull(userAuth);
        var userId = userAuth!.User!.Id;
        var roleId = Guid.Parse("00000000-0000-0000-0000-000000000002"); // User role

        var roleRequest = new { UserId = userId, RoleId = roleId };

        Output.WriteLine("[STEP] POST /api/v1/iam/roles/remove...");
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/iam/roles/remove");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userAuth.AccessToken);
        request.Content = JsonContent.Create(roleRequest);
        var response = await HttpClient.SendAsync(request);

        Output.WriteLine($"[RECEIVED] Status: {(int)response.StatusCode} {response.StatusCode}");

        // Regular users don't have iam:roles:remove permission
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Output.WriteLine("[PASS] Cannot remove roles without admin access");
    }

    #endregion
}
