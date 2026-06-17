using System.Collections.Generic;

namespace Application.Server.Identity.Models;

public sealed record RoleAssignmentRequest(
    string RoleCode,
    IReadOnlyDictionary<string, string?>? ParameterValues = null);
