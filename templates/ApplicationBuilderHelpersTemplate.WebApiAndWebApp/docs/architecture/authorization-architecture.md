# Authorization Architecture

## Overview

The permission system uses **scope-based authorization** with hierarchical path matching. This document explains how permissions are evaluated and provides examples for common patterns.

## Permission Path Hierarchy

Permissions are organized in a tree structure using colon-separated paths:

```
api                          (root)
├── auth                     (auth namespace)
│   ├── me        [RLeaf]    (GET /auth/me - userId from JWT via [FromJwt])
│   ├── logout    [WLeaf]    (POST /auth/logout - userId from JWT via [FromJwt])
│   ├── refresh   [WLeaf]    (POST /auth/refresh - validates refresh token)
│   └── sessions             (sessions namespace - userId in URL path)
│       ├── list  [RLeaf]    (GET /auth/users/{userId}/sessions)
│       └── revoke [WLeaf]   (DELETE /auth/users/{userId}/sessions/{id})
└── users
    ├── list      [RLeaf]    (GET /users - admin only)
    ├── read      [RLeaf]    (GET /users/{id} - with userId)
    ├── update    [WLeaf]    (PUT /users/{id} - with userId)
    └── delete    [WLeaf]    (DELETE /users/{id} - admin only)
```

### Permission Types

| Type | Description |
|------|-------------|
| `RLeaf` | Read leaf - terminal read permission |
| `WLeaf` | Write leaf - terminal write permission |
| Parent node | Container for child permissions |

## Scope Directive Syntax

Scopes are strings in the format:

```
action;path;param1=value1;param2=value2
```

| Component | Description |
|-----------|-------------|
| `action` | `allow` or `deny` |
| `path` | Permission path (e.g., `api:users:read`) |
| `param=value` | Required parameter bindings |

### Special Suffixes

| Suffix | Meaning |
|--------|---------|
| `:_read` | Grant **all** RLeaf permissions under this path |
| `:_write` | Grant **all** WLeaf permissions under this path |

## Path Matching Rules

The `ScopeEvaluator.PathMatches` method uses these rules:

### Rule 1: Exact Match (Leaf Permissions)
```
Directive: api:auth:logout
Requested: api:auth:logout
Result: ✅ MATCH
```

### Rule 2: Root `_read` / `_write` (Global Grants)
```
Directive: _read
Requested: api:users:read (any RLeaf)
Result: ✅ MATCH - grants all RLeaf permissions

Directive: _write
Requested: api:users:update (any WLeaf)
Result: ✅ MATCH - grants all WLeaf permissions
```

### Rule 3: Hierarchical Match
```
Directive: api:users
Requested: api:users:read
Result: ✅ MATCH - parent path grants child permissions
```

### Rule 4: Scoped `_read` / `_write` (Parent Wildcards)
```
Directive: api:accounts:_read
Requested: api:accounts:list
Result: ✅ MATCH - scoped _read grants children RLeaf

Directive: api:auth:_write
Requested: api:auth:sessions:revoke
Result: ✅ MATCH - scoped _write grants all WLeaf under api:auth
```

## ⚠️ Common Mistake: Scoped Suffix on Leaf Permissions

**WRONG:**
```csharp
ScopeTemplate.Allow("api:auth:logout:_write")  // WRONG!
```

This **does not** work because:
1. `logout` is already a WLeaf (terminal node)
2. There are no children under `logout` to grant
3. The `:_write` suffix looks for children matching `api:auth:logout:*`
4. The actual permission path is `api:auth:logout`, not `api:auth:logout:something`

**BETTER: Use `[FromJwt]` Pattern**

Instead of explicit grants for self-service endpoints, use the `[FromJwt]` pattern:

```csharp
// Controller - bind userId from JWT
[FromJwt(ClaimTypes.NameIdentifier), PermissionParameter(...)] Guid userId

// Role - userId-scoped grants cover it automatically
ScopeTemplate.Allow(Permissions.RootWriteIdentifier, ("userId", "{roleUserId}"))
```

This way, `logout` uses the userId-scoped `_write` grant, eliminating the need for special-case exact grants.

### Why This Matters

```
Directive: api:auth:logout:_write
Check: Does "api:auth:logout".StartsWith("api:auth:logout:")
Result: ❌ FALSE - equals the path, not a child of it

Directive: api:auth:logout
Check: Is "api:auth:logout" == "api:auth:logout"
Result: ✅ TRUE - exact match
```

## Role Configuration Examples

### User Role (Restricted Access)

```csharp
public static Role User { get; } = new()
{
    Id = UserRoleId,
    Name = "User",
    Description = "Standard user role with limited access to own resources",
    ScopeTemplates =
    [
        // Parameterized root grants - only for resources with userId
        ScopeTemplate.Allow(Permissions.RootReadIdentifier, ("userId", "{roleUserId}")),
        ScopeTemplate.Allow(Permissions.RootWriteIdentifier, ("userId", "{roleUserId}"))
    ]
};
```

### Admin Role (Full Access)

```csharp
public static Role Admin { get; } = new()
{
    Id = AdminRoleId,
    Name = "Admin",
    Description = "Administrator role with full system access",
    ScopeTemplates =
    [
        // Global root grants - all read/write permissions
        ScopeTemplate.Allow(Permissions.RootReadIdentifier),
        ScopeTemplate.Allow(Permissions.RootWriteIdentifier)
    ]
};
```

## Permission Evaluation Examples

### Example 1: User Accessing Own Sessions

**Request:** `GET /api/v1/auth/users/{user-a-id}/sessions` (User A's token)

**User A's Scopes:**
```
allow;_read;userId=user-a-id
allow;_write;userId=user-a-id
allow;api:auth:me
allow;api:auth:logout
```

**Required Permission:** `api:auth:sessions:list;userId=user-a-id`

**Evaluation:**
1. Check `_read;userId=user-a-id`:
   - Path `_read` is root read
   - Requested `api:auth:sessions:list` is RLeaf ✅
   - Parameters match: `userId=user-a-id` (from URL path) ✅
2. **Result: ALLOWED**

### Example 2: User Accessing Other User's Sessions

**Request:** `GET /api/v1/auth/users/{user-b-id}/sessions` (User A's token)

**User A's Scopes:** (same as above)

**Required Permission:** `api:auth:sessions:list;userId=user-b-id`

**Evaluation:**
1. Check `_read;userId=user-a-id`:
   - Path matches, but parameters don't match (`user-a-id` ≠ `user-b-id` from URL) ❌
2. Check `api:auth:me`: Path doesn't match ❌
3. Check `api:auth:logout`: Path doesn't match ❌
2. **Result: FORBIDDEN (403)**

### Example 3: User Calling Logout

**Request:** `POST /api/v1/auth/logout` (User A's token)

**Required Permission:** `api:auth:logout;userId=user-a-id` (userId from `[FromJwt]`)

**Evaluation:**
1. Check `_write;userId=user-a-id`:
   - Path `_write` is root write
   - Requested `api:auth:logout` is WLeaf ✅
   - Parameters match: `userId=user-a-id` ✅
2. **Result: ALLOWED**

> **Note:** The logout endpoint uses `[FromJwt(ClaimTypes.NameIdentifier)]` to bind the userId from the JWT, allowing the userId-scoped `_write` grant to apply.

### Example 4: Admin Accessing Any User's Data

**Request:** `GET /api/v1/users/any-user-id` (Admin token)

**Admin's Scopes:**
```
allow;_read
allow;_write
```

**Required Permission:** `api:users:read;userId=any-user-id`

**Evaluation:**
1. Check `_read` (no parameters):
   - Root `_read` with no params matches ALL RLeaf permissions
   - Requested `api:users:read` is RLeaf ✅
2. **Result: ALLOWED**

## Directive Priority

When multiple directives match, the most specific one wins:

1. **Exact path + exact params** (highest priority)
2. **Exact path + root params**
3. **Parent path + exact params**
4. **Parent path + root params**
5. **Scoped wildcards**
6. **Root wildcards** (lowest priority)

**Deny directives** at the same level always override allow directives.

## The `[FromJwt]` Pattern

The `[FromJwt]` attribute allows binding method parameters directly from JWT claims. When combined with `[PermissionParameter]`, this enables self-service endpoints to use userId-scoped permissions.

### Usage

```csharp
[HttpGet("me")]
[RequiredPermission(PermissionIds.Api.Auth.Me.Id)]
public async Task<IActionResult> GetMe(
    [FromJwt(ClaimTypes.NameIdentifier), PermissionParameter(PermissionIds.Api.Auth.Me.UserIdParameter)] Guid userId)
{
    // userId is extracted from JWT's 'sub' claim
    // Permission check: api:auth:me;userId={jwt-userId}
}
```

### How It Works

1. `[FromJwt(ClaimTypes.NameIdentifier)]` - Custom model binder extracts the `sub` claim from the JWT
2. `[PermissionParameter(...)]` - Includes the bound value in permission checking
3. The permission check becomes `api:auth:me;userId=<actual-jwt-sub>`
4. User's `_read;userId={roleUserId}` grant matches because the userId parameter value matches

### Benefits

- **No explicit grants needed** for self-service endpoints like `/me` and `/logout`
- **Consistent pattern** with other userId-scoped endpoints
- **Automatic user isolation** - users can only access their own data
- **Cleaner role definitions** - fewer special-case grants

### Files

- `FromJwtAttribute.cs` - The attribute that specifies which claim to extract
- `FromJwtModelBinder.cs` - The model binder that extracts and converts claim values
- `FromJwtModelBinderProvider.cs` - Registered in MVC options to enable the binder

## Summary Table

| Scenario | Directive | Result |
|----------|-----------|--------|
| Access own sessions | `_read;userId={roleUserId}` | ✅ Works |
| Access other's sessions | `_read;userId={roleUserId}` | ❌ Denied |
| Call `/auth/me` | `_read;userId={roleUserId}` + `[FromJwt]` | ✅ Works (userId from JWT) |
| Call `/auth/logout` | `_write;userId={roleUserId}` + `[FromJwt]` | ✅ Works (userId from JWT) |
| Admin access anything | `_read` (no params) | ✅ Works |

## Inline Role Parameters in JWT

Role claims use an inline parameter format where role codes and parameters are combined in a single string:

### Format

```
{CODE};{param1}={value1};{param2}={value2}
```

### Example Token Claims

```json
{
  "role": [
    "USER;roleUserId=550e8400-e29b-41d4-a716-446655440000",
    "MODERATOR;orgId=org123"
  ],
  "scope": [
    "allow;api:custom:endpoint"
  ]
}
```

### Key Characteristics

1. **Role parameters are inline** - No separate `role_params:` claims
2. **Scopes contain only direct grants** - Role-derived scopes are NOT in the token
3. **Runtime resolution** - Role definitions are fetched from database and expanded at permission check time
4. **Immediate effect** - Changes to role definitions take effect without re-login

### Resolution Flow

1. Extract role claims from JWT (e.g., `USER;roleUserId=abc123`)
2. Parse each claim using `Role.TryParseRoleClaim()` → code + parameters
3. Fetch role definitions from database by code
4. Expand `ScopeTemplate` placeholders with parameter values
5. Combine with direct `scope` claims from token
6. Evaluate permission against all expanded directives

### Benefits

- **Smaller tokens** - Role scopes not duplicated in token
- **Dynamic permissions** - Role changes apply immediately
- **Cleaner token structure** - Single format for role claims
