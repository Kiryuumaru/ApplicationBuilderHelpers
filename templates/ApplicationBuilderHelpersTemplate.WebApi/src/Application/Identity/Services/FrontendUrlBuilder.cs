using System.Text.Encodings.Web;
using Application.Identity.Interfaces.Inbound;
using Application.Identity.Models;
using Microsoft.Extensions.Options;

namespace Application.Identity.Services;

internal sealed class FrontendUrlBuilder(IOptions<FrontendUrlOptions> options) : IFrontendUrlBuilder
{
    private readonly FrontendUrlOptions _options = options.Value;

    public string BuildPasswordResetUrl(string email, string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        var baseUrl = _options.BaseUrl.TrimEnd('/');
        var path = _options.PasswordResetPath.TrimStart('/');
        var encodedEmail = Uri.EscapeDataString(email);
        var encodedToken = UrlEncoder.Default.Encode(token);

        return $"{baseUrl}/{path}?email={encodedEmail}&token={encodedToken}";
    }

    public string BuildEmailVerificationUrl(string email, string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        var baseUrl = _options.BaseUrl.TrimEnd('/');
        var path = _options.EmailVerificationPath.TrimStart('/');
        var encodedEmail = Uri.EscapeDataString(email);
        var encodedToken = UrlEncoder.Default.Encode(token);

        return $"{baseUrl}/{path}?email={encodedEmail}&token={encodedToken}";
    }
}
