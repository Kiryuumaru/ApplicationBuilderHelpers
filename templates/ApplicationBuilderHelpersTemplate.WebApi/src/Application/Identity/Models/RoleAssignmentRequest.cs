namespace Application.Identity.Models;

public sealed record RoleAssignmentRequest(
    string RoleCode,
    IReadOnlyDictionary<string, string?>? ParameterValues = null);
