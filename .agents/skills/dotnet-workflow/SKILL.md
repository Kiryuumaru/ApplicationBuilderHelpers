---
name: dotnet-workflow
description: Use when building, testing, running the test harness, or doing pre-commit verification for this NUKE plus dotnet repo.
---

# Dotnet Workflow

Build, test, and verify this repo the same way every time. NEVER invent other build, publish, or run commands.

## 1. Commands

### NUKE (repo entry points)

`build.ps1` (Windows) and `build.sh` (Linux/macOS) bootstrap the NUKE build in `build/_build.csproj` (NukeBuildHelpers-based; `build/Build.cs` defines the targets). `init` and `clean` are NOT defined in `build/Build.cs` — they come from the NUKE/NukeBuildHelpers bootstrap itself. Consult `./build.sh --help` for the authoritative target list. Only these entry points are verified:

| Command | Purpose |
|---|---|
| `./build.sh init` (or `.\build.ps1 init`) | First-time setup, if listed by the bootstrap help |
| `./build.sh clean` (or `.\build.ps1 clean`) | Clean build artifacts (`bin`, `obj`, `.vs`), if listed by the bootstrap help |

First-time setup is init (if listed), then build:

1. `./build.sh --help` to confirm available targets
2. `./build.sh init` (if the bootstrap lists it)
3. `dotnet build`

Verified NUKE targets in `build/Build.cs`: `ApplicationBuilderHelpersTest` (builds + tests the CLI harness projects), `ApplicationBuilderHelpersBuild` (clean + build + pack the core library), `ApplicationBuilderHelpersPublish` (pushes packages on version-bump runs).

### .NET (daily use)

| Command | Purpose |
|---|---|
| `dotnet build` | Build the solution. MUST end 0 warnings, 0 errors. |
| `dotnet test` | Run ALL tests. MUST end 100% pass. |
| `dotnet test src/ApplicationBuilderHelpers.Test.Cli.UnitTest` | Run the xUnit unit suite. |

- MUST use `dotnet build` for builds and `dotnet test` for tests. No other build/test commands exist in this repo.
- MUST treat publishing as NUKE-owned (`ApplicationBuilderHelpersPublish` target) — no manual `dotnet pack`/`dotnet push` flow in this skill.
- MUST scope `dotnet test <project>` to the single unit-test project only for fast iteration. The gate is always the FULL `dotnet test`.
- NEVER reference `tests/NetConduit.UnitTests`, `tests/NetConduit.Transit.*`, `tests/NetConduit.Transport.*`, `tests/Domain.UnitTests`, `tests/Application.UnitTests`, `src/Presentation.*`, `sampleapp.exe`, or `--urls` flags — none of those paths exist here. They are stale leftovers from a different project template.

### Test Harness

The runnable CLI harness lives in `src/ApplicationBuilderHelpers.Test.Cli/` (manual verification app, not the regression gate).

- MUST run the harness with `dotnet run --project src/ApplicationBuilderHelpers.Test.Cli` using the exact directory name.
- MUST read any `README` in the harness directory first for required setup.
- NEVER invent a `src/Presentation.*` run target.

## 2. BUILD Phase

From the remote dev playbook, adapted to this repo:

- MUST run `dotnet build` after every unit of work. NEVER present code that has not been built.
- MUST require 0 errors and 0 warnings. Every warning is a potential bug — fix the root cause, NEVER suppress warnings instead.
- MUST check the project config with tools (target frameworks, language version, dependencies) rather than recalling from memory.
- MUST NOT proceed to TEST when BUILD is red. BUILD red means back to IMPLEMENT.

## 3. TEST Phase

- MUST run the full `dotnet test` before presenting. 100% pass is required — PASSED means actually green in the runner output, NEVER inferred from absence of error.
- MUST cover every change with tests: happy path, edge cases (empty inputs, nulls, boundary values), error cases (invalid inputs, failure scenarios), and integration with neighboring components where applicable.
- MUST apply repo-relevant real-world scenarios with judgment, not blindly: unknown command, missing/duplicate args, sub-command mismatch, missing DI registration, config `@ref:` mis-resolution, middleware ordering faults, help rendering under redirected console.
- MUST keep tests deterministic, isolated, fast (unit tests in milliseconds), readable, and automated.
- MUST NOT ask anyone else to test, run, or verify — running build and tests is this skill's job.
- MUST NOT say "this should work" — MUST know it works from runner evidence.

## 4. Test Categorization (xUnit)

This repo has no `HighMemory` / `TestCategories.cs` batching scheme — MUST NOT reference one.

- MUST leave normal fast tests uncategorized unless the repo adopts a category scheme.
- MUST run the full suite with plain `dotnet test`; scope to the single unit-test project only for fast iteration.
- If a category scheme is ever introduced, MUST discover category names dynamically from compiled test assemblies and MUST NOT hardcode category lists in build orchestration.

## 5. Debug Discipline

When something fails, MUST investigate properly:

1. **Gather** — Capture actual output: logs, stack traces, error messages, system state.
2. **Reproduce** — Create a minimal reproduction; for code bugs write a failing test and confirm it fails consistently.
3. **Root-cause** — Trace execution with debugging tools. MUST NOT fix symptoms or guess from reading code alone.
4. **Fix and verify** — Fix the root cause, confirm the failing test passes, confirm no regressions.

MUST use proper diagnostic tools (test runner output, debugger, traces, structured logs). MUST NOT debug with print/log statements alone.

## 6. Retry Discipline

- MUST undo the specific changes from a failed attempt before retrying. MUST NOT stack fix B on top of failed fix A. MUST NOT leave broken code in the tree.
- If the same approach fails 3 times after clean reverts: MUST stop, analyze the failure pattern with debugging tools, research alternatives, and record what was tried and why it failed before asking for help.
- Each retry MUST be a genuinely different approach, not a minor tweak.

## 7. Pre-Commit Verification

Before every commit, MUST pass this gate:

| Check | Command | Required result |
|---|---|---|
| Build | `dotnet build` | 0 warnings, 0 errors |
| Tests | `dotnet test` | 100% pass |

Manual review checklist before commit:

- MUST verify proper dependency direction (core library depends only on declared packages plus `System.*`; templates and harness consume the core public surface).
- MUST verify correct file placement in `src/` or `templates/`.
- MUST update documentation per the `docs-sync` skill when the change touches any public API, option, command/lifecycle behavior, or test list.
- MUST leave no `TODO`, `FIXME`, placeholder, or commented-out code.
- MUST commit only when the gate above is green. NEVER commit on red.
