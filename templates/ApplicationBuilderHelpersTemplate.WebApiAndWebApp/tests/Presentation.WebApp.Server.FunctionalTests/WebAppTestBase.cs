using System.Net.Http.Json;
using System.Text.Json;
using Presentation.WebApp.Server.FunctionalTests.Fixtures;

namespace Presentation.WebApp.Server.FunctionalTests;

/// <summary>
/// Base class for WebApi functional tests that provides isolated test infrastructure.
/// Each test class gets its own WebApiTestHost with a random port and isolated database.
/// This enables parallel test execution without port or data collisions.
/// </summary>
public abstract class WebAppTestBase : IAsyncLifetime
{
    private WebApiTestHost? _host;

    protected ITestOutputHelper Output { get; }
    protected WebApiTestHost Host => _host ?? throw new InvalidOperationException("Host not initialized. Call InitializeAsync first.");
    protected HttpClient HttpClient => Host.HttpClient;
    protected string BaseUrl => Host.BaseUrl;

    protected static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    protected const string TestPassword = "TestPassword123!";

    protected WebAppTestBase(ITestOutputHelper output)
    {
        Output = output;
    }

    public virtual async Task InitializeAsync()
    {
        // Create a new host with a random port for this test class
        _host = new WebApiTestHost(Output);
        await _host.StartAsync(TimeSpan.FromSeconds(60));
    }

    public virtual async Task DisposeAsync()
    {
        if (_host != null)
        {
            await _host.DisposeAsync();
        }
    }

    #region Helper Methods

    /// <summary>
    /// Registers a new user and returns the username.
    /// </summary>
    protected async Task<string> RegisterUserAsync(string? username = null, string? email = null, string? password = null)
    {
        username ??= $"user_{Guid.NewGuid():N}";
        email ??= $"{username}@test.example.com";
        password ??= TestPassword;

        var registerRequest = new { Username = username, Email = email, Password = password };
        var response = await HttpClient.PostAsJsonAsync("/api/v1/auth/register", registerRequest);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Failed to register user: {response.StatusCode} - {error}");
        }

        return username;
    }

    /// <summary>
    /// Logs in a user and returns the auth response.
    /// </summary>
    protected async Task<AuthResponse> LoginAsync(string username, string? password = null)
    {
        password ??= TestPassword;

        var loginRequest = new { Username = username, Password = password };
        var response = await HttpClient.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Failed to login: {response.StatusCode} - {error}");
        }

        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<AuthResponse>(content, JsonOptions)
            ?? throw new InvalidOperationException("Failed to deserialize auth response");
    }

    /// <summary>
    /// Registers a user and logs them in, returning the auth response.
    /// </summary>
    protected async Task<AuthResponse> RegisterAndLoginAsync(string? username = null)
    {
        username = await RegisterUserAsync(username);
        return await LoginAsync(username);
    }

    /// <summary>
    /// Creates an HttpClient with the given access token.
    /// </summary>
    protected HttpClient CreateAuthenticatedClient(string accessToken)
    {
        return Host.CreateAuthenticatedClient(accessToken);
    }

    #endregion
}

/// <summary>
/// Auth response DTO for test helpers.
/// </summary>
public record AuthResponse
{
    public string AccessToken { get; init; } = "";
    public string RefreshToken { get; init; } = "";
    public string TokenType { get; init; } = "";
    public int ExpiresIn { get; init; }
    public UserInfo? User { get; init; }
}

/// <summary>
/// User info DTO for test helpers.
/// </summary>
public record UserInfo
{
    public string Id { get; init; } = "";
    public string Username { get; init; } = "";
    public string Email { get; init; } = "";
}
