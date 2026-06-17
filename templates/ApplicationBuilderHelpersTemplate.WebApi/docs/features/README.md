# Feature Documentation

This directory contains detailed documentation for implemented features in this application template.

## Backend API Features

| Document | Description | Tests |
|----------|-------------|-------|
| [Authentication](authentication.md) | JWT auth, login, register, 2FA, passkeys, sessions, API keys, anonymous (guest) mode | 330+ |
| [User Management](user-management.md) | User CRUD, roles, permissions | 70+ |

## Architecture

| Document | Description |
|----------|-------------|
| [Authorization Architecture](../architecture/authorization-architecture.md) | Permission system, scope directives, [FromJwt] pattern |
| [Test Architecture](../architecture/test-architecture.md) | WebApi functional test setup |

## Test Summary

| Project | Tests |
|---------|-------|
| Domain.UnitTests | 46 |
| Application.UnitTests | 67 |
| Application.IntegrationTests | 20 |
| Presentation.WebApi.FunctionalTests | 455 |
| **Total** | **588** |
