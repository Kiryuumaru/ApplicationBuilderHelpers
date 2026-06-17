using Domain.Identity.Enums;

namespace Application.Identity.Models;

public sealed record ExternalUserRegistrationRequest(
    string Username,
    ExternalLoginProvider Provider,
    string ProviderSubject,
    string? ProviderEmail = null,
    string? ProviderDisplayName = null,
    string? Email = null,
    IReadOnlyCollection<string>? PermissionIdentifiers = null,
    IReadOnlyCollection<RoleAssignmentRequest>? RoleAssignments = null,
    bool AutoActivate = true);
