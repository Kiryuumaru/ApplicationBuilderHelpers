using System.Text.Encodings.Web;
using Application.Server.Identity.Interfaces.Inbound;
using Application.Server.Identity.Models;
using Microsoft.Extensions.Options;

namespace Application.Server.Identity.Services;

internal sealed class FrontendUrlBuilder(IOptions<FrontendUrlOptions> options) : IFrontendUrlBuilder
{
    public string BuildPasswordResetUrl(string email, string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        var baseUrl = options.Value.BaseUrl.TrimEnd('/');
        var path = options.Value.PasswordResetPath.TrimStart('/');
        var encodedEmail = Uri.EscapeDataString(email);
        var encodedToken = UrlEncoder.Default.Encode(token);

        return $"{baseUrl}/{path}?email={encodedEmail}&token={encodedToken}";
    }

    public string BuildEmailVerificationUrl(string email, string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        var baseUrl = options.Value.BaseUrl.TrimEnd('/');
        var path = options.Value.EmailVerificationPath.TrimStart('/');
        var encodedEmail = Uri.EscapeDataString(email);
        var encodedToken = UrlEncoder.Default.Encode(token);

        return $"{baseUrl}/{path}?email={encodedEmail}&token={encodedToken}";
    }
}
