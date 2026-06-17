using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using Domain.Authorization.Enums;
using Domain.Authorization.Models;

namespace Domain.Authorization.Constants;

public static class Permissions
{
    /// <summary>
    /// Permission identifier for the platform-wide read scope (`_read`).
    /// </summary>
    public static string RootReadIdentifier { get; }

    /// <summary>
    /// Permission identifier for the platform-wide write scope (`_write`).
    /// </summary>
    public static string RootWriteIdentifier { get; }

    /// <summary>
    /// Principal API permission node containing all API-specific scopes and operations.
    /// </summary>
    public static Permission Api { get; }

#if DEBUG
    /// <summary>
    /// Debug-only Security Operations permission root used for internal authorization scenarios.
    /// </summary>
    public static Permission SecOpsDebug { get; }
#endif

    /// <summary>
    /// Root-level permission nodes exposed by the platform.
    /// </summary>
    public static IReadOnlyList<Permission> PermissionTreeRoots { get; }

    public static IReadOnlyCollection<Permission> GetAll() => AllPermissions;

    private static readonly Permission[] AllPermissions;
    private static readonly Permission RootReadPermission;
    private static readonly Permission RootWritePermission;

    static Permissions()
    {
        RootReadPermission = CreateRootScope("_read", "Read access to all platform operations.", PermissionAccessCategory.Read);
        RootWritePermission = CreateRootScope("_write", "Write access to all platform operations.", PermissionAccessCategory.Write);

        RootReadIdentifier = RootReadPermission.Path;
        RootWriteIdentifier = RootWritePermission.Path;

        var roots = BuildPermissionRoots();
        foreach (var root in roots)
        {
            root.SetParent(null);
        }

        PermissionTreeRoots = Array.AsReadOnly(roots);

        Api = PermissionTreeRoots.First(static permission => string.Equals(permission.Identifier, "api", StringComparison.Ordinal));

#if DEBUG
        SecOpsDebug = PermissionTreeRoots.First(static permission => string.Equals(permission.Identifier, "sec_ops_debug", StringComparison.Ordinal));
#endif

        AllPermissions = BuildAllPermissions(roots);
    }

    private static Permission[] BuildPermissionRoots()
    {
        var roots = new List<Permission>
        {
            RootReadPermission,
            RootWritePermission,
            BuildApiRoot()
        };

#if DEBUG
        roots.Add(BuildSecOpsDebugRoot());
#endif

        return [.. roots];
    }

    private static Permission BuildApiRoot()
    {
        return Node(
            "api",
            "Permissions related to API operations.",
            "API operations",

            // Auth API - Self-service authentication operations (all user-scoped)
            Node(
                "auth",
                "Self-service authentication and security operations.",
                "auth APIs",
                new[] { "userId" },

                // Token operations
                WLeaf("refresh", "Refresh access token using refresh token. Only granted to refresh tokens."),

                // Profile operations
                RLeaf("me", "Read own user profile information."),
                WLeaf("logout", "Invalidate own session."),

                // Sessions management
                Node(
                    "sessions",
                    "Manage own login sessions.",
                    "session operations",
                    RLeaf("list", "List own active sessions."),
                    WLeaf("revoke", "Revoke a specific session."),
                    WLeaf("revoke_all", "Revoke all sessions except current.")
                ),

                // API Keys management
                Node(
                    "api_keys",
                    "Manage own API keys for programmatic access.",
                    "API key operations",
                    RLeaf("list", "List own API keys."),
                    WLeaf("create", "Create a new API key."),
                    WLeaf("revoke", "Revoke an API key.")
                ),

                // Two-factor authentication
                Node(
                    "2fa",
                    "Manage two-factor authentication settings.",
                    "2FA operations",
                    RLeaf("setup", "Get 2FA setup information."),
                    WLeaf("enable", "Enable 2FA on account."),
                    WLeaf("disable", "Disable 2FA on account."),
                    WLeaf("regenerate_codes", "Regenerate recovery codes.")
                ),

                // Identity management - consolidated under /identity
                Node(
                    "identity",
                    "Manage own identity settings.",
                    "identity operations",
                    RLeaf("read", "Read own identity information."),

                    // Username
                    Node(
                        "username",
                        "Manage username.",
                        "username operations",
                        WLeaf("link", "Link a username to the account."),
                        WLeaf("change", "Change own username.")
                    ),

                    // Password
                    Node(
                        "password",
                        "Manage password.",
                        "password operations",
                        WLeaf("link", "Link a password to the account."),
                        WLeaf("change", "Change own password.")
                    ),

                    // Email
                    Node(
                        "email",
                        "Manage email.",
                        "email operations",
                        WLeaf("link", "Link an email to the account."),
                        WLeaf("change", "Change own email address."),
                        WLeaf("unlink", "Unlink email from the account.")
                    ),

                    // Passkeys (WebAuthn)
                    Node(
                        "passkeys",
                        "Manage own passkeys for passwordless login.",
                        "passkey operations",
                        RLeaf("list", "List own registered passkeys."),
                        WLeaf("register", "Register a new passkey."),
                        WLeaf("rename", "Rename a passkey."),
                        WLeaf("delete", "Delete a passkey.")
                    ),

                    // External logins (OAuth)
                    // Note: Linking happens via OAuth flow at POST /external/{provider} + callback (unauthenticated)
                    Node(
                        "external",
                        "Manage linked external login providers.",
                        "external login operations",
                        RLeaf("list", "List linked external providers."),
                        WLeaf("unlink", "Unlink an external provider.")
                    )
                )
            ),

            // IAM API - Identity and Access Management
            Node(
                "iam",
                "Identity and Access Management operations.",
                "IAM APIs",

                // Users - Admin operations for user management
                Node(
                    "users",
                    "User management operations.",
                    "user management",
                    new[] { "userId" },
                    RLeaf("list", "List all users in the system (admin only)."),
                    RLeaf("read", "Read a user's details."),
                    RLeaf("permissions", "Read a user's effective permissions."),
                    WLeaf("update", "Update a user's profile."),
                    WLeaf("delete", "Delete a user account (admin only)."),
                    WLeaf("reset_password", "Reset a user's password (admin only).")
                ),

                // Roles - Role management and assignment operations
                Node(
                    "roles",
                    "Role management and assignment operations.",
                    "role management",
                    RLeaf("list", "List all roles in the system (admin only)."),
                    RLeaf("read", "Read a role's details (admin only)."),
                    WLeaf("create", "Create a new custom role (admin only)."),
                    WLeaf("update", "Update a custom role (admin only)."),
                    WLeaf("delete", "Delete a custom role (admin only)."),
                    WLeaf("assign", "Assign a role to a user (admin only)."),
                    WLeaf("remove", "Remove a role from a user (admin only).")
                ),

                // Permissions - Direct permission grant operations
                Node(
                    "permissions",
                    "Direct permission grant operations.",
                    "permission management",
                    WLeaf("grant", "Grant a direct permission to a user (admin only)."),
                    WLeaf("revoke", "Revoke a direct permission from a user (admin only).")
                )
            )
        );
    }

    private static Permission[] BuildAllPermissions(IReadOnlyList<Permission> roots)
    {
        var flattened = new List<Permission>();

        foreach (var root in roots)
        {
            flattened.AddRange(root.Traverse());
        }

        var permissions = new Permission[flattened.Count + 2];
        permissions[0] = RootReadPermission;
        permissions[1] = RootWritePermission;
        flattened.CopyTo(permissions, 2);
        return permissions;
    }

#if DEBUG
    private static Permission BuildSecOpsDebugRoot() =>
        Node(
            "sec_ops_debug",
            "Debug-only security-operations permissions for advanced authorization testing.",
            "debug security operation APIs",
            new[] { "tenantId" },
            Node(
                "v1",
                "Debug security operations version 1.",
                "debug security operations v1 APIs",
                Node(
                    "forensics",
                    "Manage debug-only forensic datasets and transfers.",
                    "debug forensic dataset operations",
                    new[] { "datasetId", "region" },
                    RLeaf("list", "List forensic datasets for a tenant."),
                    RLeaf("metadata", "Read forensic dataset metadata."),
                    RLeaf("download", "Download forensic dataset artifacts."),
                    RLeaf("exists", "Check whether a forensic dataset exists."),
                    WLeaf("upload", "Upload forensic dataset artifacts for analysis.", ["hash"]),
                    WLeaf("delete", "Delete forensic datasets from storage."),
                    WLeaf("quarantine", "Quarantine a dataset for additional investigation.", ["reason"])
                ),
                Node(
                    "playbooks",
                    "Coordinate debug-only rapid response playbooks.",
                    "debug rapid response playbook operations",
                    RLeaf("list", "List available rapid response playbooks."),
                    RLeaf("read", "Read a rapid response playbook definition.", ["playbookId"]),
                    WLeaf("deploy", "Deploy a rapid response playbook.", ["playbookId", "deviceId", "appId"]),
                    WLeaf("cancel", "Cancel an in-flight rapid response playbook.", ["playbookId", "executionId"])
                ),
                Node(
                    "incident_workspace",
                    "Manage debug-only incident workspaces.",
                    "debug incident workspace operations",
                    new[] { "incidentId", "region" },
                    RLeaf("timeline", "Read incident timeline records."),
                    RLeaf("audit_log", "Review incident audit logs."),
                    WLeaf("containment", "Trigger incident containment actions.", ["deviceId", "approvalCode"]),
                    WLeaf("eradication", "Trigger incident eradication workflows.", ["deviceId", "toolset"]),
                    WLeaf("cleanup", "Complete incident cleanup tasks.")
                )
            )
        );
#endif

    private static Permission Leaf(
        string identifier,
        string description,
        PermissionAccessCategory accessCategory = PermissionAccessCategory.Unspecified,
        string[]? parameters = null) => new()
        {
            Identifier = identifier,
            Description = description,
            Permissions = [],
            Parameters = parameters is { Length: > 0 } values ? [.. values] : [],
            AccessCategory = accessCategory
        };

    private static Permission RLeaf(string identifier, string description, string[]? parameters = null) =>
        Leaf(identifier, description, PermissionAccessCategory.Read, parameters);

    private static Permission WLeaf(string identifier, string description, string[]? parameters = null) =>
        Leaf(identifier, description, PermissionAccessCategory.Write, parameters);

    private static Permission Scope(string identifier, string description, PermissionAccessCategory accessCategory) => new()
    {
        Identifier = identifier,
        Description = description,
        Permissions = [],
        Parameters = [],
        AccessCategory = accessCategory
    };

    private static Permission ReadScope(string scopeLabel) =>
        Scope("_read", $"Read access to {scopeLabel}.", PermissionAccessCategory.Read);

    private static Permission WriteScope(string scopeLabel) =>
        Scope("_write", $"Write access to {scopeLabel}.", PermissionAccessCategory.Write);

    private static Permission Node(
        string identifier,
        string description,
        string scopeLabel,
        params object[] childrenAndParameters)
    {
        var parameters = Array.Empty<string>();
        var children = new List<Permission>();

        foreach (var item in childrenAndParameters)
        {
            switch (item)
            {
                case Permission permission:
                    children.Add(permission);
                    break;
                case string[] parameterArray:
                    parameters = parameterArray;
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported node argument type '{item?.GetType().FullName}'.");
            }
        }

        var childPermissions = new Permission[children.Count + 2];
        childPermissions[0] = ReadScope(scopeLabel);
        childPermissions[1] = WriteScope(scopeLabel);

        if (children.Count > 0)
        {
            children.CopyTo(childPermissions, 2);
        }

        return new Permission
        {
            Identifier = identifier,
            Description = description,
            Permissions = childPermissions,
            Parameters = parameters.Length == 0 ? [] : parameters
        };
    }

    private static Permission CreateRootScope(string identifier, string description, PermissionAccessCategory category) => new()
    {
        Identifier = identifier,
        Description = description,
        Permissions = [],
        Parameters = [],
        AccessCategory = category
    };
}
