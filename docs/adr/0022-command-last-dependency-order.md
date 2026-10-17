# ADR 0022: Command-Last Dependency Order (#727)

**Status**: Accepted (issue #727).

Supersedes nothing; first record to pin the dependency pipeline order.

## Context

The #380-era design ran the command's dependency entry first; module-side
`TryAdd*` was the cooperation mechanism for command replacement. The docs
promised command override — `docs/application-dependencies.md:114`:
"So a command can replace a shared default by registering its own" — but the
promise was broken for modules using plain `Add*`: the module registered after
the command, and Microsoft DI last-registration-wins resolved the module's
registration. Maintainer decision on #727: "Commands will be last so it can
override."

## Decision

- **The command's dependency entry is appended last**: modules append in
  registration order, then the command — one ordering site,
  `src/ApplicationBuilderHelpers/CommandLineParser/CommandExecutor.cs:66-70`.
- **Position consequences**: plain `Add*` on the command overrides
  shared-module defaults; module-side `TryAdd*` is a soft default that yields
  to a command registration; command-side `TryAdd*` fills only types no module
  registered; within steps 2–7 the command's hooks run after the modules'
  hooks.
- **Steps 1 and 8 are unaffected by the ordering site**: `CommandPreparation`
  keeps its parser path, where the command runs after module prep
  (`src/ApplicationBuilderHelpers/CommandLineParser/CommandLineParser.cs:56-59,91`),
  and `RunPreparationAsync` runs all entries concurrently
  (`src/ApplicationBuilderHelpers/ApplicationHost.cs:47`).

## Consequences

- **BREAKING (major version bump)**: the DI duplicate-registration winner
  flips to the command (singleton/scoped/transient/keyed); same-key
  configuration source precedence flips to the command; steps 5–7 hook order
  flips; web-host command middleware moves from outermost to innermost;
  `IEnumerable` registrations list the command last; both-side `TryAdd*` flips
  to the module; command-side `TryAdd*` never overrides a module registration —
  use plain `Add*`.
- **Migration**: a service the host consumes as an enumerable
  (`IHostedService`, `IConfigureOptions`, ...) is reordered, not overridden —
  call `services.RemoveAll<T>()` before re-adding.
- Docs same pass: `CHANGELOG.md` (`**BREAKING**` entry),
  `docs/application-dependencies.md` (Setup Order + Practical Guidance),
  `README.md` (run-pipeline diagram).
- Current behavior lives in `docs/application-dependencies.md#setup-order` and
  `docs/application-dependencies.md#practical-guidance`, then this ADR.
- Pinned by `CommandDependencyOrderingTests` (3 tests) and
  `CommandServiceOverrideTests` (7 tests): 10 new tests, 8 fail if the command
  is re-prepended.
