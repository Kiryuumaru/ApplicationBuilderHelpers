# Changelog

All notable changes to this project are documented here. This file follows Keep a Changelog headings.

## [Unreleased]

### Added

- Built-in `DirectoryInfo` parser: options and arguments typed `DirectoryInfo` now bind like `FileInfo` (no existence check) instead of failing with `Invalid format` (exit 2). The built-in parser count moves from 24 to 25. See `docs/custom-type-parsers.md`. (Fixes #735)

### Changed

- **BREAKING**: The command's dependency entry now runs **last** in the shared setup pipeline, so commands can override shared-module defaults (Fixes #727; major version bump). Shared modules run first; the command's own hooks run after. This flips several behaviors:
  - DI duplicate-registration winner: a command's `Add*` now beats a module's registration (previously the module won).
  - Configuration: for the same key, the command's `AddConfigurations` source now wins over a module's (previously the module won).
  - Setup steps 5–7 (`AddMiddlewares`, `AddMappings`, `RunPreparation`): the command's hook now runs after the modules' hooks (previously first).
  - Web-host command middleware added via `AddMiddlewares` moves from the outermost position to the innermost.
  - `IEnumerable<IService>` consumers now see the command's registration last instead of first.
  - When both a module and the command use `TryAdd*`, the module's registration now wins (previously the command's did).
  - Command-side `TryAdd*` never overrides a module registration; use plain `Add*` to override.
  - Migration: to replace a service the host consumes as an enumerable (`IHostedService`, `IConfigureOptions`, ...), the command-last position reorders rather than overrides — call `services.RemoveAll<T>()` before re-adding. See `docs/application-dependencies.md`.

- An explicitly supplied empty string (`""`) binds as `""`, never `null`. Migrate `== null` sentinels to `string.IsNullOrEmpty`. See `docs/commands.md` and `docs/adr/0001-empty-string-preserve.md`.

### Fixed

- External cancellation (Ctrl+C / SIGTERM / a canceled outer `CancellationToken`) with a command that does not honor the cancellation now exits `130` (previously `0`), so a canceled run no longer reads as success. Behavior now matches the documented external-cancellation contract (Fixes #737). See `docs/commands.md`.
- Registering a parent command after its sub-commands no longer drops the existing sub-command subtree: on a collision with a populated node, the incoming node adopts its children instead of replacing them, so every registration order keeps the documented routes and the `COMMANDS:` help section. Same-name childless registrations still last-win. (Fixes #731)
- Hook-registered host-logging settings now win over the default host-logging policy (Fixes #732). The policy — status-message suppression plus the `Microsoft.Hosting.Lifetime` warning filter — is applied before `BuilderPreparation`/`AddConfigurations`/`AddServices`, so hook overrides take effect; previously the policy was applied last and overrode them. Precedence flip only — host defaults without hooks are unchanged. See `docs/advanced.md#host-logging`.

## Versions

Versions are set by the owner. No released versions are recorded here yet.
