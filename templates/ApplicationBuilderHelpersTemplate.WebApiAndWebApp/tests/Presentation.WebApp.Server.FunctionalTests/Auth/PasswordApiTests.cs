using Presentation.WebApp.Server.FunctionalTests;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Presentation.WebApp.Server.FunctionalTests.Auth;

/// <summary>
/// Functional tests for Password Management API endpoints.
/// Tests change password, forgot password, reset password, and security edge cases.
/// </summary>
public class PasswordApiTests : WebAppTestBase
{
    private const string NewPassword = "NewPassword456!";

    public PasswordApiTests(ITestOutputHelper output) : base(output)
    {
    }

    #region Change Password Tests

    [TimedFact]
    public async Task ChangePassword_WithValidCredentials_ReturnsNoContent()
    {
        var authResult = await RegisterUniqueUserAsync();
        Assert.NotNull(authResult);

        var userId = authResult!.User.Id;
        var changePasswordRequest = new { CurrentPassword = TestPassword, NewPassword = NewPassword };
        using var request = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/auth/users/{userId}/identity/password");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResult.AccessToken);
        request.Content = JsonContent.Create(changePasswordRequest);
        var response = await HttpClient.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [TimedFact]
    public async Task ChangePassword_OldPasswordNoLongerWorks()
    {
        var username = $"pwd_old_{Guid.NewGuid():N}";
        var authResult = await RegisterUserAsync(username);
        Assert.NotNull(authResult);

        var userId = authResult!.User.Id;

        // Change password
        var changePasswordRequest = new { CurrentPassword = TestPassword, NewPassword = NewPassword };
        using var request = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/auth/users/{userId}/identity/password");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResult.AccessToken);
        request.Content = JsonContent.Create(changePasswordRequest);
        await HttpClient.SendAsync(request);

        // Try to login with old password
        var loginOldResponse = await HttpClient.PostAsJsonAsync("/api/v1/auth/login",
            new { Username = username, Password = TestPassword });

        Assert.Equal(HttpStatusCode.Unauthorized, loginOldResponse.StatusCode);
    }

    [TimedFact]
    public async Task ChangePassword_NewPasswordWorks()
    {
        var username = $"pwd_new_{Guid.NewGuid():N}";
        var authResult = await RegisterUserAsync(username);
        Assert.NotNull(authResult);

        var userId = authResult!.User.Id;

        // Change password
        var changePasswordRequest = new { CurrentPassword = TestPassword, NewPassword = NewPassword };
        using var request = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/auth/users/{userId}/identity/password");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResult.AccessToken);
        request.Content = JsonContent.Create(changePasswordRequest);
        await HttpClient.SendAsync(request);

        // Login with new password
        var loginNewResponse = await HttpClient.PostAsJsonAsync("/api/v1/auth/login",
            new { Username = username, Password = NewPassword });

        Assert.Equal(HttpStatusCode.OK, loginNewResponse.StatusCode);
    }

    [TimedFact]
    public async Task ChangePassword_WithWrongCurrentPassword_Returns401()
    {
        var authResult = await RegisterUniqueUserAsync();
        Assert.NotNull(authResult);

        var userId = authResult!.User.Id;
        var changePasswordRequest = new { CurrentPassword = "WrongPassword123!", NewPassword = NewPassword };
        using var request = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/auth/users/{userId}/identity/password");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResult.AccessToken);
        request.Content = JsonContent.Create(changePasswordRequest);
        var response = await HttpClient.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TimedFact]
    public async Task ChangePassword_WithWeakNewPassword_Returns400()
    {
        var authResult = await RegisterUniqueUserAsync();
        Assert.NotNull(authResult);

        var userId = authResult!.User.Id;
        var changePasswordRequest = new { CurrentPassword = TestPassword, NewPassword = "123" };
        using var request = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/auth/users/{userId}/identity/password");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResult.AccessToken);
        request.Content = JsonContent.Create(changePasswordRequest);
        var response = await HttpClient.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TimedFact]
    public async Task ChangePassword_WithoutAuthentication_Returns401()
    {
        var randomUserId = Guid.NewGuid();
        var changePasswordRequest = new { CurrentPassword = TestPassword, NewPassword = NewPassword };
        var response = await HttpClient.PutAsJsonAsync($"/api/v1/auth/users/{randomUserId}/identity/password", changePasswordRequest);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TimedFact]
    public async Task ChangePassword_WithSamePassword_MayReturn400()
    {
        var authResult = await RegisterUniqueUserAsync();
        Assert.NotNull(authResult);

        var userId = authResult!.User.Id;

        // Try to change to the same password
        var changePasswordRequest = new { CurrentPassword = TestPassword, NewPassword = TestPassword };
        using var request = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/auth/users/{userId}/identity/password");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResult.AccessToken);
        request.Content = JsonContent.Create(changePasswordRequest);
        var response = await HttpClient.SendAsync(request);

        // Some systems prevent reusing same password, others allow it
        Output.WriteLine($"Same password change returned: {(int)response.StatusCode}");
        Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    #endregion

    #region Forgot Password Tests

    [TimedFact]
    public async Task ForgotPassword_WithValidEmail_ReturnsNoContent()
    {
        var username = $"forgot_valid_{Guid.NewGuid():N}";
        await RegisterUserAsync(username);

        var forgotRequest = new { Email = $"{username}@example.com" };
        var response = await HttpClient.PostAsJsonAsync("/api/v1/auth/forgot-password", forgotRequest);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [TimedFact]
    public async Task ForgotPassword_WithNonExistentEmail_ReturnsNoContent()
    {
        // Security: Should not reveal whether email exists
        var forgotRequest = new { Email = $"nonexistent_{Guid.NewGuid():N}@example.com" };
        var response = await HttpClient.PostAsJsonAsync("/api/v1/auth/forgot-password", forgotRequest);

        // Always returns success to prevent email enumeration
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [TimedFact]
    public async Task ForgotPassword_WithInvalidEmailFormat_Returns400()
    {
        var forgotRequest = new { Email = "not-an-email" };
        var response = await HttpClient.PostAsJsonAsync("/api/v1/auth/forgot-password", forgotRequest);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TimedFact]
    public async Task ForgotPassword_WithEmptyEmail_Returns400()
    {
        var forgotRequest = new { Email = "" };
        var response = await HttpClient.PostAsJsonAsync("/api/v1/auth/forgot-password", forgotRequest);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region Reset Password Tests

    [TimedFact]
    public async Task ResetPassword_WithInvalidToken_Returns400()
    {
        var username = $"reset_invalid_{Guid.NewGuid():N}";
        await RegisterUserAsync(username);

        var resetRequest = new
        {
            Email = $"{username}@example.com",
            Token = "invalid-token",
            NewPassword = NewPassword
        };
        var response = await HttpClient.PostAsJsonAsync("/api/v1/auth/reset-password", resetRequest);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TimedFact]
    public async Task ResetPassword_WithNonExistentEmail_Returns400()
    {
        var resetRequest = new
        {
            Email = $"nonexistent_{Guid.NewGuid():N}@example.com",
            Token = "some-token",
            NewPassword = NewPassword
        };
        var response = await HttpClient.PostAsJsonAsync("/api/v1/auth/reset-password", resetRequest);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TimedFact]
    public async Task ResetPassword_WithWeakPassword_Returns400()
    {
        var username = $"reset_weak_{Guid.NewGuid():N}";
        await RegisterUserAsync(username);

        var resetRequest = new
        {
            Email = $"{username}@example.com",
            Token = "some-token",
            NewPassword = "123"
        };
        var response = await HttpClient.PostAsJsonAsync("/api/v1/auth/reset-password", resetRequest);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TimedFact]
    public async Task ResetPassword_WithMalformedRequest_Returns400()
    {
        var response = await HttpClient.PostAsync(
            "/api/v1/auth/reset-password",
            new StringContent("{}", Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region Security Tests - Email Enumeration Prevention

    [TimedFact]
    public async Task ForgotPassword_ResponseTime_SimilarForExistingAndNonExisting()
    {
        // Register a real user
        var realUsername = $"enum_real_{Guid.NewGuid():N}";
        await RegisterUserAsync(realUsername);

        var realEmail = $"{realUsername}@example.com";
        var fakeEmail = $"fake_{Guid.NewGuid():N}@example.com";

        // Measure response time for real email
        var realTimes = new List<long>();
        for (int i = 0; i < 3; i++)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            await HttpClient.PostAsJsonAsync("/api/v1/auth/forgot-password", new { Email = realEmail });
            sw.Stop();
            realTimes.Add(sw.ElapsedMilliseconds);
        }

        // Measure response time for fake email
        var fakeTimes = new List<long>();
        for (int i = 0; i < 3; i++)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            await HttpClient.PostAsJsonAsync("/api/v1/auth/forgot-password", new { Email = fakeEmail });
            sw.Stop();
            fakeTimes.Add(sw.ElapsedMilliseconds);
        }

        var realAvg = realTimes.Average();
        var fakeAvg = fakeTimes.Average();

        Output.WriteLine($"Real email avg: {realAvg}ms, Fake email avg: {fakeAvg}ms");

        // Times should be similar to prevent timing-based enumeration
        // Using 1000ms threshold to account for test environment variance
        var difference = Math.Abs(realAvg - fakeAvg);
        Assert.True(difference < 1000, $"Timing difference of {difference}ms may indicate enumeration vulnerability");
    }

    #endregion

    #region Security Tests - Token Manipulation

    [TimedTheory]
    [InlineData("../../../etc/passwd")]  // Path traversal
    [InlineData("<script>alert(1)</script>")]  // XSS
    [InlineData("' OR '1'='1")]  // SQL injection
    [InlineData("{{7*7}}")]  // Template injection
    public async Task ResetPassword_WithMaliciousToken_DoesNotCauseServerError(string maliciousToken)
    {
        var resetRequest = new
        {
            Email = "test@example.com",
            Token = maliciousToken,
            NewPassword = NewPassword
        };
        var response = await HttpClient.PostAsJsonAsync("/api/v1/auth/reset-password", resetRequest);

        // Should handle gracefully, not crash
        Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    #endregion

    #region Security Tests - Brute Force Protection

    [TimedFact]
    public async Task ChangePassword_MultipleWrongAttempts_DoesNotLockAccount()
    {
        var username = $"pwd_brute_{Guid.NewGuid():N}";
        var authResult = await RegisterUserAsync(username);
        Assert.NotNull(authResult);

        var userId = authResult!.User.Id;

        // Try multiple wrong current passwords
        for (int i = 0; i < 5; i++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/auth/users/{userId}/identity/password");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResult.AccessToken);
            request.Content = JsonContent.Create(new { CurrentPassword = $"Wrong{i}!", NewPassword = NewPassword });
            await HttpClient.SendAsync(request);
        }

        // Account should still work for login (not locked out via password change failures)
        var loginResponse = await HttpClient.PostAsJsonAsync("/api/v1/auth/login",
            new { Username = username, Password = TestPassword });

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
    }

    #endregion

    #region Security Tests - Password Complexity

    [TimedTheory]
    [InlineData("Password1!")]           // Meets minimum requirements
    [InlineData("P@$$w0rdVeryL0ng!")]   // Long password
    [InlineData("🔐Password123!")]       // Unicode in password
    public async Task ChangePassword_WithValidComplexPassword_Succeeds(string newPassword)
    {
        var authResult = await RegisterUniqueUserAsync();
        Assert.NotNull(authResult);

        var userId = authResult!.User.Id;
        var changePasswordRequest = new { CurrentPassword = TestPassword, NewPassword = newPassword };
        using var request = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/auth/users/{userId}/identity/password");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResult.AccessToken);
        request.Content = JsonContent.Create(changePasswordRequest);
        var response = await HttpClient.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    #endregion

    #region Security Tests - Response Information

    [TimedFact]
    public async Task ChangePassword_ErrorResponse_DoesNotLeakInfo()
    {
        var authResult = await RegisterUniqueUserAsync();
        Assert.NotNull(authResult);

        var userId = authResult!.User.Id;
        var changePasswordRequest = new { CurrentPassword = "WrongPassword!", NewPassword = NewPassword };
        using var request = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/auth/users/{userId}/identity/password");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResult.AccessToken);
        request.Content = JsonContent.Create(changePasswordRequest);
        var response = await HttpClient.SendAsync(request);

        var content = await response.Content.ReadAsStringAsync();

        // Should not leak internal details
        Assert.DoesNotContain("at System.", content);
        Assert.DoesNotContain("StackTrace", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("passwordhash", content, StringComparison.OrdinalIgnoreCase);
    }

    [TimedFact]
    public async Task ForgotPassword_Response_DoesNotRevealEmailExistence()
    {
        var realUsername = $"leak_real_{Guid.NewGuid():N}";
        await RegisterUserAsync(realUsername);

        var realResponse = await HttpClient.PostAsJsonAsync("/api/v1/auth/forgot-password",
            new { Email = $"{realUsername}@example.com" });
        var fakeResponse = await HttpClient.PostAsJsonAsync("/api/v1/auth/forgot-password",
            new { Email = $"fake_{Guid.NewGuid():N}@example.com" });

        // Both should return the same status code
        Assert.Equal(realResponse.StatusCode, fakeResponse.StatusCode);
    }

    #endregion

    #region Security Tests - Session Invalidation

    [TimedFact]
    public async Task ChangePassword_MayInvalidateOtherSessions()
    {
        var username = $"pwd_session_{Guid.NewGuid():N}";
        var authResult1 = await RegisterUserAsync(username);
        Assert.NotNull(authResult1);

        var userId = authResult1!.User.Id;

        // Login again to create another session
        var loginResponse = await HttpClient.PostAsJsonAsync("/api/v1/auth/login",
            new { Username = username, Password = TestPassword });
        var content = await loginResponse.Content.ReadAsStringAsync();
        var authResult2 = JsonSerializer.Deserialize<AuthResponse>(content, JsonOptions);
        Assert.NotNull(authResult2);

        // Change password using first session
        using var changeRequest = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/auth/users/{userId}/identity/password");
        changeRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResult1.AccessToken);
        changeRequest.Content = JsonContent.Create(new { CurrentPassword = TestPassword, NewPassword = NewPassword });
        await HttpClient.SendAsync(changeRequest);

        // Check if second session's refresh token still works
        var refreshResponse = await HttpClient.PostAsJsonAsync("/api/v1/auth/refresh",
            new { RefreshToken = authResult2!.RefreshToken });

        // Document the behavior - some systems invalidate all sessions on password change
        Output.WriteLine($"Other session after password change: {(int)refreshResponse.StatusCode}");
    }

    #endregion

    #region Helper Methods

    private async Task<AuthResponse?> RegisterUniqueUserAsync()
    {
        return await RegisterUserAsync($"pwd_test_{Guid.NewGuid():N}");
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








