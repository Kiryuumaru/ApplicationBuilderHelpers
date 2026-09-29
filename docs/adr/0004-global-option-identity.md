# ADR-0004: Global-Option Identity (Shared Root-Owned, Define-Once Reference-Everywhere)

**Status**: Accepted (per Vitruvius decision B; per-run lifetime; OwnerCommand/BindTarget split and precedence pinned below).

## Context

Global-option handling fans out across the parser pipeline instead of living in one place. The promotion decision lives in `CommandHierarchyBuilder.DetermineGlobalOptions` (`src/ApplicationBuilderHelpers/CommandLineParser/CommandHierarchyBuilder.cs:223-279`), the built-in `--help` global lives in `AddBuiltInGlobalOptions` (`:403-440`), the per-run initializer comparison lives in `InitializerValuesEqual` (`:296-311`), and copies are frozen in `CreateGlobalOptionCopy` (`:544-563`). Each command exposes its effective scope through the `AllOptions` ref-dedup view (`src/ApplicationBuilderHelpers/CommandLineParser/SubCommandInfo.cs:72-98`), which walks `Options` plus `IsGlobal`/`IsInherited` parent options and dedups by reference (`HashSet<SubCommandOptionInfo>`). The merge sites read that view: parse (`ArgumentParser.cs:92`), bind (`ValueBinder.cs:25-28,34-40`), validate (`ParameterValidator.cs:18`), completion (`CompletionEngine.cs:48,56,64,74`), execute (`CommandExecutor.cs:288`), and help (`HelpFormatter.cs:163`), with help default-value fallback at `HelpContentProvider.cs:446-464`.

Lifetime is per-run only: each `BuildCommandHierarchy` resolves a fresh per-run instance so type-registered commands cannot leak bound values across runs, while caller-supplied instance registrations keep identity (`CommandHierarchyBuilder.cs:32-66`, materialized at `:41-63`).

The observed behavior being pinned (per Hopper/Parker, caller-provided): single identity, last-wins scalar, `Ordinal` (case-sensitive) keys, tightened promotion gate (`EnvironmentVariable` / initializer / `IsCaseSensitive` / `Description`), with `DuplicateOptionTests` revised to match.

## Decision

Adopt decision B — shared root-owned identity via the `AllOptions` ref-dedup view:

- Promotion stays where it is (`DetermineGlobalOptions`, `CommandHierarchyBuilder.cs:223-279`): an option present on **all** concrete commands with an identical signature is marked `IsGlobal`/`IsInherited` and one frozen copy is added to `RootCommand.Options` (`:268-273`).
- The promotion gate is tightened — all of: `PropertyType`, `IsRequired`, `IsSecret`, `ShortName`, `LongName`, `EnvironmentVariable` (`StringComparison.Ordinal`), `IsCaseSensitive`, `Description` (`StringComparison.Ordinal`), live initializer values (`InitializerValuesEqual`, `:296-311`), and `ValidValues` element-wise (`ArraysEqual`, `:387-398`) must match (`:248-258`). Any divergence stays local.
- Copies are frozen snapshots: `CreateGlobalOptionCopy` (`:544-563`) copies `Property`/`PropertyType`/names/`Description`/`IsRequired`/`EnvironmentVariable`/`IsCaseSensitive`/`IsSecret`/`DefaultValue`, defensively copies `ValidValues`, sets `IsGlobal`/`IsInherited`, and preserves `OwnerCommand` at the definition site — the caller sets `BindTarget` to the scope holding the copy (root at `:271`, per-command help copies at `:431`).
- `OwnerCommand` re-homes to the definition site; `BindTarget` records the copy-holding scope. Definition-site nodes set both to the owner (`SubCommandOptionInfo.cs:126-127,157-158`); the split is documented on the properties (`SubCommandOptionInfo.cs:99-106`). Help default-value lookup reads definition-site first, then the copy-holding scope, then the declaring-type-holder fallback (`HelpContentProvider.cs:446-484`) — definition-site-first coincides with the legacy first-scan-hit for identical globals, so step 1 is behavior-preserving.
- Keys are case-sensitive throughout: canonical-key dedup/merge uses `StringComparison.Ordinal`/`StringComparer.Ordinal` (`ParseResult.cs:23,26,29-38,45-70`; key def at `:77-78`; sole identity reader at `:110-135`) and `StringComparer.Ordinal` grouping (`ValueBinder.cs:34-36`, identity via resolver at `:38-40`, display fallback via `GetCanonicalOptionKey` at `:102-103`); long/short token matching is `Ordinal` (`SubCommandOptionInfo.cs:329,337-348,360-381,387`, `FindNoValueBase` at `:323-330`).
- Precedence is pinned: scalar repeats are last-wins — `AddOptionValue` evicts prior canonical-key entries before appending (`ParseResult.cs:45-70`); collections accumulate and bind from the merged list (`ValueBinder.cs:41,47-63`); merged values decide CLI-wins via `TryGetMergedOptionValues` (`ParseResult.cs:85-95`, sole value reader); identity comes from the sole identity reader `TryGetCanonicalIdentityOption` (`ParseResult.cs:110-135`); environment-variable fallback applies only when no merged CLI value exists and the env value is not null-or-whitespace (`EnvVarFallback.cs:29-35`), applied per `AllOptions` scope before binding (`ValueBinder.cs:25-28`).

## Options Considered

- **B — shared root-owned identity via `AllOptions` ref-dedup view (chosen)**: one logical option, many scope-local references; matches the existing `AllOptions` consumers without a merge choke point.
- **C — centralize merge choke / throwaway-only (rejected)**: routing every merge through one choke, or treating globals as throwaway copies, was rejected per Vitruvius — it discards the definition-site identity `OwnerCommand`/`BindTarget` preserves and adds a coupling point the merge sites do not need.

## Industry Precedent

Per Curie (caller-provided): System.CommandLine recursive option propagation, Spectre.Console.Cli inherited settings, and Click context-passed shared options all treat a global as one logical definition visible in every scope; argparse `parents=` (copy-into-each-parser) is the copy outlier this ADR does not follow.

## Consequences

- Define once, reference everywhere within a run: identical-on-every-command options share one logical identity surfaced per scope through `AllOptions`; divergent signatures stay local and never merge.
- No cross-run leakage: sharing is within-run only (`CommandHierarchyBuilder.cs:32-66`); the next `RunAsync` rebuilds fresh per-run nodes.
- Precedence contract: last-wins scalar, accumulate multi, explicit CLI beats env fallback, fallback only when absent.
- Matching stays case-sensitive (`Ordinal`); help default display resolves definition-site first (`HelpContentProvider.cs:446-464`).
- Contract pinned by the revised `DuplicateOptionTests` (caller-provided; not re-run under docs-only restraint).
- #470 exception: satisfied-required bare repeat fails `MissingRequired` — see ADR-0005.
- #593 strict-mode carve-out (opt-in): `ICommandBuilder.RejectDuplicateOptions` (`src/ApplicationBuilderHelpers/Interfaces/ICommandBuilder.cs:30-31`, default off at `src/ApplicationBuilderHelpers/ApplicationBuilder.cs:26`) rejects valued non-collection repeats — two or more explicit CLI valued occurrences under `SetRejectDuplicateOptions` (`src/ApplicationBuilderHelpers/Extensions/ICommandBuilderExtensions.cs:174-181`) fail `DuplicateOption` (exit 2). Counts live in `ParseResult.ValuedOccurrenceCounts` (`src/ApplicationBuilderHelpers/CommandLineParser/ParseResult.cs:26`), checked in `ParameterValidator.CollectDuplicateErrors` (`:87-106`).

## Hardening #482 (behavior-preserving)

- `TryGetCanonicalIdentityOption` (`ParseResult.cs:110-135`) is the sole identity reader for one logical (canonical-key) group: P1 returns the first `TargetCommand.AllOptions` node in walk order whose canonical key matches (the target command's own copy, which sorts before inherited globals); P2 falls back to the first `OptionValues` key in encounter order, preserving prior `GroupBy`/`SelectMany` behavior. It returns an existing node and never synthesizes one; `OwnerCommand`/`BindTarget` are carried read-only. `TryGetMergedOptionValues` (`:85-95`) stays the sole value reader. `GetCanonicalOptionKey` (`:77-78`) is unchanged, as is the `BareOptionOccurrences` ledger (`:23,60,68-69`).
- This accessor is not the rejected option-C centralize/throwaway choke above: it preserves the definition-site identity `OwnerCommand`/`BindTarget` carries instead of discarding it, and merges values from all copy identities rather than treating copies as throwaway.
- Merged-list ordering contract (pinned in the accessor doc): encounter order preserved, scalars last-wins via `AddOptionValue` eviction unchanged, no re-sort.

## #487 follow-up (rebased onto origin/master, master-wins)

- Promotion gate re-key kept from #487: `ReadInitializerValue` (`CommandHierarchyBuilder.cs:321-345`) reads off the registration holder keyed by the option property's declaring type, fail-closed via the `InitializerValuesUnreadable` sentinel when the holder is unknown, ambiguous, missing, or unreadable; `FindHolder` (`:368-382`) returns null on no match or multiple matches. `OwnerCommand`/`BindTarget` split restored verbatim from master (definition-site first).
- Help default-value layer kept from #487: `GetOptionDefaultValue` (`HelpContentProvider.cs:446`) keeps master's OwnerCommand-first then BindTarget-second chain verbatim and layers only a declaring-type-holder fallback (snapshot-first, live only when `!IsInstanceRegistration`) replacing the trailing legacy `_allCommands` scan; the `_allCommands` field stays stored and the local `FindHolder` (`:511`) keeps its concrete-command-type key with the divergence from the gate documented on it.
- Definition-site nodes set both `OwnerCommand` and `BindTarget` to the owner (`SubCommandOptionInfo.cs:126-127,157-158`); the split stays documented on the properties (`:99-106`).
