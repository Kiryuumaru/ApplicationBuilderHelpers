using Domain.Shared.Exceptions;

namespace Domain.Authorization.Exceptions;

public sealed class PermissionDeniedException : DomainException
{
    public string PermissionIdentifier { get; }

    public PermissionDeniedException(string permissionIdentifier)
        : base($"User lacks permission '{permissionIdentifier}'.")
    {
        if (string.IsNullOrWhiteSpace(permissionIdentifier))
        {
            throw new ArgumentException("Permission identifier cannot be null or whitespace.", nameof(permissionIdentifier));
        }

        PermissionIdentifier = permissionIdentifier;
    }

    public PermissionDeniedException(string permissionIdentifier, Exception innerException)
        : base($"User lacks permission '{permissionIdentifier}'.", innerException)
    {
        if (string.IsNullOrWhiteSpace(permissionIdentifier))
        {
            throw new ArgumentException("Permission identifier cannot be null or whitespace.", nameof(permissionIdentifier));
        }

        PermissionIdentifier = permissionIdentifier;
    }
}
