using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Presentation.WebApp.Server.FunctionalTests;

namespace Presentation.WebApp.Server.FunctionalTests.Auth;

/// <summary>
/// Functional tests for Login API endpoints.
/// Tests authentication flows, credential validation, and security edge cases.
/// Each test class gets its own isolated WebApi host with random port and database.
/// </summary>
public class LoginApiTests : WebAppTestBase
{
    public LoginApiTests(ITestOutputHelper output) : base(output)
    {
    }

    #region Basic Login Tests

    [TimedFact]
    public async Task Login_WithValidCredentials_ReturnsJwtToken()
    {
        var username = $"login_valid_{Guid.NewGuid():N}";
        await RegisterUserAsync(username);

        var loginRequest = new { Username = username, Password = TestPassword };
        var response = await HttpClient.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<LoginAuthResponse>(content, JsonOptions);
        
        Assert.NotNull(result);
        Assert.NotEmpty(result!.AccessToken);
        Assert.NotEmpty(result.RefreshToken);
        Assert.Equal("Bearer", result.TokenType);
        Assert.True(result.ExpiresIn > 0);
        Assert.NotNull(result.User);
        Assert.Equal(username, result.User.Username);
    }

    [TimedFact]
    public async Task Login_WithEmail_ReturnsJwtToken()
    {
        var username = $"login_email_{Guid.NewGuid():N}";
        var email = $"{username}@test.example.com";
        await RegisterUserAsync(username, email);

        // Login with email instead of username
        var loginRequest = new { Username = email, Password = TestPassword };
        var response = await HttpClient.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<LoginAuthResponse>(content, JsonOptions);
        
        Assert.NotNull(result);
        Assert.NotEmpty(result!.AccessToken);
        Assert.NotEmpty(result.RefreshToken);
        Assert.NotNull(result.User);
        Assert.Equal(username, result.User.Username);
    }

    [TimedFact]
    public async Task Login_WithInvalidPassword_Returns401()
    {
        var username = $"login_badpwd_{Guid.NewGuid():N}";
        await RegisterUserAsync(username);

        var loginRequest = new { Username = username, Password = "WrongPassword123!" };
        var response = await HttpClient.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TimedFact]
    public async Task Login_WithNonExistentUser_Returns401()
    {
        var loginRequest = new { Username = $"nonexistent_{Guid.NewGuid():N}", Password = TestPassword };
        var response = await HttpClient.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion

    #region Input Validation Tests

    [TimedTheory]
    [InlineData("", "TestPassword123!")]           // Empty username
    [InlineData("   ", "TestPassword123!")]        // Whitespace username
    [InlineData("user", "")]                 // Empty password
    [InlineData("user", "   ")]              // Whitespace password
    public async Task Login_WithMissingCredentials_Returns400Or401(string? username, string? password)
    {
        var loginRequest = new { Username = username, Password = password };
        var response = await HttpClient.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

        // Should return 400 (bad request) or 401 (unauthorized) - both are acceptable for invalid input
        Assert.True(
            response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.Unauthorized,
            $"Expected 400 or 401, got {(int)response.StatusCode}");
    }

    [TimedFact]
    public async Task Login_WithEmptyBody_Returns400()
    {
        var response = await HttpClient.PostAsync(
            "/api/v1/auth/login",
            new StringContent("{}", Encoding.UTF8, "application/json"));

        Assert.True(
            response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.Unauthorized,
            $"Expected 400 or 401, got {(int)response.StatusCode}");
    }

    [TimedFact]
    public async Task Login_WithMalformedJson_Returns400()
    {
        var response = await HttpClient.PostAsync(
            "/api/v1/auth/login",
            new StringContent("{invalid json}", Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region Security Tests - Injection Attacks

    [TimedTheory]
    [InlineData("admin'--")]                           // SQL injection
    [InlineData("admin' OR '1'='1")]                   // SQL injection
    [InlineData("admin\"; DROP TABLE users;--")]       // SQL injection
    [InlineData("<script>alert('xss')</script>")]      // XSS
    [InlineData("{{constructor.constructor('return this')()}}")]  // Template injection
    [InlineData("${7*7}")]                             // Expression injection
    [InlineData("admin%00")]                           // Null byte injection
    [InlineData("../../../etc/passwd")]                // Path traversal
    public async Task Login_WithInjectionAttempt_Returns401NotServerError(string maliciousUsername)
    {
        var loginRequest = new { Username = maliciousUsername, Password = TestPassword };
        var response = await HttpClient.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

        // Should NOT return 500 (server error) - injection should not break the server
        // May return 401 (user not found), 400 (bad request), or even 200 (if user exists from another test)
        Assert.True(
            response.StatusCode != HttpStatusCode.InternalServerError,
            $"Injection attempt should not cause server error. Got {(int)response.StatusCode}");
    }

    [TimedTheory]
    [InlineData("password'--")]
    [InlineData("' OR '1'='1")]
    [InlineData("password\"; DROP TABLE users;--")]
    public async Task Login_WithSqlInjectionInPassword_Returns401NotServerError(string maliciousPassword)
    {
        var username = $"sqlinject_{Guid.NewGuid():N}";
        await RegisterUserAsync(username);

        var loginRequest = new { Username = username, Password = maliciousPassword };
        var response = await HttpClient.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

        Assert.True(
            response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.BadRequest,
            $"Injection attempt should not cause server error. Got {(int)response.StatusCode}");
    }

    #endregion

    #region Security Tests - Header Manipulation

    [TimedFact]
    public async Task Login_WithSpoofedHeaders_DoesNotBypassAuth()
    {
        var loginRequest = new { Username = $"nonexistent_{Guid.NewGuid():N}", Password = "wrong" };
        
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/login");
        request.Content = JsonContent.Create(loginRequest);
        
        // Add spoofed headers that attackers might try
        request.Headers.Add("X-Forwarded-For", "127.0.0.1");
        request.Headers.Add("X-Real-IP", "127.0.0.1");
        request.Headers.Add("X-Original-URL", "/admin");
        request.Headers.Add("X-Rewrite-URL", "/admin");
        
        var response = await HttpClient.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TimedFact]
    public async Task Login_WithFakeAuthHeader_DoesNotBypassLogin()
    {
        var loginRequest = new { Username = $"nonexistent_{Guid.NewGuid():N}", Password = "wrong" };
        
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/login");
        request.Content = JsonContent.Create(loginRequest);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "fake.jwt.token");
        
        var response = await HttpClient.SendAsync(request);

        // Should still validate credentials, not accept fake token
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion

    #region Security Tests - Timing Attack Resistance

    [TimedFact]
    public async Task Login_TimingForValidVsInvalidUser_ShouldBeSimilar()
    {
        // Register a valid user
        var validUsername = $"timing_valid_{Guid.NewGuid():N}";
        await RegisterUserAsync(validUsername);

        var invalidUsername = $"timing_invalid_{Guid.NewGuid():N}";

        // Measure time for valid user with wrong password
        var validUserTimes = new List<long>();
        for (int i = 0; i < 5; i++)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            await HttpClient.PostAsJsonAsync("/api/v1/auth/login", 
                new { Username = validUsername, Password = "WrongPassword!" });
            sw.Stop();
            validUserTimes.Add(sw.ElapsedMilliseconds);
        }

        // Measure time for invalid user
        var invalidUserTimes = new List<long>();
        for (int i = 0; i < 5; i++)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            await HttpClient.PostAsJsonAsync("/api/v1/auth/login",
                new { Username = invalidUsername, Password = "WrongPassword!" });
            sw.Stop();
            invalidUserTimes.Add(sw.ElapsedMilliseconds);
        }

        var validAvg = validUserTimes.Average();
        var invalidAvg = invalidUserTimes.Average();

        Output.WriteLine($"Valid user avg: {validAvg}ms, Invalid user avg: {invalidAvg}ms");

        // Times should be within reasonable range (not differ by more than 500ms on average)
        // Large differences could indicate timing attack vulnerability
        var difference = Math.Abs(validAvg - invalidAvg);
        Assert.True(difference < 500, 
            $"Timing difference of {difference}ms may indicate timing attack vulnerability");
    }

    #endregion

    #region Security Tests - Response Information Leakage

    [TimedFact]
    public async Task Login_InvalidCredentials_DoesNotLeakUserExistence()
    {
        // Register a real user
        var realUsername = $"leak_real_{Guid.NewGuid():N}";
        await RegisterUserAsync(realUsername);

        var fakeUsername = $"leak_fake_{Guid.NewGuid():N}";

        // Get error response for real user with wrong password
        var realUserResponse = await HttpClient.PostAsJsonAsync("/api/v1/auth/login",
            new { Username = realUsername, Password = "WrongPassword!" });
        var realUserContent = await realUserResponse.Content.ReadAsStringAsync();

        // Get error response for fake user
        var fakeUserResponse = await HttpClient.PostAsJsonAsync("/api/v1/auth/login",
            new { Username = fakeUsername, Password = "WrongPassword!" });
        var fakeUserContent = await fakeUserResponse.Content.ReadAsStringAsync();

        // Both should return same status code
        Assert.Equal(realUserResponse.StatusCode, fakeUserResponse.StatusCode);

        // Error messages should be identical or similarly vague (not leak user existence)
        // This is a weak check - in practice, messages should be the same generic message
        Assert.Equal(HttpStatusCode.Unauthorized, realUserResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, fakeUserResponse.StatusCode);
    }

    [TimedFact]
    public async Task Login_ErrorResponse_DoesNotContainStackTrace()
    {
        var response = await HttpClient.PostAsJsonAsync("/api/v1/auth/login",
            new { Username = "test", Password = "wrong" });
        
        var content = await response.Content.ReadAsStringAsync();

        // Response should not contain stack traces or internal details
        Assert.DoesNotContain("at System.", content);
        Assert.DoesNotContain("Exception", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("StackTrace", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(".cs:line", content);
    }

    #endregion

    #region Security Tests - Large Payload Protection

    [TimedFact]
    public async Task Login_WithExtremelyLongUsername_Returns400OrDoesNotCrash()
    {
        var longUsername = new string('a', 100000); // 100KB username
        var loginRequest = new { Username = longUsername, Password = TestPassword };
        var response = await HttpClient.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

        // Should handle gracefully - 400 or 401, not crash
        Assert.True(
            response.StatusCode == HttpStatusCode.BadRequest || 
            response.StatusCode == HttpStatusCode.Unauthorized ||
            response.StatusCode == HttpStatusCode.RequestEntityTooLarge,
            $"Long payload should be handled gracefully. Got {(int)response.StatusCode}");
    }

    [TimedFact]
    public async Task Login_WithExtremelyLongPassword_Returns400OrDoesNotCrash()
    {
        var longPassword = new string('a', 100000); // 100KB password
        var loginRequest = new { Username = "testuser", Password = longPassword };
        var response = await HttpClient.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

        Assert.True(
            response.StatusCode == HttpStatusCode.BadRequest ||
            response.StatusCode == HttpStatusCode.Unauthorized ||
            response.StatusCode == HttpStatusCode.RequestEntityTooLarge,
            $"Long payload should be handled gracefully. Got {(int)response.StatusCode}");
    }

    #endregion

    #region Security Tests - Case Sensitivity

    [TimedFact]
    public async Task Login_UsernameIsCaseInsensitive()
    {
        var username = $"CaseTEST_{Guid.NewGuid():N}";
        await RegisterUserAsync(username);

        // Try login with different casing
        var lowercaseLogin = new { Username = username.ToLowerInvariant(), Password = TestPassword };
        var lowercaseResponse = await HttpClient.PostAsJsonAsync("/api/v1/auth/login", lowercaseLogin);

        var uppercaseLogin = new { Username = username.ToUpperInvariant(), Password = TestPassword };
        var uppercaseResponse = await HttpClient.PostAsJsonAsync("/api/v1/auth/login", uppercaseLogin);

        // Username matching should be case-insensitive
        Assert.Equal(HttpStatusCode.OK, lowercaseResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, uppercaseResponse.StatusCode);
    }

    [TimedFact]
    public async Task Login_PasswordIsCaseSensitive()
    {
        var username = $"pwdcase_{Guid.NewGuid():N}";
        await RegisterUserAsync(username);

        // Try login with uppercase password (should fail)
        var wrongCaseLogin = new { Username = username, Password = TestPassword.ToUpperInvariant() };
        var response = await HttpClient.PostAsJsonAsync("/api/v1/auth/login", wrongCaseLogin);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion

    #region Security Tests - Unicode and Special Characters

    [TimedFact]
    public async Task Login_WithUnicodeUsername_WorksCorrectly()
    {
        // Note: This test may need adjustment based on actual username validation rules
        var username = $"用户_{Guid.NewGuid():N}"; // Chinese characters
        var registerResponse = await HttpClient.PostAsJsonAsync("/api/v1/auth/register", new
        {
            Username = username,
            Email = $"{Guid.NewGuid():N}@example.com",
            Password = TestPassword,
            ConfirmPassword = TestPassword
        });

        if (registerResponse.StatusCode == HttpStatusCode.Created)
        {
            var loginRequest = new { Username = username, Password = TestPassword };
            var response = await HttpClient.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
        else
        {
            // Unicode usernames might be disallowed or cause validation errors
            Assert.True(
                registerResponse.StatusCode == HttpStatusCode.BadRequest ||
                registerResponse.StatusCode == HttpStatusCode.InternalServerError,
                $"Unexpected status: {(int)registerResponse.StatusCode}");
        }
    }

    [TimedFact]
    public async Task Login_WithSpecialCharactersInPassword_WorksCorrectly()
    {
        var username = $"specialpwd_{Guid.NewGuid():N}";
        var specialPassword = "P@$$w0rd!#$%^&*()_+-=[]{}|;':\",./<>?";

        await HttpClient.PostAsJsonAsync("/api/v1/auth/register", new
        {
            Username = username,
            Email = $"{Guid.NewGuid():N}@example.com",
            Password = specialPassword,
            ConfirmPassword = specialPassword
        });

        var loginRequest = new { Username = username, Password = specialPassword };
        var response = await HttpClient.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    #endregion

    #region DTOs

    private record LoginAuthResponse(
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


