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

## Versions

Versions are set by the owner. No released versions are recorded here yet.
