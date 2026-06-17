using System.Collections.Immutable;

namespace Domain.SourceGenerators.Authorization.Models;

internal sealed class PermissionTreeNode
{
    public PermissionTreeNode(
        PermissionNodeKind kind,
        PermissionAccess access,
        string identifier,
        string description,
        string? scopeLabel,
        ImmutableArray<string> localParameters,
        ImmutableArray<PermissionTreeNode> children)
    {
        Kind = kind;
        Access = access;
        Identifier = identifier;
        Description = description;
        ScopeLabel = scopeLabel;
        LocalParameters = localParameters;
        Children = children;
    }

    public PermissionNodeKind Kind { get; }
    public PermissionAccess Access { get; }
    public string Identifier { get; }
    public string Description { get; }
    public string? ScopeLabel { get; }
    public ImmutableArray<string> LocalParameters { get; }
    public ImmutableArray<PermissionTreeNode> Children { get; }
}
