````markdown
# IAM API

> **Controllers:** `IamUsersController`, `IamRolesController`, `IamPermissionsController`  
> **Base Path:** `/api/v1/iam`  
> **Authentication:** Bearer Token (all endpoints require authentication)

## Overview

Identity and Access Management (IAM) endpoints for managing users, roles, and permissions. Most operations require admin-level permissions.

---

## User Management

### `GET /api/v1/iam/users`

Lists all users (admin only).

**Permission:** `api:iam:users:list`

**Response:** `200 OK`
```json
{
  "users": [
    {
      "id": "550e8400-e29b-41d4-a716-446655440000",
      "username": "admin",
      "email": "admin@example.com",
      "roles": ["ADMIN", "USER"],
      "createdAt": "2026-01-01T00:00:00Z"
    }
  ],
  "total": 1
}
```

**Errors:**
- `403 Forbidden` - Lacks `api:iam:users:list` permission

---

### `GET /api/v1/iam/users/{id}`

Gets a user by ID.

**Permission:** `api:iam:users:read;userId={id}`

**Response:** `200 OK`
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "username": "player1",
  "email": "player1@example.com",
  "roles": ["USER"],
  "createdAt": "2026-01-01T00:00:00Z"
}
```

**Errors:**
- `403 Forbidden` - Cannot access other user's data (regular users)
- `404 Not Found` - User not found

---

### `PUT /api/v1/iam/users/{id}`

Updates a user's profile.

**Permission:** `api:iam:users:update;userId={id}`

**Request Body:**
```json
{
  "email": "newemail@example.com",
  "phoneNumber": "+1234567890",
  "lockoutEnabled": true
}
```

**Response:** `200 OK` - Updated user object

**Errors:**
- `403 Forbidden` - Cannot update other user's profile
- `404 Not Found` - User not found

---

### `DELETE /api/v1/iam/users/{id}`

Deletes a user (admin only).

**Permission:** `api:iam:users:delete`

**Response:** `204 No Content`

**Errors:**
- `403 Forbidden` - Lacks delete permission
- `404 Not Found` - User not found

---

### `GET /api/v1/iam/users/{id}/permissions`

Gets the effective permissions for a user.

**Permission:** `api:iam:users:permissions;userId={id}`

**Response:** `200 OK`
```json
{
  "userId": "550e8400-e29b-41d4-a716-446655440000",
  "permissions": [
    "api:auth:me",
    "api:auth:logout",
    "api:iam:users:read;userId=550e8400-e29b-41d4-a716-446655440000"
  ]
}
```

**Errors:**
- `403 Forbidden` - Cannot view other user's permissions
- `404 Not Found` - User not found

---

### `PUT /api/v1/iam/users/{id}/password`

Resets a user's password (admin only).

**Permission:** `api:iam:users:reset-password`

**Request Body:**
```json
{
  "newPassword": "NewSecurePassword123!"
}
```

**Response:** `204 No Content`

**Errors:**
- `400 Bad Request` - Password doesn't meet requirements
- `404 Not Found` - User not found

---

## Role Management

### `GET /api/v1/iam/roles`

Lists all available roles (system and custom).

**Permission:** `api:iam:roles:list`

**Response:** `200 OK`
```json
{
  "roles": [
    {
      "id": "00000000-0000-0000-0000-000000000001",
      "code": "ADMIN",
      "name": "Administrator",
      "description": "Full system access",
      "isSystemRole": true,
      "parameters": [],
      "scopeTemplates": [
        { "type": "allow", "permissionPath": "_read", "parameters": null },
        { "type": "allow", "permissionPath": "_write", "parameters": null }
      ]
    },
    {
      "id": "00000000-0000-0000-0000-000000000002",
      "code": "USER",
      "name": "User",
      "description": "Standard user role",
      "isSystemRole": true,
      "parameters": ["roleUserId"],
      "scopeTemplates": [
        { "type": "allow", "permissionPath": "_read", "parameters": { "userId": "{roleUserId}" } },
        { "type": "allow", "permissionPath": "_write", "parameters": { "userId": "{roleUserId}" } }
      ]
    }
  ]
}
```

---

### `GET /api/v1/iam/roles/{roleId}`

Gets a specific role by ID.

**Permission:** `api:iam:roles:read`

**Response:** `200 OK` - Role object (same format as list item)

**Errors:**
- `404 Not Found` - Role not found

---

### `POST /api/v1/iam/roles`

Creates a new custom role.

**Permission:** `api:iam:roles:create`

**Request Body:**
```json
{
  "code": "MODERATOR",
  "name": "Moderator",
  "description": "Can moderate content",
  "scopeTemplates": [
    {
      "type": "allow",
      "permissionPath": "api:content:moderate",
      "parameters": { "orgId": "{roleOrgId}" }
    }
  ]
}
```

**Response:** `201 Created` - Created role object

**Errors:**
- `400 Bad Request` - Invalid scope template or validation error
- `409 Conflict` - Role code already exists or reserved

---

### `PUT /api/v1/iam/roles/{roleId}`

Updates an existing custom role.

**Permission:** `api:iam:roles:update`

**Request Body:**
```json
{
  "name": "Updated Name",
  "description": "Updated description",
  "scopeTemplates": [
    { "type": "allow", "permissionPath": "api:content:_read", "parameters": null }
  ]
}
```

**Response:** `200 OK` - Updated role object

**Errors:**
- `400 Bad Request` - Cannot modify system roles
- `404 Not Found` - Role not found

---

### `DELETE /api/v1/iam/roles/{roleId}`

Deletes a custom role.

**Permission:** `api:iam:roles:delete`

**Response:** `204 No Content`

**Errors:**
- `400 Bad Request` - Cannot delete system roles
- `404 Not Found` - Role not found

---

### `POST /api/v1/iam/roles/assign`

Assigns a role to a user.

**Permission:** `api:iam:roles:assign`

**Request Body:**
```json
{
  "userId": "550e8400-e29b-41d4-a716-446655440000",
  "roleCode": "MODERATOR",
  "parameterValues": {
    "roleOrgId": "org-123"
  }
}
```

**Response:** `204 No Content`

**Errors:**
- `400 Bad Request` - Missing required role parameters
- `404 Not Found` - User or role not found

---

### `POST /api/v1/iam/roles/remove`

Removes a role from a user.

**Permission:** `api:iam:roles:remove`

**Request Body:**
```json
{
  "userId": "550e8400-e29b-41d4-a716-446655440000",
  "roleId": "role-assignment-guid"
}
```

**Response:** `204 No Content`

**Errors:**
- `404 Not Found` - User or role assignment not found

---

## Permission Management

### `GET /api/v1/iam/permissions`

Lists all available permissions in the system (permission tree).

**Permission:** None (any authenticated user)

**Response:** `200 OK`
```json
{
  "permissions": [
    {
      "path": "api",
      "identifier": "api",
      "description": "API root",
      "parameters": [],
      "isRead": false,
      "isWrite": false,
      "children": [
        {
          "path": "api:auth",
          "identifier": "api:auth",
          "description": "Authentication",
          "parameters": [],
          "isRead": false,
          "isWrite": false,
          "children": [
            {
              "path": "api:auth:me",
              "identifier": "api:auth:me",
              "description": "Get current user",
              "parameters": ["userId"],
              "isRead": true,
              "isWrite": false,
              "children": null
            }
          ]
        }
      ]
    }
  ]
}
```

---

### `POST /api/v1/iam/permissions/grant`

Grants a direct permission to a user (admin only). Direct grants are baked into the user's JWT token.

**Permission:** `api:iam:permissions:grant`

**Request Body:**
```json
{
  "userId": "550e8400-e29b-41d4-a716-446655440000",
  "permissionIdentifier": "api:reports:_read",
  "isAllow": true,
  "description": "Granted for Q4 reporting"
}
```

**Notes:**
- `isAllow: true` creates an **allow** grant (user CAN access)
- `isAllow: false` creates a **deny** grant (user CANNOT access, overrides role permissions)
- Direct grants require re-login to take effect (baked into JWT)

**Response:** `204 No Content`

**Errors:**
- `404 Not Found` - User not found

---

### `POST /api/v1/iam/permissions/revoke`

Revokes a direct permission from a user (admin only).

**Permission:** `api:iam:permissions:revoke`

**Request Body:**
```json
{
  "userId": "550e8400-e29b-41d4-a716-446655440000",
  "permissionIdentifier": "api:reports:_read"
}
```

**Response:** `204 No Content`

**Errors:**
- `404 Not Found` - User not found or permission not granted

---

## Permission Model

### Grant Types

| Type | Effect |
|------|--------|
| **Allow** | Explicitly grants permission |
| **Deny** | Explicitly denies permission (overrides allow) |

### Scope Resolution

Permissions are resolved from multiple sources:

1. **Role-derived scopes** - Fetched from database at runtime based on assigned roles
2. **Direct grants** - Baked into JWT token at login time

Changes to role definitions take effect immediately (next permission check). Direct grants require re-login.

See [Authorization Architecture](../architecture/authorization-architecture.md) for detailed permission evaluation rules.

---

## Built-in Roles

| Role | Code | Description |
|------|------|-------------|
| Administrator | `ADMIN` | Full system access (`_read`, `_write` without params) |
| User | `USER` | Access to own resources only (`_read;userId={roleUserId}`, `_write;userId={roleUserId}`) |

---

## Test Coverage

Tests are located in `tests/Presentation.WebApi.FunctionalTests/Iam/`:

| Test File | Description |
|-----------|-------------|
| `UsersApiTests.cs` | User CRUD operations |
| `RolesApiTests.cs` | Role management |
| `PermissionsApiTests.cs` | Permission queries |
| `PermissionAllowDenyTests.cs` | Allow/Deny grant behavior |
| `CustomRoleManagementTests.cs` | Custom role CRUD |
| `IamSecurityTests.cs` | Security boundary tests |

**Total: 70+ IAM tests**

````
