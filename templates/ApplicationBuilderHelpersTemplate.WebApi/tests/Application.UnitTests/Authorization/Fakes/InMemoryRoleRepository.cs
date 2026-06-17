using System.Collections.Concurrent;
using Domain.Authorization.Interfaces;
using Domain.Authorization.Entities;

namespace Application.UnitTests.Authorization.Fakes;

/// <summary>
/// In-memory implementation of IRoleRepository for unit testing.
/// </summary>
internal sealed class InMemoryRoleRepository : IRoleRepository
{
    private readonly ConcurrentDictionary<Guid, Role> _roles = new();
    private readonly ConcurrentDictionary<string, Guid> _codeIndex = new(StringComparer.OrdinalIgnoreCase);

    public Task<Role?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(FindById(id));
    }

    public Task<Role?> GetByCodeAsync(string code, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(code))
        {
            return Task.FromResult<Role?>(null);
        }

        var normalized = code.Trim();
        if (_codeIndex.TryGetValue(normalized, out var roleId) && _roles.TryGetValue(roleId, out var role))
        {
            return Task.FromResult<Role?>(role);
        }

        return Task.FromResult<Role?>(null);
    }

    public Task<IReadOnlyCollection<Role>> ListAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var snapshot = _roles.Values
            .OrderBy(static role => role.Code, StringComparer.Ordinal)
            .ToArray();
        return Task.FromResult<IReadOnlyCollection<Role>>(snapshot);
    }

    public void Add(Role role)
    {
        ArgumentNullException.ThrowIfNull(role);
        _roles[role.Id] = role;
        _codeIndex[role.Code] = role.Id;
    }

    public void Update(Role role)
    {
        ArgumentNullException.ThrowIfNull(role);
        _roles[role.Id] = role;
        _codeIndex[role.Code] = role.Id;
    }

    public void Remove(Role role)
    {
        ArgumentNullException.ThrowIfNull(role);
        if (_roles.TryRemove(role.Id, out _) && !string.IsNullOrWhiteSpace(role.Code))
        {
            _codeIndex.TryRemove(role.Code, out _);
        }
    }

    public Task<IReadOnlyCollection<Role>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(ids);
        cancellationToken.ThrowIfCancellationRequested();

        var list = new List<Role>();
        foreach (var id in ids.Distinct())
        {
            if (_roles.TryGetValue(id, out var role))
            {
                list.Add(role);
            }
        }

        return Task.FromResult<IReadOnlyCollection<Role>>(list);
    }

    public Task<IReadOnlyCollection<Role>> GetByCodesAsync(IEnumerable<string> codes, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(codes);
        cancellationToken.ThrowIfCancellationRequested();

        var list = new List<Role>();
        foreach (var code in codes.Where(c => !string.IsNullOrWhiteSpace(c)).Select(c => c.Trim()).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (_codeIndex.TryGetValue(code, out var roleId) && _roles.TryGetValue(roleId, out var role))
            {
                list.Add(role);
            }
        }

        return Task.FromResult<IReadOnlyCollection<Role>>(list);
    }

    private Role? FindById(Guid id)
        => _roles.TryGetValue(id, out var role) ? role : null;
}
