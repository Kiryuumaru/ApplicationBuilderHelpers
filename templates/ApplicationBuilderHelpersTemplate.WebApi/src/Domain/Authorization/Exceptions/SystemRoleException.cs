using Domain.Shared.Exceptions;

namespace Domain.Authorization.Exceptions;

/// <summary>
/// Exception thrown when an operation is attempted on a system or static role that is not allowed.
/// </summary>
public sealed class SystemRoleException : DomainException
{
    public string? RoleCode { get; }
    public Guid? RoleId { get; }

    public SystemRoleException(string message)
        : base(message)
    {
    }

    public SystemRoleException(string message, string roleCode)
        : base(message)
    {
        RoleCode = roleCode;
    }

    public SystemRoleException(string message, Guid roleId)
        : base(message)
    {
        RoleId = roleId;
    }

    public SystemRoleException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
