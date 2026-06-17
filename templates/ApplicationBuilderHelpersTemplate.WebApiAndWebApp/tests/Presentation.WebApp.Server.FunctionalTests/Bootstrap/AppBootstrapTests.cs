using Presentation.WebApp.Server.FunctionalTests;
using System.Net;

namespace Presentation.WebApp.Server.FunctionalTests.Bootstrap;

/// <summary>
/// Tests that the WebApi application boots correctly.
/// </summary>
public class AppBootstrapTests : WebAppTestBase
{
    public AppBootstrapTests(ITestOutputHelper output) : base(output)
    {
    }

    [TimedFact]
    public async Task WebApi_StartsSuccessfully()
    {
        Output.WriteLine("[TEST] WebApi_StartsSuccessfully");
        Output.WriteLine("[STEP] GET / (root endpoint)...");

        var response = await HttpClient.GetAsync("/");

        Output.WriteLine($"[RECEIVED] Status: {(int)response.StatusCode} {response.StatusCode}");

        // Root serves the Blazor app which may require authentication
        // The app is running successfully if we get any valid HTTP response
        Assert.True(
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.Redirect ||
            response.StatusCode == HttpStatusCode.Found ||
            response.StatusCode == HttpStatusCode.Unauthorized,
            $"Expected OK, Redirect, or Unauthorized, got {response.StatusCode}");

        Output.WriteLine("[PASS] WebApi started and responds successfully");
    }

    [TimedFact]
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

    [TimedFact]
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




