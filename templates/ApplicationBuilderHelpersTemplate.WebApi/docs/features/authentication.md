# Authentication API

**Status:** ✅ Complete

## Overview

JWT-based authentication for the REST API. Supports login, registration, token refresh, password management, two-factor authentication (2FA), passkeys (WebAuthn), session management, API keys, and anonymous (guest) authentication.

## Token Types

The system uses three JWT token types, distinguished by the `typ` header:

| Token Type | JWT `typ` | Lifetime | Use Case |
|------------|-----------|----------|----------|
| Access | `at+jwt` | 60 minutes | Interactive API calls |
| Refresh | `rt+jwt` | 7 days | Obtain new access tokens |
| API Key | `ak+jwt` | Configurable (months/years/never) | Programmatic/bot access |

### Token Capabilities

| Permission | Access Token | Refresh Token | API Key |
|------------|--------------|---------------|---------|
| API endpoints | ✅ Yes | ❌ No (lacks permissions) | ✅ Yes |
| Refresh tokens | ❌ No (explicit deny) | ✅ Yes | ❌ No (explicit deny) |
| Manage API keys | ✅ Yes | ❌ No | ❌ No (explicit deny) |

## Anonymous Authentication (Guest Mode)

Zero-friction onboarding allowing users to start using the application immediately without signup. Users can later upgrade their anonymous account by linking credentials (password, OAuth, passkey).

### User Flow

```
User visits → POST /auth/register (empty body) → Anonymous account created
                                                        ↓
                                    User interacts with the application
                                                        ↓
                               Prompt: "Save progress?" → POST /auth/link/password
                                                        ↓
                                        Same userId, all data preserved
```

### Anonymous User Creation

The `POST /api/v1/auth/register` endpoint supports anonymous registration with an empty body:

**Anonymous Registration:**
```json
POST /api/v1/auth/register
{}
```

**Response (201 Created):**
```json
{
  "accessToken": "eyJhbGciOiJIUzI1...",
  "refreshToken": "eyJhbGciOiJIUzI1...",
  "tokenType": "Bearer",
  "expiresIn": 3600,
  "user": {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "username": null,
    "email": null,
    "isAnonymous": true,
    "roles": [
      "USER;roleUserId=550e8400-e29b-41d4-a716-446655440000"
    ],
    "permissions": [
      "allow;_read;userId=550e8400-e29b-41d4-a716-446655440000",
      "allow;_write;userId=550e8400-e29b-41d4-a716-446655440000"
    ]
  }
}
```

### Registration Behavior Matrix

| Provided | Result |
|----------|--------|
| Nothing | Anonymous account (`isAnonymous: true`, `username: null`) |
| Email only | Anonymous with email linked |
| Username + Password | Full account (`isAnonymous: false`) |
| All fields | Full account with email |

### Background Cleanup

The `AnonymousUserCleanupWorker` background service automatically deletes abandoned anonymous accounts:
- **Criteria:** 30+ days inactive AND no data
- **Frequency:** Runs daily

## URL Structure for Admin Access

Most authenticated endpoints that operate on user-specific data include `{userId}` in the URL path rather than implicitly using the JWT's user ID. This design enables:

- **Regular users**: Can only access their own data (permission system validates `userId` matches JWT)
- **Admins**: Can access any user's data by specifying their `userId` in the URL

**Example:**
- `GET /api/v1/auth/users/{userId}/sessions` - Admin can view any user's sessions
- Regular users are restricted to their own `userId` by the permission system

**Exceptions:**
- `GET /api/v1/auth/me` - Always returns current user's info (from JWT)
- `POST /api/v1/auth/logout` - Always logs out current session (from JWT)

## Endpoints

### Authentication (anonymous)

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/v1/auth/login` | Authenticate with username/password |
| POST | `/api/v1/auth/login/2fa` | Complete login with 2FA code or recovery code |
| POST | `/api/v1/auth/login/passkey` | Authenticate with passkey (WebAuthn) |
| POST | `/api/v1/auth/login/passkey/options` | Get passkey login challenge |
| POST | `/api/v1/auth/register` | Create new user account |
| POST | `/api/v1/auth/refresh` | Refresh expired access token |
| POST | `/api/v1/auth/forgot-password` | Request password reset email |
| POST | `/api/v1/auth/reset-password` | Reset password with token |
| GET | `/api/v1/auth/external/providers` | List available OAuth providers |
| POST | `/api/v1/auth/external/{provider}` | Initiate OAuth login flow |
| POST | `/api/v1/auth/external/callback` | Process OAuth callback |

### Session Management (authenticated)

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/v1/auth/logout` | Invalidate current session |
| GET | `/api/v1/auth/users/{userId}/sessions` | List all active sessions |
| DELETE | `/api/v1/auth/users/{userId}/sessions/{id}` | Revoke specific session |
| DELETE | `/api/v1/auth/users/{userId}/sessions` | Revoke all sessions (logout everywhere) |

### Profile (authenticated)

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/v1/auth/me` | Get current user info |

### Two-Factor Authentication (authenticated)

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/v1/auth/users/{userId}/2fa/setup` | Get 2FA setup info (shared key, QR code URI) |
| POST | `/api/v1/auth/users/{userId}/2fa/enable` | Enable 2FA with verification code |
| POST | `/api/v1/auth/users/{userId}/2fa/disable` | Disable 2FA (requires password) |
| POST | `/api/v1/auth/users/{userId}/2fa/recovery-codes` | Generate new recovery codes |

### Identity Management (authenticated)

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/v1/auth/users/{userId}/identity` | Get all identity info (username, email, methods) |
| PUT | `/api/v1/auth/users/{userId}/identity/username` | Change username |
| POST | `/api/v1/auth/users/{userId}/identity/password` | Link password (OAuth-only users) |
| PUT | `/api/v1/auth/users/{userId}/identity/password` | Change password |
| POST | `/api/v1/auth/users/{userId}/identity/email` | Link email address |
| PUT | `/api/v1/auth/users/{userId}/identity/email` | Change email address |
| DELETE | `/api/v1/auth/users/{userId}/identity/email` | Unlink email address |
| GET | `/api/v1/auth/users/{userId}/identity/passkeys` | List registered passkeys |
| POST | `/api/v1/auth/users/{userId}/identity/passkeys` | Register new passkey |
| POST | `/api/v1/auth/users/{userId}/identity/passkeys/options` | Get passkey registration challenge |
| POST | `/api/v1/auth/users/{userId}/identity/passkeys/link` | Link passkey (upgrades anonymous users) |
| PUT | `/api/v1/auth/users/{userId}/identity/passkeys/{id}` | Rename passkey |
| DELETE | `/api/v1/auth/users/{userId}/identity/passkeys/{id}` | Delete passkey |
| GET | `/api/v1/auth/users/{userId}/identity/external` | List linked OAuth providers |
| DELETE | `/api/v1/auth/users/{userId}/identity/external/{provider}` | Unlink OAuth provider |

### API Keys (authenticated)

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/v1/auth/users/{userId}/api-keys` | List user's API keys (excludes revoked) |
| POST | `/api/v1/auth/users/{userId}/api-keys` | Create new API key (returns JWT once) |
| DELETE | `/api/v1/auth/users/{userId}/api-keys/{id}` | Revoke an API key |

## API Keys

API keys provide programmatic access for bots, scripts, and integrations without requiring interactive login sessions.

### Key Characteristics

- **JWT-based:** API keys are JWTs with `typ: ak+jwt` header
- **Mirror permissions:** Same roles and scopes as the user (resolved at runtime)
- **Restrictions:** Cannot refresh tokens or manage API keys
- **Revocable:** Instant revocation via database flag
- **Optional expiration:** Can be set to never expire or have specific expiration date

### Create API Key

**Request:**
```json
POST /api/v1/auth/users/{userId}/api-keys
Authorization: Bearer {access_token}
{
  "name": "Trading Bot",
  "expiresAt": "2027-01-01T00:00:00Z"  // optional, null = never expires
}
```

**Response (201 Created):**
```json
{
  "id": "api-key-guid",
  "name": "Trading Bot",
  "key": "eyJhbGciOiJIUzI1...",  // ONLY SHOWN ONCE
  "createdAt": "2026-01-09T00:00:00Z",
  "expiresAt": "2027-01-01T00:00:00Z"
}
```

> ⚠️ **Important:** The `key` field is only returned at creation time. Store it securely - it cannot be retrieved again.

### List API Keys

**Request:**
```
GET /api/v1/auth/users/{userId}/api-keys
Authorization: Bearer {access_token}
```

**Response (200 OK):**
```json
{
  "items": [
    {
      "id": "api-key-guid",
      "name": "Trading Bot",
      "createdAt": "2026-01-09T00:00:00Z",
      "expiresAt": "2027-01-01T00:00:00Z",
      "lastUsedAt": "2026-01-09T12:00:00Z"
    }
  ]
}
```

### Revoke API Key

**Request:**
```
DELETE /api/v1/auth/users/{userId}/api-keys/{id}
Authorization: Bearer {access_token}
```

**Response:** `204 No Content`

### Using API Keys

Use API keys exactly like access tokens:

```
GET /api/v1/some-endpoint
Authorization: Bearer {api_key_jwt}
```

### API Key JWT Claims

```json
{
  "sub": "user-id",
  "name": "username",
  "jti": "api-key-id",           // Used for revocation lookup
  "iat": 1704844800,
  "exp": 1736380800,              // Optional
  "rbac_version": "2",
  "roles": ["USER;roleUserId=user-id"],
  "scope": [
    "deny;api:auth:refresh;userId=user-id",
    "deny;api:auth:api_keys:_read;userId=user-id",
    "deny;api:auth:api_keys:_write;userId=user-id"
  ]
}
```

### Background Cleanup

The `ApiKeyCleanupWorker` background service automatically deletes:
- **Expired keys:** Immediately after expiration (0 days retention)
- **Revoked keys:** After 30 days (for audit purposes)
- **Frequency:** Runs daily

## Token Structure

Authentication uses a permission-based token separation model where access tokens and refresh tokens have distinct permission scopes, enforced by the permission system (not just token type claims).

### Token Generation Architecture

Token generation is handled by `IUserTokenService` in the Application layer, ensuring clean separation from presentation concerns:

```csharp
public interface IUserTokenService
{
    Task<UserTokenResult> CreateSessionWithTokensAsync(
        string userId, string? username, Guid? correlationId,
        string deviceType, string deviceId, string deviceModel);
    
    Task<UserTokenResult> RotateTokensAsync(Guid sessionId, Guid? correlationId);
}
```

The `UserTokenResult` includes:
- `AccessToken` - JWT with user permissions
- `RefreshToken` - JWT with limited refresh scope
- `SessionId` - Unique session identifier
- `ExpiresInSeconds` - Token expiration time

### Access Token
- **Expiration:** 60 minutes
- **Contains:** User ID, username, session ID, roles
- **Scope:** Direct permission grants (if any) + `deny;api:auth:refresh;userId={userId}`
- **Use:** `Authorization: Bearer {accessToken}`
- **Can:** Access all API endpoints the user has permissions for (resolved via token roles + DB role templates + direct grants)
- **Cannot:** Be used to refresh tokens (blocked by deny directive)

### Refresh Token
- **Expiration:** 7 days
- **Contains:** User ID, username, session ID
- **Scope:** `allow;api:auth:refresh;userId={userId}` (only)
- **Use:** Send in request body to `/auth/refresh` endpoint
- **Can:** Refresh tokens
- **Cannot:** Access endpoints with `[RequiredPermission]` (e.g., `/auth/me`, `/auth/logout`, `/iam/*`)

### Token Separation Security Model

```
┌─────────────────────────────────────────────────────────────┐
│                    ACCESS TOKEN                             │
│  Roles: USER;roleUserId=...                                 │
│  Scope: ... direct grants ..., deny;api:auth:refresh        │
├─────────────────────────────────────────────────────────────┤
│  ✅ Can access /api/v1/accounts                            │
│  ✅ Can access /api/v1/auth/me                             │
│  ✅ Can access /api/v1/auth/logout                         │
│  ❌ Cannot refresh tokens (denied by scope)                │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│                    REFRESH TOKEN                            │
│  Scope: allow;api:auth:refresh;userId={userId}             │
├─────────────────────────────────────────────────────────────┤
│  ✅ Can refresh tokens at /api/v1/auth/refresh             │
│  ❌ Cannot access /api/v1/auth/me (permission required)     │
│  ❌ Cannot access /api/v1/auth/logout (permission required) │
│  ❌ Cannot access /api/v1/accounts (lacks permission)      │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│                    API KEY                                  │
│  Roles: USER;roleUserId=...                                 │
│  Scope: deny;api:auth:refresh, deny;api:auth:api_keys:*    │
├─────────────────────────────────────────────────────────────┤
│  ✅ Can access /api/v1/accounts                            │
│  ✅ Can access /api/v1/auth/me                             │
│  ❌ Cannot refresh tokens (denied by scope)                │
│  ❌ Cannot manage API keys (denied by scope)               │
└─────────────────────────────────────────────────────────────┘
```

**Key Points:**
- The `/auth/refresh` endpoint is `[AllowAnonymous]` and validates the token in the request body
- Access tokens cannot be used as refresh tokens because they have `deny;api:auth:refresh`
- Refresh tokens only have refresh permission; they cannot access endpoints that require other permissions
- API keys mirror user permissions except refresh and api-key management
- This model prevents stolen access tokens from being used to generate new tokens

## Request/Response Examples

### Login

**Request:**
```json
POST /api/v1/auth/login
{
  "username": "player1",
  "password": "SecurePassword123!"
}
```

**Response (200 OK):**
```json
{
  "accessToken": "eyJhbGciOiJIUzI1...",
  "refreshToken": "eyJhbGciOiJIUzI1...",
  "tokenType": "Bearer",
  "expiresIn": 3600,
  "user": {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "username": "player1",
    "email": "player1@example.com",
    "roles": ["USER;roleUserId=550e8400-e29b-41d4-a716-446655440000"],
    "permissions": ["allow;_read;userId=...", "allow;_write;userId=..."]
  }
}
```

### Register

**Request:**
```json
POST /api/v1/auth/register
{
  "username": "newplayer",
  "password": "SecurePassword123!",
  "email": "newplayer@example.com"
}
```

**Response (201 Created):** Same as login response

### Refresh Token

**Request:**
```json
POST /api/v1/auth/refresh
{
  "refreshToken": "eyJhbGciOiJIUzI1..."
}
```

**Response (200 OK):** New access and refresh tokens

### Change Password

**Request:**
```json
PUT /api/v1/auth/users/{userId}/identity/password
Authorization: Bearer eyJhbGciOiJIUzI1...
{
  "currentPassword": "OldPassword123!",
  "newPassword": "NewSecurePassword456!"
}
```

**Response (204 No Content):** Password changed successfully

**Error Responses:**
- 401: Current password incorrect
- 400: New password doesn't meet requirements

### Get Current User

**Request:**
```
GET /api/v1/auth/me
Authorization: Bearer eyJhbGciOiJIUzI1...
```

**Response (200 OK):**
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "username": "player1",
  "email": "player1@example.com",
  "roles": ["USER;roleUserId=..."],
  "permissions": ["allow;_read;userId=...", "allow;_write;userId=..."]
}
```

## Two-Factor Authentication (2FA)

### 2FA Setup

Get the shared key and authenticator URI for setting up 2FA.

**Request:**
```
GET /api/v1/auth/users/{userId}/2fa/setup
Authorization: Bearer eyJhbGciOiJIUzI1...
```

**Response (200 OK):**
```json
{
  "sharedKey": "JBSWY3DPEHPK3PXP",
  "formattedSharedKey": "jbsw y3dp ehpk 3pxp",
  "authenticatorUri": "otpauth://totp/AppName:user1?secret=JBSWY3DPEHPK3PXP&issuer=AppName&digits=6"
}
```

### Enable 2FA

Enable 2FA by verifying a TOTP code from the authenticator app.

**Request:**
```json
POST /api/v1/auth/users/{userId}/2fa/enable
Authorization: Bearer eyJhbGciOiJIUzI1...
{
  "verificationCode": "123456"
}
```

**Response (200 OK):**
```json
{
  "recoveryCodes": [
    "AAAA-BBBB",
    "CCCC-DDDD",
    "EEEE-FFFF",
    ...
  ]
}
```

### Login with 2FA

Complete login when 2FA is required. The regular login endpoint returns 202 Accepted when 2FA is required.

**Regular Login Response (202 Accepted) when 2FA required:**
```json
{
  "requiresTwoFactor": true,
  "userId": "550e8400-e29b-41d4-a716-446655440000",
  "message": "Two-factor authentication required. Please provide the verification code."
}
```

**2FA Login Request:**
```json
POST /api/v1/auth/login/2fa
{
  "userId": "550e8400-e29b-41d4-a716-446655440000",
  "code": "123456"
}
```

**Response (200 OK):** Same as regular login response (JWT tokens + user info)

**Note:** The code can be either a 6-digit TOTP code or a recovery code. Recovery codes can only be used once.

## Error Responses

| Status | Scenario |
|--------|----------|
| 400 | Invalid request format, validation errors |
| 401 | Invalid credentials, expired token, revoked session |
| 403 | Account locked, insufficient permissions |
| 404 | Resource not found |
| 409 | Username/email already exists (register) |

## Test Coverage

Tests are located in `tests/Presentation.WebApi.FunctionalTests/Auth/`:

| Test File | Tests | Description |
|-----------|-------|-------------|
| LoginApiTests.cs | 16 | Login with credentials, 2FA, error handling |
| RegisterApiTests.cs | 13 | Registration, anonymous, duplicate handling |
| TokenApiTests.cs | 19 | Token refresh, rotation, validation |
| TokenExpirationTests.cs | 25 | Expiration, session revocation, theft detection |
| TokenSeparationTests.cs | 10 | Access/refresh token scope separation |
| TokenFormatTests.cs | - | JWT structure validation |
| RefreshTokenMisuseTests.cs | - | Refresh token abuse prevention |
| PasswordApiTests.cs | 20 | Password change, forgot/reset |
| PasskeyApiTests.cs | 21 | WebAuthn passkey flow |
| TwoFactorApiTests.cs | 19 | 2FA setup, enable, disable, login |
| SessionApiTests.cs | 10 | Session list, revoke |
| SessionSecurityTests.cs | 17 | Session isolation, security |
| IdentityApiTests.cs | - | Username, email, linking |
| OAuthApiTests.cs | 12 | OAuth providers, callback |
| ApiKeyApiTests.cs | - | API key CRUD, validation |
| AuthSecurityTests.cs | 22 | JWT attacks, injection, headers |

**Total: 330+ authentication tests** (includes [Theory] tests with multiple data sets)

## Password Requirements

- Minimum 6 characters
- At least one uppercase letter
- At least one lowercase letter
- At least one digit
- At least one special character

## Security Features

1. **Session-based tokens:** Each login creates a tracked session with unique ID
2. **Session validation on every request:** Access tokens validate session state on each authenticated request - revoked sessions are immediately rejected
3. **API key revocation:** API keys validated against database on each request
4. **Refresh token rotation:** Each refresh returns a NEW refresh token, old token is invalidated
5. **Token theft detection:** Reusing old refresh token revokes entire session
6. **Timing attack resistance:** Consistent response times for valid/invalid users
7. **No user enumeration:** Same response for existing/non-existing accounts
8. **Security headers:** X-Content-Type-Options, X-Frame-Options, etc.
9. **CRLF/injection protection:** Request sanitization
10. **Challenge replay prevention:** Passkey challenges single-use
