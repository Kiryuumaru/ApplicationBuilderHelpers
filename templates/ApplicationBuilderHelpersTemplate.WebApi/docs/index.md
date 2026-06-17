# Application Builder Helpers Template - Documentation

## Feature Documentation

| Feature | Document | Status |
|---------|----------|--------|
| Authentication | [features/authentication.md](features/authentication.md) | ✅ Complete |
| User Management | [features/user-management.md](features/user-management.md) | ✅ Complete |

## Architecture Documentation

| Topic | Document |
|-------|----------|
| Authorization Architecture | [architecture/authorization-architecture.md](architecture/authorization-architecture.md) |
| Test Architecture | [architecture/test-architecture.md](architecture/test-architecture.md) |

## API Documentation

| API | Document |
|-----|----------|
| Authentication | [api/api-auth.md](api/api-auth.md) |
| IAM (Users, Roles, Permissions) | [api/api-iam.md](api/api-iam.md) |

## Quick Start

```bash
# Build the solution
dotnet build

# Run all tests
dotnet test

# Run WebApi
dotnet run --project src/Presentation.WebApi
```

## API Base URL

```
Development: http://localhost:5199
```

## Authentication

All authenticated endpoints require JWT bearer token:

```
Authorization: Bearer {access_token}
```

Supported token types:
- **Access Token** (`typ: at+jwt`) - Short-lived (60 min), full API access
- **Refresh Token** (`typ: rt+jwt`) - Long-lived (7 days), only for token refresh
- **API Key** (`typ: ak+jwt`) - User-managed, programmatic access for bots/scripts

Get tokens via:
- `POST /api/v1/auth/login` - Username/password login
- `POST /api/v1/auth/register` - Registration (supports anonymous)
- `POST /api/v1/auth/users/{userId}/api-keys` - Create API key

## Test Summary

| Project | Tests |
|---------|-------|
| Presentation.WebApi.FunctionalTests | 588 |
| **Total** | **588** |

## Recent Changes (January 2026)

### API Key Management (NEW)
- User-managed API keys for programmatic access (bots, scripts, CI/CD)
- JWTs with `typ: ak+jwt` that mirror user permissions
- Cannot refresh tokens or manage API keys (security restriction)
- Background cleanup worker for expired/revoked keys
- Endpoints: `GET/POST /auth/users/{userId}/api-keys`, `DELETE /auth/users/{userId}/api-keys/{id}`

### Token Generation Refactoring
- Moved token generation logic to Application layer via `IUserTokenService`
- `UserTokenResult` now includes `ExpiresInSeconds` property
- All auth controllers updated to use the new service

### Token Type System
- Three token types: Access (`at+jwt`), Refresh (`rt+jwt`), API Key (`ak+jwt`)
- Unified validation via `ITokenValidationService`
- Type-specific validation (session check, revocation check, endpoint restriction)

### Allow/Deny Permission Grants
- Added `GrantType` enum (`Allow`, `Deny`) for permission grants
- Direct permission grants (via IAM API) are baked into JWT tokens
- Role scope changes take effect immediately (resolved at runtime from DB)

### Inline Role Parameters in JWT
- Role claims now use inline parameter format: `USER;roleUserId=abc123`
- Removed separate `role_params:` claims from tokens
- Smaller token size, cleaner format

### `[FromJwt]` Attribute Pattern
- Binds method parameters directly from JWT claims (e.g., `ClaimTypes.NameIdentifier`)
- Used for self-service endpoints like `/auth/me` and `/auth/logout`
- Enables userId-scoped permission grants to apply automatically

See [Authorization Architecture](architecture/authorization-architecture.md) for details.
