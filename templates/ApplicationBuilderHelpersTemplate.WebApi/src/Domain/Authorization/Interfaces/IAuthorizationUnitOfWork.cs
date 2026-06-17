using Domain.Shared.Interfaces;

namespace Domain.Authorization.Interfaces;

/// <summary>
/// Unit of work for Authorization feature operations.
/// Defines the atomicity boundary for role persistence.
/// </summary>
public interface IAuthorizationUnitOfWork : IUnitOfWork;
