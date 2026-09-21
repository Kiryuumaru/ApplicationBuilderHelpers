# ADR-0004: Global-Option Identity (Shared Root-Owned, Define-Once Reference-Everywhere)

**Status**: Accepted (per Vitruvius decision B; per-run lifetime; OwnerCommand/BindTarget split and precedence pinned below).

## Context

Global-option handling fans out across the parser pipeline instead of living in one place. The promotion decision lives in `CommandHierarchyBuilder.DetermineGlobalOptions` (`src/ApplicationBuilderHelpers/CommandLineParser/CommandHierarchyBuilder.cs:218-286`), the built-in `--help` global lives in `AddBuiltInGlobalOptions` (`:346-387`), the per-run initializer comparison lives in `InitializerValuesEqual` (`:296-325`), and copies are frozen in `CreateGlobalOptionCopy` (`:414-434`). Each command exposes its effective scope through the `AllOptions` ref-dedup view (`src/ApplicationBuilderHelpers/CommandLineParser/SubCommandInfo.cs:64-88`), which walks `Options` plus `IsGlobal`/`IsInherited` parent options and dedups by reference (`HashSet<SubCommandOptionInfo>`). The merge sites read that view: parse (`ArgumentParser.cs:92`), bind (`ValueBinder.cs:25,32-35`), validate (`ParameterValidator.cs:18`), completion (`CompletionEngine.cs:48,56,64,74`), execute (`CommandExecutor.cs:288`), and help (`HelpFormatter.cs:163`), with help default-value fallback at `HelpFormatter.cs:767-778`.

Lifetime is per-run only: each `BuildCommandHierarchy` resolves a fresh per-run instance so type-registered commands cannot leak bound values across runs, while caller-supplied instance registrations keep identity (`CommandHierarchyBuilder.cs:42-45`, materialized at `:46-64`).

The observed behavior being pinned (per Hopper/Parker, caller-provided): single identity, last-wins scalar, `Ordinal` (case-sensitive) keys, tightened promotion gate (`EnvironmentVariable` / initializer / `IsCaseSensitive` / `Description`), with `DuplicateOptionTests` revised to match.

## Decision

Adopt decision B — shared root-owned identity via the `AllOptions` ref-dedup view:

- Promotion stays where it is (`DetermineGlobalOptions`, `CommandHierarchyBuilder.cs:240-281`): an option present on **all** concrete commands with an identical signature is marked `IsGlobal`/`IsInherited` and one frozen copy is added to `RootCommand.Options` (`:270-278`).
- The promotion gate is tightened — all of: `PropertyType`, `IsRequired`, `IsSecret`, `ShortName`, `LongName`, `EnvironmentVariable` (`StringComparison.Ordinal`), `IsCaseSensitive`, `Description` (`StringComparison.Ordinal`), live initializer values (`InitializerValuesEqual`, `:296-325`), and `ValidValues` element-wise (`ArraysEqual`, `:330-341`) must match (`:248-258`). Any divergence stays local.
- Copies are frozen snapshots: `CreateGlobalOptionCopy` (`:414-434`) copies `Property`/`PropertyType`/names/`Description`/`IsRequired`/`EnvironmentVariable`/`IsCaseSensitive`/`IsSecret`/`DefaultValue`, defensively copies `ValidValues`, sets `IsGlobal`/`IsInherited`, and preserves `OwnerCommand` at the definition site — the caller sets `BindTarget` to the scope holding the copy (root at `:277`, per-command help copies at `:378`).
- `OwnerCommand` re-homes to the definition site; `BindTarget` records the copy-holding scope. Definition-site nodes set both to the owner (`SubCommandOptionInfo.cs:135-136,172-173`); the split is documented on the properties (`SubCommandOptionInfo.cs:102,104-112`). Help default-value lookup reads definition-site first, then the copy-holding scope, then the legacy scan (`HelpFormatter.cs:767-778`) — definition-site-first coincides with the legacy first-scan-hit for identical globals, so step 1 is behavior-preserving.
- Keys are case-sensitive throughout: canonical-key dedup/merge uses `StringComparison.Ordinal` (`ParseResult.cs:28,61`) and `StringComparer.Ordinal` grouping (`ValueBinder.cs:32-35`); long/short token matching is `Ordinal` (`SubCommandOptionInfo.cs:369,380,397,406,415,427`, `FindNoValueBase` at `:359-360`).
- Precedence is pinned: scalar repeats are last-wins — `AddOptionValue` evicts prior canonical-key entries before appending (`ParseResult.cs:22-33`); collections accumulate and bind from the merged list (`ValueBinder.cs:43-54`); merged values decide CLI-wins via `TryGetMergedOptionValues` (`ParseResult.cs:55-65`); environment-variable fallback applies only when no merged CLI value exists and the env value is not null-or-whitespace (`EnvVarFallback.cs:29-35`), applied per `AllOptions` scope before binding (`ValueBinder.cs:25-28`).

## Options Considered

- **B — shared root-owned identity via `AllOptions` ref-dedup view (chosen)**: one logical option, many scope-local references; matches the existing `AllOptions` consumers without a merge choke point.
- **C — centralize merge choke / throwaway-only (rejected)**: routing every merge through one choke, or treating globals as throwaway copies, was rejected per Vitruvius — it discards the definition-site identity `OwnerCommand`/`BindTarget` preserves and adds a coupling point the merge sites do not need.

## Industry Precedent

Per Curie (caller-provided): System.CommandLine recursive option propagation, Spectre.Console.Cli inherited settings, and Click context-passed shared options all treat a global as one logical definition visible in every scope; argparse `parents=` (copy-into-each-parser) is the copy outlier this ADR does not follow.

## Consequences

- Define once, reference everywhere within a run: identical-on-every-command options share one logical identity surfaced per scope through `AllOptions`; divergent signatures stay local and never merge.
- No cross-run leakage: sharing is within-run only (`CommandHierarchyBuilder.cs:42-45`); the next `RunAsync` rebuilds fresh per-run nodes.
- Precedence contract: last-wins scalar, accumulate multi, explicit CLI beats env fallback, fallback only when absent.
- Matching stays case-sensitive (`Ordinal`); help default display resolves definition-site first (`HelpFormatter.cs:771-774`).
- Contract pinned by the revised `DuplicateOptionTests` (caller-provided; not re-run under docs-only restraint).
