using Application.Shared.Interfaces.Outbound;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Mock.Services;

internal sealed class MockEmailService(ILogger<MockEmailService> logger) : IEmailService
{
    public Task SendPasswordResetLinkAsync(string email, string resetLink, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "[MOCK EMAIL] Password Reset Link sent to {Email}\n" +
            "Reset Link: {ResetLink}",
            email, resetLink);

        return Task.CompletedTask;
    }

    public Task SendPasswordResetCodeAsync(string email, string resetCode, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "[MOCK EMAIL] Password Reset Code sent to {Email}\n" +
            "Reset Code: {ResetCode}",
            email, resetCode);

        return Task.CompletedTask;
    }

    public Task SendEmailConfirmationLinkAsync(string email, string confirmationLink, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "[MOCK EMAIL] Email Confirmation Link sent to {Email}\n" +
            "Confirmation Link: {ConfirmationLink}",
            email, confirmationLink);

        return Task.CompletedTask;
    }
}
