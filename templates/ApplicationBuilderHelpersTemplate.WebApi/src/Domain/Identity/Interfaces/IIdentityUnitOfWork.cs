using Domain.Shared.Interfaces;

namespace Domain.Identity.Interfaces;

/// <summary>
/// Unit of work for Identity feature operations.
/// Defines the atomicity boundary for user, session, API key, and passkey persistence.
/// </summary>
public interface IIdentityUnitOfWork : IUnitOfWork;
