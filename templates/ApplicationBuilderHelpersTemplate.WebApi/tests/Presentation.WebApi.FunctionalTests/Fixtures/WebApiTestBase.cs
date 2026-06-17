using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Presentation.WebApi.FunctionalTests.Fixtures;

namespace Presentation.WebApi.FunctionalTests;

/// <summary>
/// Base class for WebApi functional tests with parallel execution support.
/// Each test class gets its own isolated WebApi host with a random port and unique database.
/// This enables running tests in parallel without port or database collisions.
/// </summary>
public abstract class WebApiTestBase : IAsyncLifetime
{
    private WebApiTestHost? _host;

    /// <summary>
    /// Test output helper for logging.
    /// </summary>
    protected readonly ITestOutputHelper Output;

    /// <summary>
    /// HTTP client configured to communicate with the test host.
    /// </summary>
    protected HttpClient HttpClient => _host?.HttpClient ?? throw new InvalidOperationException("Host not initialized");

    /// <summary>
    /// Base URL of the test API server.
    /// </summary>
    protected string BaseUrl => _host?.BaseUrl ?? throw new InvalidOperationException("Host not initialized");

    /// <summary>
    /// JSON serialization options with case-insensitive property names.
    /// </summary>
    protected static JsonSerializerOptions JsonOptions { get; } = new() { PropertyNameCaseInsensitive = true };

    /// <summary>
    /// Standard test password used across tests.
    /// </summary>
    protected const string TestPassword = "TestPassword123!";

    protected WebApiTestBase(ITestOutputHelper output)
    {
        Output = output;
    }

    public virtual async Task InitializeAsync()
    {
        Output.WriteLine("[TEST] Initializing WebApi test host with random port...");
        _host = new WebApiTestHost(Output);
        await _host.StartAsync(TimeSpan.FromSeconds(60));
        Output.WriteLine($"[TEST] WebApi started at {_host.BaseUrl}");
    }

    public virtual async Task DisposeAsync()
    {
        Output.WriteLine("[TEST] Disposing WebApi test host...");
        if (_host != null)
        {
            await _host.DisposeAsync();
        }
        Output.WriteLine("[TEST] WebApi test host disposed");
    }

    #region Helper Methods

    /// <summary>
    /// Register a new user and return the authentication response.
    /// </summary>
    protected async Task<AuthResponse?> RegisterUserAsync(string? username = null)
    {
        username ??= $"testuser_{Guid.NewGuid():N}";
        var email = $"{username}@example.com";

        var registerRequest = new
        {
            Username = username,
            Email = email,
            Password = TestPassword,
            ConfirmPassword = TestPassword
        };

        var response = await HttpClient.PostAsJsonAsync("/api/v1/auth/register", registerRequest);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<AuthResponse>(content, JsonOptions);
    }

    /// <summary>
    /// Login with username/password and return the authentication response.
    /// </summary>
    protected async Task<AuthResponse?> LoginAsync(string username, string? password = null)
    {
        password ??= TestPassword;

        var loginRequest = new { Username = username, Password = password };
        var response = await HttpClient.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<AuthResponse>(content, JsonOptions);
    }

    /// <summary>
    /// Register and login a new user in one step.
    /// </summary>
    protected async Task<AuthResponse?> RegisterAndLoginAsync(string? username = null)
    {
        username ??= $"testuser_{Guid.NewGuid():N}";
        var registerResult = await RegisterUserAsync(username);
        if (registerResult == null) return null;
        return await LoginAsync(username);
    }

    /// <summary>
    /// Create an authenticated HTTP client with the given access token.
    /// </summary>
    protected HttpClient CreateAuthenticatedClient(string accessToken)
    {
        var client = new HttpClient { BaseAddress = new Uri(BaseUrl) };
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }

    #endregion

    #region DTOs

    /// <summary>
    /// Authentication response from login/register endpoints.
    /// </summary>
    protected record AuthResponse(
        string AccessToken,
        string RefreshToken,
        string TokenType,
        int ExpiresIn,
        UserInfoResponse? User);

    /// <summary>
    /// User information included in auth responses.
    /// </summary>
    protected record UserInfoResponse(
        Guid Id,
        string? Username,
        string? Email,
        IReadOnlyCollection<string> Roles,
        IReadOnlyCollection<string> Permissions,
        bool IsAnonymous = false);

    #endregion
}
