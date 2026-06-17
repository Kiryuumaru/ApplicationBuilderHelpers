# User Management API

**Status:** ✅ Complete

## Overview

User management endpoints for admin and self-service operations. Role-based access control (RBAC) with permission scopes.

## Endpoints

| Method | Endpoint | Auth | Admin | Description |
|--------|----------|------|-------|-------------|
| GET | `/api/v1/iam/users` | ✅ | ✅ | List all users |
| GET | `/api/v1/iam/users/{id}` | ✅ | - | Get user by ID |
| PUT | `/api/v1/iam/users/{id}` | ✅ | - | Update user |
| DELETE | `/api/v1/iam/users/{id}` | ✅ | ✅ | Delete user |
| POST | `/api/v1/iam/users/{id}/roles` | ✅ | ✅ | Assign role |
| DELETE | `/api/v1/iam/users/{id}/roles/{roleId}` | ✅ | ✅ | Remove role |
| GET | `/api/v1/iam/users/{id}/permissions` | ✅ | - | Get effective permissions |

## Request/Response Examples

### List Users (Admin)

**Request:**
```
GET /api/v1/iam/users
Authorization: Bearer {admin-token}
```

**Response:**
```json
{
  "users": [
    {
      "id": "user-uuid-1",
      "username": "admin",
      "email": "admin@example.com",
      "roles": ["admin", "user"],
      "createdAt": "2025-01-01T00:00:00Z"
    },
    {
      "id": "user-uuid-2",
      "username": "player1",
      "email": "player1@example.com",
      "roles": ["user"],
      "createdAt": "2025-12-20T00:00:00Z"
    }
  ],
  "total": 2
}
```

### Update User Profile

**Request:**
```json
PUT /api/v1/iam/users/{id}
{
  "email": "newemail@example.com"
}
```

Users can only update their own profile unless they have admin role.

### Assign Role (Admin)

**Request:**
```json
POST /api/v1/iam/users/{id}/roles
{
  "roleId": "admin"
}
```

### Get Effective Permissions

**Request:**
```
GET /api/v1/iam/users/{id}/permissions
```

**Response:**
```json
{
  "userId": "user-uuid",
  "permissions": [
    "users._read",
    "users._write",
    "auth.sessions._read",
    "auth.sessions._write"
  ]
}
```

## Role-Based Access Control

### Built-in Roles

| Role | Description |
|------|-------------|
| `admin` | Full system access |
| `user` | Standard user permissions |

### Permission Structure

Permissions use a hierarchical scope pattern:

```
resource._action
resource._action;param=value

Examples:
users._read                       # Read all users
users._read;userId={id}           # Read specific user
auth.sessions._write              # Manage sessions
```

## Error Responses

| Status | Scenario |
|--------|----------|
| 403 | List users as non-admin |
| 403 | Update other user's profile |
| 403 | Delete own account |
| 403 | Assign/remove roles as non-admin |
| 404 | User not found |

## Test Coverage

Tests are located in `tests/Presentation.WebApi.FunctionalTests/Iam/`:

- **UsersApiTests.cs** - User CRUD operations
- **RolesApiTests.cs** - Role management
- **PermissionsApiTests.cs** - Permission queries
- **PermissionAllowDenyTests.cs** - Allow/Deny permission grants
- **CustomRoleManagementTests.cs** - Custom role creation and management
- **IamSecurityTests.cs** - Security boundary tests

**Total: 55 IAM tests**
