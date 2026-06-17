using Application.Client.Identity.Models;

namespace Application.Client.Identity.Interfaces.Inbound;

/// <summary>
/// Provides access to the current authentication state.
/// Initialization is guaranteed by RunPreparationAsync before the app serves requests.
/// </summary>
public interface IAuthStateProvider
{
    /// <summary>
    /// Gets the current authentication state.
    /// </summary>
    AuthState CurrentState { get; }

    /// <summary>
    /// Event raised when authentication state changes.
    /// </summary>
    event Action? OnStateChanged;

    /// <summary>
    /// Updates the authentication state with new credentials.
    /// </summary>
    Task UpdateStateAsync(StoredCredentials credentials);

    /// <summary>
    /// Clears the current authentication state (logout).
    /// </summary>
    Task ClearStateAsync();
}
