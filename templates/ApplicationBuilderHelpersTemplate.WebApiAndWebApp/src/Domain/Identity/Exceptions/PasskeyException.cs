using Domain.Shared.Exceptions;

namespace Domain.Identity.Exceptions;

public sealed class PasskeyException : DomainException
{
    public Guid? CredentialId { get; }
    public Guid? ChallengeId { get; }

    public PasskeyException(string message)
        : base(message)
    {
    }

    public PasskeyException(string message, Guid? credentialId = null, Guid? challengeId = null)
        : base(message)
    {
        CredentialId = credentialId;
        ChallengeId = challengeId;
    }

    public PasskeyException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
