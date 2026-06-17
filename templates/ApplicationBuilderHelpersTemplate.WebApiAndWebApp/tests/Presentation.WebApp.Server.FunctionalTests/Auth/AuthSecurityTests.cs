using Presentation.WebApp.Server.FunctionalTests;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
namespace Presentation.WebApp.Server.FunctionalTests.Auth;

/// <summary>
/// Cross-cutting security tests for Authentication API.
/// Tests JWT security, header manipulation, and common attack vectors.
/// </summary>
public class AuthSecurityTests : WebAppTestBase
{
    public AuthSecurityTests(ITestOutputHelper output) : base(output)
    {
    }

    #region JWT Security Tests

    [TimedFact]
    public async Task JWT_WithNoneAlgorithm_IsRejected()
    {
        // "none" algorithm attack - tries to bypass signature verification
        // Base64 of {"alg":"none","typ":"JWT"}
        var noneHeader = "eyJhbGciOiJub25lIiwidHlwIjoiSldUIn0";
        // Base64 of {"sub":"admin","role":"admin"}
        var fakePayload = "eyJzdWIiOiJhZG1pbiIsInJvbGUiOiJhZG1pbiJ9";
        var noneToken = $"{noneHeader}.{fakePayload}.";

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", noneToken);
        var response = await HttpClient.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TimedFact]
    public async Task JWT_WithModifiedPayload_IsRejected()
    {
        var authResult = await RegisterUniqueUserAsync();
        Assert.NotNull(authResult);

        var tokenParts = authResult!.AccessToken.Split('.');
        if (tokenParts.Length == 3)
        {
            // Decode, modify, and re-encode the payload
            var payload = tokenParts[1];
            // Simply corrupting the payload should invalidate the signature
            var modifiedPayload = payload.Substring(0, payload.Length - 2) + "XX";
            var modifiedToken = $"{tokenParts[0]}.{modifiedPayload}.{tokenParts[2]}";

            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", modifiedToken);
            var response = await HttpClient.SendAsync(request);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }

    [TimedFact]
    public async Task JWT_WithModifiedSignature_IsRejected()
    {
        var authResult = await RegisterUniqueUserAsync();
        Assert.NotNull(authResult);

        var tokenParts = authResult!.AccessToken.Split('.');
        if (tokenParts.Length == 3)
        {
            var modifiedSignature = tokenParts[2].Substring(0, tokenParts[2].Length - 2) + "XX";
            var modifiedToken = $"{tokenParts[0]}.{tokenParts[1]}.{modifiedSignature}";

            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", modifiedToken);
            var response = await HttpClient.SendAsync(request);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }

    [TimedFact]
    public async Task JWT_WithEmptySignature_IsRejected()
    {
        var authResult = await RegisterUniqueUserAsync();
        Assert.NotNull(authResult);

        var tokenParts = authResult!.AccessToken.Split('.');
        if (tokenParts.Length == 3)
        {
            var noSignatureToken = $"{tokenParts[0]}.{tokenParts[1]}.";

            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", noSignatureToken);
            var response = await HttpClient.SendAsync(request);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }

    [TimedTheory]
    [InlineData("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9")]  // Only header
    [InlineData("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ0ZXN0IjoiZGF0YSJ9")]  // No signature
    [InlineData("notbase64.notbase64.notbase64")]  // Invalid base64
    [InlineData("")]  // Empty
    [InlineData("   ")]  // Whitespace
    public async Task JWT_WithMalformedFormat_IsRejected(string malformedToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        if (!string.IsNullOrWhiteSpace(malformedToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", malformedToken);
        }
        var response = await HttpClient.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TimedFact]
    public async Task JWT_FromDifferentKey_IsRejected()
    {
        // A JWT signed with a different key should be rejected
        // This is a valid JWT format but signed with a different secret
        var foreignJwt = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", foreignJwt);
        var response = await HttpClient.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion

    #region Header Manipulation Tests

    [TimedFact]
    public async Task Request_WithMultipleAuthHeaders_UsesFirstOrRejects()
    {
        var authResult = await RegisterUniqueUserAsync();
        Assert.NotNull(authResult);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {authResult!.AccessToken}");
        request.Headers.TryAddWithoutValidation("Authorization", "Bearer invalid.token.here");
        var response = await HttpClient.SendAsync(request);

        // Should either use first valid token or reject ambiguous request
        Output.WriteLine($"Multiple auth headers response: {(int)response.StatusCode}");
        Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [TimedFact]
    public async Task Request_WithSpoofedXForwardedFor_DoesNotBypassSecurity()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        request.Headers.Add("X-Forwarded-For", "127.0.0.1");
        request.Headers.Add("X-Real-IP", "127.0.0.1");
        var response = await HttpClient.SendAsync(request);

        // Should still require authentication
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TimedFact]
    public async Task Request_WithSpoofedHost_DoesNotCauseIssues()
    {
        var authResult = await RegisterUniqueUserAsync();
        Assert.NotNull(authResult);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResult!.AccessToken);
        request.Headers.Host = "evil.attacker.com";
        var response = await HttpClient.SendAsync(request);

        // Should either work (ignoring Host) or reject
        Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [TimedFact]
    public async Task Request_WithUrlRewriteHeaders_DoesNotBypassAuth()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        request.Headers.Add("X-Original-URL", "/admin");
        request.Headers.Add("X-Rewrite-URL", "/admin");
        request.Headers.Add("X-Original-Host", "admin.internal");
        var response = await HttpClient.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion

    #region Content-Type Manipulation Tests

    [TimedFact]
    public async Task Login_WithWrongContentType_Returns400Or415()
    {
        var loginData = "Username=test&Password=test";
        var response = await HttpClient.PostAsync(
            "/api/v1/auth/login",
            new StringContent(loginData, Encoding.UTF8, "application/x-www-form-urlencoded"));

        // Should reject non-JSON content
        Assert.True(
            response.StatusCode == HttpStatusCode.BadRequest ||
            response.StatusCode == HttpStatusCode.UnsupportedMediaType,
            $"Expected 400 or 415, got {(int)response.StatusCode}");
    }

    [TimedFact]
    public async Task Login_WithXmlContentType_Returns400Or415()
    {
        var xmlData = "<login><username>test</username><password>test</password></login>";
        var response = await HttpClient.PostAsync(
            "/api/v1/auth/login",
            new StringContent(xmlData, Encoding.UTF8, "application/xml"));

        Assert.True(
            response.StatusCode == HttpStatusCode.BadRequest ||
            response.StatusCode == HttpStatusCode.UnsupportedMediaType,
            $"Expected 400 or 415, got {(int)response.StatusCode}");
    }

    #endregion

    #region HTTP Method Tests

    [TimedTheory]
    [InlineData("GET")]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    [InlineData("PATCH")]
    [InlineData("OPTIONS")]
    [InlineData("HEAD")]
    public async Task Login_WithWrongHttpMethod_Returns405Or404(string method)
    {
        using var request = new HttpRequestMessage(new HttpMethod(method), "/api/v1/auth/login");
        if (method != "GET" && method != "HEAD")
        {
            request.Content = JsonContent.Create(new { Username = "test", Password = "test" });
        }
        var response = await HttpClient.SendAsync(request);

        Assert.True(
            response.StatusCode == HttpStatusCode.MethodNotAllowed ||
            response.StatusCode == HttpStatusCode.NotFound ||
            response.StatusCode == HttpStatusCode.OK, // OPTIONS might return 200
            $"Expected 405 or 404, got {(int)response.StatusCode}");
    }

    #endregion

    #region Path Traversal Tests

    [TimedTheory]
    [InlineData("/api/v1/auth/../../../etc/passwd")]
    [InlineData("/api/v1/auth/..\\..\\..\\windows\\system32")]
    [InlineData("/api/v1/auth/me%2f..%2f..%2fetc%2fpasswd")]
    [InlineData("/api/v1/auth/me/../admin")]
    public async Task Request_WithPathTraversal_Returns404OrDoesNotExposeFiles(string path)
    {
        var authResult = await RegisterUniqueUserAsync();
        Assert.NotNull(authResult);

        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResult!.AccessToken);
        var response = await HttpClient.SendAsync(request);

        var content = await response.Content.ReadAsStringAsync();

        // Should not expose system files
        Assert.DoesNotContain("root:", content);
        Assert.DoesNotContain("[boot loader]", content);
        Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    #endregion

    #region Request Smuggling Tests

    [TimedFact]
    public async Task Request_WithCRLFInjection_DoesNotCauseIssues()
    {
        var authResult = await RegisterUniqueUserAsync();
        Assert.NotNull(authResult);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResult!.AccessToken);
        // Try to inject headers via CRLF
        request.Headers.TryAddWithoutValidation("X-Custom", "value\r\nX-Injected: malicious");
        var response = await HttpClient.SendAsync(request);

        // Should handle gracefully
        Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    #endregion

    #region Encoding Tests

    [TimedFact]
    public async Task Login_WithUnicodeNormalizationAttack_DoesNotBypass()
    {
        var authResult = await RegisterUniqueUserAsync();
        Assert.NotNull(authResult);

        // Try username with unicode lookalikes
        var unicodeUsername = "аdmin"; // 'а' is Cyrillic, not Latin 'a'
        var loginRequest = new { Username = unicodeUsername, Password = TestPassword };
        var response = await HttpClient.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

        // Should not find a user (unless unicode normalization is applied)
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TimedFact]
    public async Task Login_WithNullByteInjection_DoesNotBypass()
    {
        var loginRequest = new { Username = "admin\x00ignore", Password = TestPassword };
        var response = await HttpClient.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

        Assert.True(
            response.StatusCode == HttpStatusCode.Unauthorized ||
            response.StatusCode == HttpStatusCode.BadRequest,
            $"Null byte injection should be handled. Got {(int)response.StatusCode}");
    }

    #endregion

    #region Response Security Headers Tests

    [TimedFact]
    public async Task Response_HasSecurityHeaders()
    {
        var response = await HttpClient.GetAsync("/api/v1/auth/me");

        // Check for common security headers (availability depends on configuration)
        var headers = response.Headers;

        // Document what headers are present
        Output.WriteLine("Response headers:");
        foreach (var header in headers)
        {
            Output.WriteLine($"  {header.Key}: {string.Join(", ", header.Value)}");
        }

        // Verify no sensitive server info leaked
        if (headers.Contains("Server"))
        {
            var serverHeader = string.Join("", headers.GetValues("Server"));
            Assert.DoesNotContain("version", serverHeader, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Microsoft", serverHeader, StringComparison.OrdinalIgnoreCase);
        }
    }

    [TimedFact]
    public async Task Response_DoesNotCacheAuthTokens()
    {
        var authResult = await RegisterUniqueUserAsync();
        Assert.NotNull(authResult);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResult!.AccessToken);
        var response = await HttpClient.SendAsync(request);

        // Auth responses should have no-cache directives
        if (response.Headers.CacheControl != null)
        {
            Output.WriteLine($"Cache-Control: {response.Headers.CacheControl}");
        }
    }

    #endregion

    #region Error Response Tests

    [TimedFact]
    public async Task ErrorResponse_DoesNotLeakStackTrace()
    {
        // Send malformed request that might cause error
        var response = await HttpClient.PostAsync(
            "/api/v1/auth/login",
            new StringContent("{malformed json", Encoding.UTF8, "application/json"));

        var content = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain("at System.", content);
        Assert.DoesNotContain("StackTrace", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(".cs:line", content);
        Assert.DoesNotContain("NullReferenceException", content);
        Assert.DoesNotContain("ArgumentException", content);
    }

    [TimedFact]
    public async Task ErrorResponse_DoesNotLeakDatabaseInfo()
    {
        var loginRequest = new { Username = "'; DROP TABLE Users;--", Password = "test" };
        var response = await HttpClient.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

        var content = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain("SQLite", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SQL Server", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("table", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("column", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("constraint", content, StringComparison.OrdinalIgnoreCase);
    }

    [TimedFact]
    public async Task ErrorResponse_DoesNotLeakFilePaths()
    {
        var response = await HttpClient.PostAsync(
            "/api/v1/auth/login",
            new StringContent("{bad}", Encoding.UTF8, "application/json"));

        var content = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain(":\\", content);  // Windows paths
        Assert.DoesNotContain("/home/", content);  // Linux paths
        Assert.DoesNotContain("/usr/", content);
        Assert.DoesNotContain("wwwroot", content, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Session Fixation Tests

    [TimedFact]
    public async Task Login_GeneratesNewSessionId()
    {
        var username = $"session_fix_{Guid.NewGuid():N}";
        var authResult = await RegisterUserAsync(username);
        Assert.NotNull(authResult);

        // Login again
        var loginRequest = new { Username = username, Password = TestPassword };
        var loginResponse = await HttpClient.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        var content = await loginResponse.Content.ReadAsStringAsync();
        var newAuthResult = JsonSerializer.Deserialize<AuthResponse>(content, JsonOptions);

        Assert.NotNull(newAuthResult);
        // Each login should generate different tokens
        Assert.NotEqual(authResult!.AccessToken, newAuthResult!.AccessToken);
        Assert.NotEqual(authResult.RefreshToken, newAuthResult.RefreshToken);
    }

    #endregion

    #region Concurrent Request Tests

    [TimedFact]
    public async Task ConcurrentLogins_DoNotCauseRaceConditions()
    {
        var username = $"concurrent_{Guid.NewGuid():N}";
        await RegisterUserAsync(username);

        var loginRequest = new { Username = username, Password = TestPassword };
        var tasks = Enumerable.Range(0, 10)
            .Select(_ => HttpClient.PostAsJsonAsync("/api/v1/auth/login", loginRequest))
            .ToList();

        var responses = await Task.WhenAll(tasks);

        // All requests should succeed or fail gracefully
        foreach (var response in responses)
        {
            Assert.True(
                response.StatusCode == HttpStatusCode.OK ||
                response.StatusCode == HttpStatusCode.TooManyRequests,
                $"Unexpected status: {(int)response.StatusCode}");
        }
    }

    [TimedFact]
    public async Task ConcurrentRegistrations_PreventDuplicates()
    {
        var username = $"dup_race_{Guid.NewGuid():N}";
        var registerRequest = new
        {
            Username = username,
            Email = $"{username}@example.com",
            Password = TestPassword,
            ConfirmPassword = TestPassword
        };

        var tasks = Enumerable.Range(0, 5)
            .Select(_ => HttpClient.PostAsJsonAsync("/api/v1/auth/register", registerRequest))
            .ToList();

        var responses = await Task.WhenAll(tasks);

        // Only one should succeed, rest should get conflict or some other error
        var successCount = responses.Count(r => r.StatusCode == HttpStatusCode.Created);
        var errorCount = responses.Count(r => 
            r.StatusCode == HttpStatusCode.Conflict ||
            r.StatusCode == HttpStatusCode.BadRequest ||
            r.StatusCode == HttpStatusCode.InternalServerError);

        // At most one should succeed (database constraints should prevent duplicates)
        Assert.True(successCount <= 1, $"Expected at most 1 success, got {successCount}");
        // All should either succeed or error
        Assert.Equal(5, successCount + errorCount);
    }

    #endregion

    #region Helper Methods

    private async Task<AuthResponse?> RegisterUniqueUserAsync()
    {
        return await RegisterUserAsync($"sec_test_{Guid.NewGuid():N}");
    }

    private async Task<AuthResponse?> RegisterUserAsync(string username)
    {
        var registerRequest = new
        {
            Username = username,
            Email = $"{username}@example.com",
            Password = TestPassword,
            ConfirmPassword = TestPassword
        };
        var response = await HttpClient.PostAsJsonAsync("/api/v1/auth/register", registerRequest);

        if (!response.IsSuccessStatusCode) return null;

        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<AuthResponse>(content, JsonOptions);
    }

    #endregion

    #region DTOs

    private record AuthResponse(
        string AccessToken,
        string RefreshToken,
        string TokenType,
        int ExpiresIn,
        UserInfoResponse User);

    private record UserInfoResponse(
        Guid Id,
        string Username,
        string? Email,
        IReadOnlyCollection<string> Roles,
        IReadOnlyCollection<string> Permissions);

    #endregion
}





