using System.Security.Claims;
using System.Text.Json;
using Application.Client.Identity.Interfaces.Inbound;
using Application.Client.Identity.Models;
using Application.Client.Serialization;

namespace Application.Client.Identity.Services;

internal class ClientAuthStateProvider(ITokenStorage tokenStorage, AuthStateNotifier notifier) : IAuthStateProvider
{
    public AuthState CurrentState => notifier.CurrentState;
    public event Action? OnStateChanged
    {
        add => notifier.OnStateChanged += value;
        remove => notifier.OnStateChanged -= value;
    }

    public async Task InitializeAsync()
    {
        var credentials = await tokenStorage.GetCredentialsAsync();
        if (credentials != null && credentials.IsValid)
        {
            notifier.SetState(BuildAuthState(credentials));
        }
        else
        {
            notifier.ClearState();
        }
    }

    public async Task UpdateStateAsync(StoredCredentials credentials)
    {
        await tokenStorage.StoreCredentialsAsync(credentials);
        notifier.SetState(BuildAuthState(credentials));
    }

    public async Task ClearStateAsync()
    {
        await tokenStorage.ClearCredentialsAsync();
        notifier.ClearState();
    }

    private static AuthState BuildAuthState(StoredCredentials credentials)
    {
        try
        {
            var claims = ParseClaimsFromJwt(credentials.AccessToken);
            
            var userIdStr = claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier || c.Type == "sub")?.Value;
            var username = claims.FirstOrDefault(c => c.Type == ClaimTypes.Name || c.Type == "name")?.Value;
            var email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email || c.Type == "email")?.Value;
            var twoFactorEnabled = claims.Any(c => c.Type == "amr" && c.Value == "mfa") ||
                                   claims.Any(c => c.Type == "2fa_enabled" && c.Value == "true");

            var userId = Guid.TryParse(userIdStr, out var parsedId) ? parsedId : Guid.Empty;

            return new AuthState
            {
                IsAuthenticated = true,
                UserId = userId,
                Username = username,
                Email = email,
                Roles = credentials.Roles,
                Permissions = credentials.Permissions,
                TokenExpiry = credentials.AccessTokenExpiry,
                TwoFactorEnabled = twoFactorEnabled
            };
        }
        catch
        {
            return AuthState.Anonymous;
        }
    }

    private static IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
    {
        var claims = new List<Claim>();
        var payload = jwt.Split('.')[1];
        var jsonBytes = ParseBase64WithoutPadding(payload);
        var keyValuePairs = JsonSerializer.Deserialize(
            jsonBytes,
            ApplicationClientJsonContext.Default.DictionaryStringJsonElement);

        if (keyValuePairs == null)
            return claims;

        foreach (var kvp in keyValuePairs)
        {
            var element = kvp.Value;
            if (element.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in element.EnumerateArray())
                {
                    claims.Add(new Claim(kvp.Key, item.GetString() ?? ""));
                }
            }
            else
            {
                claims.Add(new Claim(kvp.Key, element.ToString()));
            }
        }

        return claims;
    }

    private static byte[] ParseBase64WithoutPadding(string base64)
    {
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }
        return Convert.FromBase64String(base64.Replace('-', '+').Replace('_', '/'));
    }
}
