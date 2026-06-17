using Domain.Shared.Exceptions;
using Domain.Authorization.Exceptions;
using Domain.Identity.Exceptions;
using Fido2NetLib;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using System.Text.Json;

namespace Presentation.WebApp.Server.Filters;

internal sealed class ApiExceptionFilter(ProblemDetailsFactory problemDetailsFactory, ILogger<ApiExceptionFilter> logger) : IAsyncExceptionFilter
{
    private readonly ProblemDetailsFactory _problemDetailsFactory = problemDetailsFactory ?? throw new ArgumentNullException(nameof(problemDetailsFactory));
    private readonly ILogger<ApiExceptionFilter> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public Task OnExceptionAsync(ExceptionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.ExceptionHandled)
        {
            return Task.CompletedTask;
        }

        var httpContext = context.HttpContext;

        if (httpContext.RequestAborted.IsCancellationRequested)
        {
            return Task.CompletedTask;
        }

        var exception = context.Exception;

        if (TryMapException(exception, out var statusCode, out var title, out var detail, out var extensions))
        {
            if (statusCode >= 500)
            {
                _logger.LogError(exception, "Unhandled exception mapped to {StatusCode}: {Title}", statusCode, title);
            }
            else
            {
                _logger.LogInformation("Exception mapped to {StatusCode}: {Title}", statusCode, title);
            }

            var problem = _problemDetailsFactory.CreateProblemDetails(
                httpContext,
                statusCode: statusCode,
                title: title,
                detail: detail,
                instance: httpContext.Request.Path);

            problem.Extensions["traceId"] = httpContext.TraceIdentifier;

            if (extensions is { Count: > 0 })
            {
                foreach (var pair in extensions)
                {
                    problem.Extensions[pair.Key] = pair.Value;
                }
            }

            context.Result = new ObjectResult(problem)
            {
                StatusCode = statusCode,
            };

            context.ExceptionHandled = true;

            return Task.CompletedTask;
        }

        _logger.LogError(exception, "Unhandled exception (unmapped) caught by ApiExceptionFilter");

        var unexpectedProblem = _problemDetailsFactory.CreateProblemDetails(
            httpContext,
            statusCode: StatusCodes.Status500InternalServerError,
            title: "Internal server error",
            detail: "An unexpected error occurred.",
            instance: httpContext.Request.Path);

        unexpectedProblem.Extensions["traceId"] = httpContext.TraceIdentifier;

        context.Result = new ObjectResult(unexpectedProblem)
        {
            StatusCode = StatusCodes.Status500InternalServerError,
        };

        context.ExceptionHandled = true;

        return Task.CompletedTask;
    }

    private static bool TryMapException(
        Exception exception,
        out int statusCode,
        out string title,
        out string detail,
        out IReadOnlyDictionary<string, object?>? extensions)
    {
        extensions = null;

        switch (exception)
        {
            case Domain.Identity.Exceptions.AuthenticationException:
                statusCode = StatusCodes.Status401Unauthorized;
                title = "Invalid credentials";
                detail = "The username or password is incorrect.";
                return true;

            case TwoFactorSessionInvalidException invalidTwoFactorSession:
                statusCode = StatusCodes.Status401Unauthorized;
                title = "Invalid request";
                detail = invalidTwoFactorSession.Message;
                return true;

            case InvalidTwoFactorCodeException invalidTwoFactorCode:
                statusCode = StatusCodes.Status401Unauthorized;
                title = "Invalid code";
                detail = invalidTwoFactorCode.Message;
                return true;

            case InvalidPasswordException invalidPassword:
                statusCode = StatusCodes.Status401Unauthorized;
                title = "Invalid password";
                detail = invalidPassword.Message;
                return true;

            case RefreshTokenInvalidException refreshTokenInvalid:
                statusCode = StatusCodes.Status401Unauthorized;
                title = refreshTokenInvalid.Error ?? "Authentication failed";
                detail = refreshTokenInvalid.ErrorDescription ?? "The refresh token is invalid or expired.";
                return true;

            case OAuthAuthenticationFailedException oauthFailed:
                statusCode = StatusCodes.Status401Unauthorized;
                title = oauthFailed.Error ?? "Authentication failed";
                detail = oauthFailed.ErrorDescription ?? "OAuth authentication was not successful.";
                return true;

            case PasswordResetTokenInvalidException passwordResetTokenInvalid:
                statusCode = StatusCodes.Status400BadRequest;
                title = "Password reset failed";
                detail = passwordResetTokenInvalid.Message;
                return true;

            case AccountLockedException accountLocked:
                statusCode = StatusCodes.Status403Forbidden;
                title = "Account locked";
                detail = accountLocked.Message;
                extensions = new Dictionary<string, object?>
                {
                    ["lockoutEnd"] = accountLocked.LockoutEnd,
                };
                return true;

            case PermissionDeniedException permissionDenied:
                statusCode = StatusCodes.Status403Forbidden;
                title = "Forbidden";
                detail = permissionDenied.Message;
                extensions = new Dictionary<string, object?>
                {
                    ["requiredPermission"] = permissionDenied.PermissionIdentifier,
                };
                return true;

            case DuplicateEntityException duplicate:
                statusCode = StatusCodes.Status409Conflict;
                title = "Conflict";
                detail = duplicate.Message;
                extensions = new Dictionary<string, object?>
                {
                    ["entityType"] = duplicate.EntityType,
                    ["entityIdentifier"] = duplicate.EntityIdentifier,
                };
                return true;

            case ReservedNameException reserved:
                statusCode = StatusCodes.Status409Conflict;
                title = "Conflict";
                detail = reserved.Message;
                extensions = new Dictionary<string, object?>
                {
                    ["reservedName"] = reserved.ReservedName,
                };
                return true;

            case EntityNotFoundException notFound:
                statusCode = StatusCodes.Status404NotFound;
                if (!string.IsNullOrWhiteSpace(notFound.EntityType) && !string.IsNullOrWhiteSpace(notFound.EntityIdentifier))
                {
                    title = $"{notFound.EntityType} not found";

                    if (Guid.TryParse(notFound.EntityIdentifier, out _))
                    {
                        detail = $"No {notFound.EntityType.ToLowerInvariant()} found with ID '{notFound.EntityIdentifier}'.";
                    }
                    else
                    {
                        detail = notFound.Message;
                    }
                }
                else
                {
                    title = "Not found";
                    detail = notFound.Message;
                }
                extensions = new Dictionary<string, object?>
                {
                    ["entityType"] = notFound.EntityType,
                    ["entityIdentifier"] = notFound.EntityIdentifier,
                };
                return true;

            case SystemRoleException systemRole:
                statusCode = StatusCodes.Status400BadRequest;
                title = "Operation not allowed";
                detail = systemRole.Message;
                extensions = new Dictionary<string, object?>
                {
                    ["roleCode"] = systemRole.RoleCode,
                    ["roleId"] = systemRole.RoleId,
                };
                return true;

            case PasswordValidationException passwordValidation:
                statusCode = StatusCodes.Status400BadRequest;
                title = "Password validation failed";
                detail = passwordValidation.Message;
                return true;

            case TwoFactorException twoFactor:
                statusCode = StatusCodes.Status400BadRequest;
                title = "Two-factor error";
                detail = twoFactor.Message;
                return true;

            case PasskeyException passkey:
                statusCode = StatusCodes.Status400BadRequest;
                title = "Passkey error";
                detail = passkey.Message;
                return true;

            case JsonException json:
                statusCode = StatusCodes.Status400BadRequest;
                title = "Invalid JSON";
                detail = json.Message;
                return true;

            case Fido2VerificationException fido2:
                statusCode = StatusCodes.Status400BadRequest;
                title = "Passkey verification failed";
                detail = fido2.Message;
                return true;

            case System.Security.Authentication.AuthenticationException authFailed:
                statusCode = StatusCodes.Status401Unauthorized;
                title = "Authentication failed";
                detail = string.IsNullOrWhiteSpace(authFailed.Message)
                    ? "Authentication failed."
                    : authFailed.Message;
                return true;

            case ValidationException validation:
                statusCode = StatusCodes.Status400BadRequest;
                title = "Validation error";
                detail = validation.Message;
                extensions = new Dictionary<string, object?>
                {
                    ["propertyName"] = validation.PropertyName,
                };
                return true;

            default:
                statusCode = default;
                title = string.Empty;
                detail = string.Empty;
                return false;
        }
    }
}
