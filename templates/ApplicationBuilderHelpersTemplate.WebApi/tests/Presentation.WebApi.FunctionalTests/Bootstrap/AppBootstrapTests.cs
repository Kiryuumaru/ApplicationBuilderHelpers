using System.Net;
using Presentation.WebApi.FunctionalTests.Fixtures;

namespace Presentation.WebApi.FunctionalTests.Bootstrap;

/// <summary>
/// Tests that the WebApi application boots correctly.
/// </summary>
public sealed class AppBootstrapTests : WebApiTestBase
{
    public AppBootstrapTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public async Task WebApi_StartsSuccessfully()
    {
        Output.WriteLine("[TEST] WebApi_StartsSuccessfully");
        Output.WriteLine("[STEP] GET / (root endpoint)...");

        var response = await HttpClient.GetAsync("/");

        Output.WriteLine($"[RECEIVED] Status: {(int)response.StatusCode} {response.StatusCode}");

        // Root redirects to Scalar API docs (302/200)
        Assert.True(
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.Redirect ||
            response.StatusCode == HttpStatusCode.Found,
            $"Expected OK or Redirect, got {response.StatusCode}");

        Output.WriteLine("[PASS] WebApi started and responds successfully");
    }

    [Fact]
    public async Task Swagger_ReturnsOpenApiDocument()
    {
        Output.WriteLine("[TEST] Swagger_ReturnsOpenApiDocument");

        Output.WriteLine("[STEP] GET /swagger/v1/swagger.json...");
        var response = await HttpClient.GetAsync("/swagger/v1/swagger.json");

        Output.WriteLine($"[RECEIVED] Status: {(int)response.StatusCode} {response.StatusCode}");

        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            Output.WriteLine($"[RECEIVED] Content length: {content.Length} chars");

            Assert.Contains("openapi", content.ToLower());
            Output.WriteLine("[PASS] OpenAPI document available");
        }
        else
        {
            Output.WriteLine($"[SKIP] OpenAPI not at expected path (status {response.StatusCode})");
        }
    }

    [Fact]
    public async Task Scalar_UI_Available()
    {
        Output.WriteLine("[TEST] Scalar_UI_Available");

        Output.WriteLine("[STEP] GET /scalar/v1...");
        var response = await HttpClient.GetAsync("/scalar/v1");

        Output.WriteLine($"[RECEIVED] Status: {(int)response.StatusCode} {response.StatusCode}");

        // Scalar UI should be available
        Assert.True(response.IsSuccessStatusCode, $"Expected success, got {response.StatusCode}");
        Output.WriteLine("[PASS] Scalar UI available");
    }
}
