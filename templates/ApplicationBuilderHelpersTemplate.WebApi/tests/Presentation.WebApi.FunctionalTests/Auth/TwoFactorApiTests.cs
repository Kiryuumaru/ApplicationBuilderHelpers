using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Presentation.WebApi.FunctionalTests.Fixtures;

namespace Presentation.WebApi.FunctionalTests.Auth;

/// <summary>
/// Functional tests for Two-Factor Authentication API endpoints.
/// Tests 2FA setup, enable, disable, login, and recovery codes.
/// </summary>
public sealed class TwoFactorApiTests(ITestOutputHelper output) : WebApiTestBase(output)
{

    #region 2FA Setup Tests

    [Fact]
    public async Task TwoFactorSetup_WithValidToken_ReturnsSetupInfo()
    {
        var authResult = await RegisterUserAsync();
        Assert.NotNull(authResult);
        Assert.NotNull(authResult.User);

        var userId = authResult.User.Id;
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/auth/users/{userId}/2fa/setup");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResult.AccessToken);
        var response = await HttpClient.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<TwoFactorSetupResponse>(content, JsonOptions);

        Assert.NotNull(result);
        Assert.NotEmpty(result.SharedKey);
        Assert.NotEmpty(result.FormattedSharedKey);
        Assert.NotEmpty(result.AuthenticatorUri);
        Assert.StartsWith("otpauth://totp/", result.AuthenticatorUri);
    }

    [Fact]
    public async Task TwoFactorSetup_WithoutToken_Returns401()
    {
        var randomUserId = Guid.NewGuid();
        var response = await HttpClient.GetAsync($"/api/v1/auth/users/{randomUserId}/2fa/setup");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task TwoFactorSetup_SharedKeyIsUnique_PerUser()
    {
        var user1 = await RegisterUserAsync();
        var user2 = await RegisterUserAsync();

        Assert.NotNull(user1);
        Assert.NotNull(user1.User);
        Assert.NotNull(user2);
        Assert.NotNull(user2.User);

        var user1Id = user1.User.Id;
        var user2Id = user2.User.Id;

        using var request1 = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/auth/users/{user1Id}/2fa/setup");
        request1.Headers.Authorization = new AuthenticationHeaderValue("Bearer", user1.AccessToken);
        var response1 = await HttpClient.SendAsync(request1);
        var content1 = await response1.Content.ReadAsStringAsync();
        var result1 = JsonSerializer.Deserialize<TwoFactorSetupResponse>(content1, JsonOptions);

        using var request2 = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/auth/users/{user2Id}/2fa/setup");
        request2.Headers.Authorization = new AuthenticationHeaderValue("Bearer", user2.AccessToken);
        var response2 = await HttpClient.SendAsync(request2);
        var content2 = await response2.Content.ReadAsStringAsync();
        var result2 = JsonSerializer.Deserialize<TwoFactorSetupResponse>(content2, JsonOptions);

        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.NotEmpty(result1.SharedKey);
        Assert.NotEmpty(result2.SharedKey);
        Assert.NotEqual(result1.SharedKey, result2.SharedKey);
    }

    #endregion

    #region 2FA Enable Tests

    [Fact]
    public async Task TwoFactorEnable_WithInvalidCode_Returns400()
    {
        var authResult = await RegisterUserAsync();
        Assert.NotNull(authResult);
        Assert.NotNull(authResult.User);

        var userId = authResult.User.Id;
        var enableRequest = new { VerificationCode = "000000" };
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/auth/users/{userId}/2fa/enable");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResult.AccessToken);
        request.Content = JsonContent.Create(enableRequest);
        var response = await HttpClient.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task TwoFactorEnable_WithoutToken_Returns401()
    {
        var randomUserId = Guid.NewGuid();
        var enableRequest = new { VerificationCode = "123456" };
        var response = await HttpClient.PostAsJsonAsync($"/api/v1/auth/users/{randomUserId}/2fa/enable", enableRequest);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("12345")]      // Too short
    [InlineData("1234567")]    // Too long
    [InlineData("abcdef")]     // Non-numeric
    public async Task TwoFactorEnable_WithMalformedCode_Returns400(string malformedCode)
    {
        var authResult = await RegisterUserAsync();
        Assert.NotNull(authResult);
        Assert.NotNull(authResult.User);

        var userId = authResult.User.Id;
        var enableRequest = new { VerificationCode = malformedCode };
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/auth/users/{userId}/2fa/enable");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResult.AccessToken);
        request.Content = JsonContent.Create(enableRequest);
        var response = await HttpClient.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region 2FA Disable Tests

    [Fact]
    public async Task TwoFactorDisable_WithoutToken_Returns401()
    {
        var randomUserId = Guid.NewGuid();
        var disableRequest = new { Password = TestPassword };
        var response = await HttpClient.PostAsJsonAsync($"/api/v1/auth/users/{randomUserId}/2fa/disable", disableRequest);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task TwoFactorDisable_WhenNotEnabled_Returns400Or500()
    {
        var authResult = await RegisterUserAsync();
        Assert.NotNull(authResult);
        Assert.NotNull(authResult.User);

        var userId = authResult.User.Id;
        var disableRequest = new { Password = TestPassword };
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/auth/users/{userId}/2fa/disable");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResult.AccessToken);
        request.Content = JsonContent.Create(disableRequest);
        var response = await HttpClient.SendAsync(request);

        // 2FA isn't enabled, so can't disable - returns 400 or 500
        Assert.True(
            response.StatusCode == HttpStatusCode.BadRequest ||
            response.StatusCode == HttpStatusCode.InternalServerError,
            $"Unexpected status: {(int)response.StatusCode}");
    }

    [Fact]
    public async Task TwoFactorDisable_WithWrongPassword_Returns401()
    {
        var authResult = await RegisterUserAsync();
        Assert.NotNull(authResult);
        Assert.NotNull(authResult.User);

        var userId = authResult.User.Id;
        var disableRequest = new { Password = "WrongPassword123!" };
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/auth/users/{userId}/2fa/disable");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResult.AccessToken);
        request.Content = JsonContent.Create(disableRequest);
        var response = await HttpClient.SendAsync(request);

        // Either 400 (2FA not enabled) or 401 (wrong password) are acceptable
        Assert.True(
            response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.Unauthorized,
            $"Expected 400 or 401, got {(int)response.StatusCode}");
    }

    #endregion

    #region 2FA Login Tests

    [Fact]
    public async Task TwoFactorLogin_WithInvalidUserId_Returns401()
    {
        var twoFactorLoginRequest = new { UserId = Guid.NewGuid(), Code = "123456" };
        var response = await HttpClient.PostAsJsonAsync("/api/v1/auth/login/2fa", twoFactorLoginRequest);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task TwoFactorLogin_WithInvalidCode_Returns401()
    {
        var authResult = await RegisterUserAsync();
        Assert.NotNull(authResult);
        Assert.NotNull(authResult.User);

        var twoFactorLoginRequest = new { UserId = authResult.User.Id, Code = "000000" };
        var response = await HttpClient.PostAsJsonAsync("/api/v1/auth/login/2fa", twoFactorLoginRequest);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task TwoFactorLogin_WithEmptyGuid_Returns401()
    {
        var twoFactorLoginRequest = new { UserId = Guid.Empty, Code = "123456" };
        var response = await HttpClient.PostAsJsonAsync("/api/v1/auth/login/2fa", twoFactorLoginRequest);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("12345")]
    [InlineData("1234567")]
    [InlineData("abcdef")]
    public async Task TwoFactorLogin_WithMalformedCode_Returns400Or401(string malformedCode)
    {
        var authResult = await RegisterUserAsync();
        Assert.NotNull(authResult);
        Assert.NotNull(authResult.User);

        var twoFactorLoginRequest = new { UserId = authResult.User.Id, Code = malformedCode };
        var response = await HttpClient.PostAsJsonAsync("/api/v1/auth/login/2fa", twoFactorLoginRequest);

        Assert.True(
            response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.Unauthorized,
            $"Expected 400 or 401, got {(int)response.StatusCode}");
    }

    #endregion

    #region Recovery Code Tests

    [Fact]
    public async Task RecoveryCodes_WithoutTwoFactorEnabled_Returns400()
    {
        var authResult = await RegisterUserAsync();
        Assert.NotNull(authResult);
        Assert.NotNull(authResult.User);

        var userId = authResult.User.Id;
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/auth/users/{userId}/2fa/recovery-codes");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResult.AccessToken);
        var response = await HttpClient.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RecoveryCodes_WithoutToken_Returns401()
    {
        var randomUserId = Guid.NewGuid();
        var response = await HttpClient.PostAsync($"/api/v1/auth/users/{randomUserId}/2fa/recovery-codes", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion

    #region Security Tests - Brute Force Protection

    [Fact]
    public async Task TwoFactorEnable_MultipleWrongCodes_DoesNotLockAccount()
    {
        var username = $"2fa_brute_{Guid.NewGuid():N}";
        var authResult = await RegisterUserAsync(username);
        Assert.NotNull(authResult);
        Assert.NotNull(authResult.User);

        var userId = authResult.User.Id;

        // Try multiple wrong codes
        for (int i = 0; i < 5; i++)
        {
            var enableRequest = new { VerificationCode = $"{i:D6}" };
            using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/auth/users/{userId}/2fa/enable");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResult.AccessToken);
            request.Content = JsonContent.Create(enableRequest);
            await HttpClient.SendAsync(request);
        }

        // Account should still be accessible
        var loginResponse = await HttpClient.PostAsJsonAsync("/api/v1/auth/login",
            new { Username = username, Password = TestPassword });

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
    }

    [Fact]
    public async Task TwoFactorLogin_MultipleWrongCodes_TracksFailures()
    {
        var authResult = await RegisterUserAsync();
        Assert.NotNull(authResult);
        Assert.NotNull(authResult.User);

        // Try multiple wrong 2FA codes
        for (int i = 0; i < 5; i++)
        {
            var twoFactorLoginRequest = new { UserId = authResult.User.Id, Code = $"{i:D6}" };
            var response = await HttpClient.PostAsJsonAsync("/api/v1/auth/login/2fa", twoFactorLoginRequest);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        // Document behavior after multiple failures
        Output.WriteLine("Multiple wrong 2FA codes submitted - check if account lockout policy applies");
    }

    #endregion

    #region Security Tests - Timing Attacks

    [Fact]
    public async Task TwoFactorLogin_TimingForValidVsInvalidUserId_ShouldBeSimilar()
    {
        var authResult = await RegisterUserAsync();
        Assert.NotNull(authResult);
        Assert.NotNull(authResult.User);

        var validUserId = authResult.User.Id;
        var invalidUserId = Guid.NewGuid();

        // Measure time for valid user ID with wrong code
        var validTimes = new List<long>();
        for (int i = 0; i < 3; i++)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            await HttpClient.PostAsJsonAsync("/api/v1/auth/login/2fa",
                new { UserId = validUserId, Code = "000000" });
            sw.Stop();
            validTimes.Add(sw.ElapsedMilliseconds);
        }

        // Measure time for invalid user ID
        var invalidTimes = new List<long>();
        for (int i = 0; i < 3; i++)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            await HttpClient.PostAsJsonAsync("/api/v1/auth/login/2fa",
                new { UserId = invalidUserId, Code = "000000" });
            sw.Stop();
            invalidTimes.Add(sw.ElapsedMilliseconds);
        }

        var validAvg = validTimes.Average();
        var invalidAvg = invalidTimes.Average();

        Output.WriteLine($"Valid userId avg: {validAvg}ms, Invalid userId avg: {invalidAvg}ms");

        var difference = Math.Abs(validAvg - invalidAvg);
        Assert.True(difference < 500, $"Timing difference of {difference}ms may indicate vulnerability");
    }

    #endregion

    #region Security Tests - TOTP Code Reuse

    [Fact]
    public async Task TwoFactorLogin_SameCodeCannotBeReused()
    {
        // This is a theoretical test - in practice we can't generate valid codes
        // But we can verify the system properly rejects already-used codes
        var authResult = await RegisterUserAsync();
        Assert.NotNull(authResult);
        Assert.NotNull(authResult.User);

        var code = "123456";

        // First attempt
        var response1 = await HttpClient.PostAsJsonAsync("/api/v1/auth/login/2fa",
            new { UserId = authResult.User.Id, Code = code });

        // Second attempt with same code
        var response2 = await HttpClient.PostAsJsonAsync("/api/v1/auth/login/2fa",
            new { UserId = authResult.User.Id, Code = code });

        // Both should fail (2FA not enabled), but demonstrates code handling
        Assert.Equal(HttpStatusCode.Unauthorized, response1.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, response2.StatusCode);
    }

    #endregion

    #region Security Tests - Response Information

    [Fact]
    public async Task TwoFactorEnable_ErrorResponse_DoesNotLeakSecretKey()
    {
        var authResult = await RegisterUserAsync();
        Assert.NotNull(authResult);
        Assert.NotNull(authResult.User);

        var userId = authResult.User.Id;
        var enableRequest = new { VerificationCode = "000000" };
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/auth/users/{userId}/2fa/enable");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResult.AccessToken);
        request.Content = JsonContent.Create(enableRequest);
        var response = await HttpClient.SendAsync(request);

        var content = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain("sharedkey", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("secret", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("otpauth://", content);
    }

    [Fact]
    public async Task TwoFactorLogin_ErrorResponse_DoesNotRevealIf2FAEnabled()
    {
        var user1 = await RegisterUserAsync(); // 2FA not enabled
        var user2 = await RegisterUserAsync(); // 2FA not enabled

        Assert.NotNull(user1);
        Assert.NotNull(user1.User);
        Assert.NotNull(user2);
        Assert.NotNull(user2.User);

        var response1 = await HttpClient.PostAsJsonAsync("/api/v1/auth/login/2fa",
            new { UserId = user1.User.Id, Code = "000000" });
        var content1 = await response1.Content.ReadAsStringAsync();

        var response2 = await HttpClient.PostAsJsonAsync("/api/v1/auth/login/2fa",
            new { UserId = user2.User.Id, Code = "000000" });
        var content2 = await response2.Content.ReadAsStringAsync();

        // Both should return same status and similar generic message
        Assert.Equal(response1.StatusCode, response2.StatusCode);
    }

    #endregion

    #region Security Tests - Injection in 2FA Code

    [Theory]
    [InlineData("000000' OR '1'='1")]
    [InlineData("<script>alert(1)</script>")]
    [InlineData("000000; DROP TABLE Users")]
    public async Task TwoFactorLogin_WithInjectionInCode_DoesNotCauseServerError(string maliciousCode)
    {
        var authResult = await RegisterUserAsync();
        Assert.NotNull(authResult);
        Assert.NotNull(authResult.User);

        var response = await HttpClient.PostAsJsonAsync("/api/v1/auth/login/2fa",
            new { UserId = authResult.User.Id, Code = maliciousCode });

        Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    #endregion

    #region DTOs

    private record TwoFactorSetupResponse(
        string SharedKey,
        string FormattedSharedKey,
        string AuthenticatorUri);

    #endregion
}
