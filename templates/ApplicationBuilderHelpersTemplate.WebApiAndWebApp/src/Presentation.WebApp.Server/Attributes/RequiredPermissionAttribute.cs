using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Application.Server.Authorization.Interfaces.Inbound;
using Domain.Authorization.Exceptions;
using Domain.Authorization.Constants;
using Domain.Authorization.Models;
using Domain.Shared.Constants;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TokenClaimTypes = Domain.Identity.Constants.TokenClaimTypes;

namespace Presentation.WebApp.Server.Attributes;

/// <summary>
/// Enforces that a request principal possesses a specific permission before the decorated controller or action executes.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public sealed partial class RequiredPermissionAttribute : Attribute, IAsyncActionFilter
{
    private static readonly Lazy<IReadOnlyDictionary<string, Permission>> PermissionLookup = new(BuildPermissionLookup, true);

    /// <summary>
    /// Initializes a new instance of the <see cref="RequiredPermissionAttribute"/> class for the supplied permission identifier.
    /// </summary>
    /// <param name="permissionIdentifier">Canonical permission identifier to validate for the current user.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="permissionIdentifier"/> is null or whitespace.</exception>
    public RequiredPermissionAttribute(string permissionIdentifier)
    {
        if (string.IsNullOrWhiteSpace(permissionIdentifier))
        {
            throw new ArgumentException("Permission identifier cannot be null or whitespace.", nameof(permissionIdentifier));
        }

        PermissionIdentifier = permissionIdentifier.Trim();
    }

    /// <summary>
    /// Gets the canonical permission identifier that must be held by the current principal.
    /// </summary>
    public string PermissionIdentifier { get; }

    /// <inheritdoc />
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        var services = context.HttpContext.RequestServices;
        var logger = services.GetService<ILogger<RequiredPermissionAttribute>>();
        var permissionService = services.GetService<IPermissionService>();

        if (permissionService is null)
        {
            logger?.LogError("IPermissionService is not registered in the current request scope. Permission check cannot proceed.");
            throw new InvalidOperationException("IPermissionService is not registered in the current request scope. Permission check cannot proceed.");
        }

        var user = context.HttpContext.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            context.Result = new ChallengeResult();
            return;
        }

        var requiredIdentifier = ResolveRequiredIdentifier(context, logger);

        if (!await permissionService.HasPermissionAsync(user, requiredIdentifier, context.HttpContext.RequestAborted))
        {
            logger?.LogWarning(
                "Permission requirement failed for {ActionName}. User lacks permission {Permission}.",
                context.ActionDescriptor.DisplayName,
                requiredIdentifier);

            throw new PermissionDeniedException(requiredIdentifier);
        }

        await next();
    }

    private string ResolveRequiredIdentifier(ActionExecutingContext context, ILogger? logger)
    {
        // First, resolve any placeholders in the permission identifier
        var resolvedIdentifier = ResolvePlaceholders(PermissionIdentifier, context, logger);
        
        Permission.ParsedIdentifier parsedIdentifier;

        try
        {
            parsedIdentifier = Permission.ParseIdentifier(resolvedIdentifier);
        }
        catch (FormatException ex)
        {
            throw new InvalidOperationException($"Permission identifier '{resolvedIdentifier}' is not valid.", ex);
        }

        if (!PermissionLookup.Value.TryGetValue(parsedIdentifier.Canonical, out var permission))
        {
            throw new InvalidOperationException($"Permission '{resolvedIdentifier}' is not recognized in the canonical permission tree.");
        }

        var scopedValues = CollectParameterValues(context, permission, logger);
        
        // Merge in resolved placeholder values (they should already be in the identifier as parameters)
        // But we need to include them in scopedValues for consistency with permission checking
        foreach (var param in parsedIdentifier.Parameters)
        {
            if (!scopedValues.ContainsKey(param.Key))
            {
                scopedValues = new Dictionary<string, string?>(scopedValues, StringComparer.Ordinal)
                {
                    [param.Key] = param.Value
                };
            }
        }
        
        if (scopedValues.Count == 0)
        {
            return permission.Path;
        }

        // Build permission identifier in semicolon format: path;key=value;key2=value2
        // This is the format expected by HasPermission, which parses with Permission.ParseIdentifier
        var paramSegments = scopedValues
            .Where(kv => kv.Value is not null)
            .OrderBy(kv => kv.Key, StringComparer.Ordinal)
            .Select(kv => $"{kv.Key}={kv.Value}");

        return $"{permission.Path};{string.Join(';', paramSegments)}";
    }

    /// <summary>
    /// Resolves placeholders like [userId] in the permission identifier from JWT claims.
    /// </summary>
    private static string ResolvePlaceholders(string permissionIdentifier, ActionExecutingContext context, ILogger? logger)
    {
        // Pattern matches [placeholderName]
        var placeholderPattern = PlaceholderRegex();
        
        return placeholderPattern.Replace(permissionIdentifier, match =>
        {
            var placeholderName = match.Groups[1].Value;
            var resolvedValue = ResolvePlaceholderValue(placeholderName, context, logger);
            
            if (string.IsNullOrEmpty(resolvedValue))
            {
                logger?.LogWarning(
                    "Could not resolve placeholder '[{PlaceholderName}]' in permission identifier '{PermissionIdentifier}' for action {ActionName}.",
                    placeholderName,
                    permissionIdentifier,
                    context.ActionDescriptor.DisplayName);
                
                // Return the original placeholder if we can't resolve it
                return match.Value;
            }
            
            return resolvedValue;
        });
    }

    /// <summary>
    /// Resolves a single placeholder value from various sources.
    /// </summary>
    private static string? ResolvePlaceholderValue(string placeholderName, ActionExecutingContext context, ILogger? logger)
    {
        // Handle special known placeholders
        switch (placeholderName.ToLowerInvariant())
        {
            case "userid":
                var userId = context.HttpContext.User.FindFirst(TokenClaimTypes.Subject)?.Value;
                return userId;
        }
        
        // Try to resolve from action arguments (for non-special placeholders)
        if (context.ActionArguments.TryGetValue(placeholderName, out var argValue) && argValue is not null)
        {
            return Convert.ToString(argValue, CultureInfo.InvariantCulture);
        }
        
        // Try to resolve from route data
        if (context.RouteData.Values.TryGetValue(placeholderName, out var routeValue) && routeValue is not null)
        {
            return Convert.ToString(routeValue, CultureInfo.InvariantCulture);
        }
        
        return null;
    }

    [GeneratedRegex(@"\[([a-zA-Z_][a-zA-Z0-9_]*)\]")]
    private static partial Regex PlaceholderRegex();

    private static IReadOnlyDictionary<string, string?> CollectParameterValues(ActionExecutingContext context, Permission permission, ILogger? logger)
    {
        // Root _read and _write permissions accept any parameters for scoping
        var isRootScopePermission = permission.Identifier is "_read" or "_write" && permission.Parent is null;

        var relevantParameters = new HashSet<string>(permission.GetParameterHierarchy(), StringComparer.Ordinal);
        if (relevantParameters.Count == 0 && !isRootScopePermission)
        {
            return EmptyCollections.StringNullableStringDictionary;
        }

        var resolvedValues = new Dictionary<string, string?>(StringComparer.Ordinal);

        foreach (var descriptor in context.ActionDescriptor.Parameters.OfType<ControllerParameterDescriptor>())
        {
            var attribute = descriptor.ParameterInfo.GetCustomAttribute<PermissionParameterAttribute>(true);
            if (attribute is null)
            {
                continue;
            }

            // For root scope permissions, allow any parameter; otherwise validate against hierarchy
            if (!isRootScopePermission && !relevantParameters.Contains(attribute.Name))
            {
                throw new InvalidOperationException($"Parameter '{descriptor.ParameterInfo.Name}' maps to permission parameter '{attribute.Name}', which is not applicable to permission '{permission.Path}'.");
            }

            if (!context.ActionArguments.TryGetValue(descriptor.Name, out var argumentValue) || argumentValue is null)
            {
                if (!context.RouteData.Values.TryGetValue(descriptor.Name, out argumentValue) || argumentValue is null)
                {
                    logger?.LogDebug(
                        "Permission parameter '{ParameterName}' was not present in action arguments for {ActionName}.",
                        descriptor.ParameterInfo.Name,
                        context.ActionDescriptor.DisplayName);
                    continue;
                }
            }

            var stringValue = Convert.ToString(argumentValue, CultureInfo.InvariantCulture);
            if (string.IsNullOrWhiteSpace(stringValue))
            {
                logger?.LogDebug(
                    "Permission parameter '{ParameterName}' evaluated to an empty value in {ActionName}.",
                    descriptor.ParameterInfo.Name,
                    context.ActionDescriptor.DisplayName);
                continue;
            }

            resolvedValues[attribute.Name] = stringValue;
        }

        if (resolvedValues.Count == 0)
        {
            return EmptyCollections.StringNullableStringDictionary;
        }

        return resolvedValues;
    }

    private static IReadOnlyDictionary<string, Permission> BuildPermissionLookup()
    {
        var permissions = Permissions.GetAll();
        var lookup = new Dictionary<string, Permission>(permissions.Count, StringComparer.Ordinal);

        foreach (var permission in permissions)
        {
            lookup[permission.Path] = permission;
        }

        return lookup;
    }

}
