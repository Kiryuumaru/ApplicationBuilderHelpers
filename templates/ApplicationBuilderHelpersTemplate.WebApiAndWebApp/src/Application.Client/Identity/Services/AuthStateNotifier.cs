using Application.Client.Identity.Models;

namespace Application.Client.Identity.Services;

internal sealed class AuthStateNotifier
{
    private AuthState _currentState = AuthState.Anonymous;

    public AuthState CurrentState => _currentState;

    public event Action? OnStateChanged;

    public void SetState(AuthState state)
    {
        _currentState = state;
        OnStateChanged?.Invoke();
    }

    public void ClearState()
    {
        _currentState = AuthState.Anonymous;
        OnStateChanged?.Invoke();
    }
}
