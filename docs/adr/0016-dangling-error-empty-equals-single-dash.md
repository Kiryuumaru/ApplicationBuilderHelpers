# ADR 0016: Dangling-Valued Error Beats Help, Empty `=`-Form Carve-Out, Single-Dash Long Tokens (#636)

**Status**: Accepted (issue #636).

Supersedes the dangling-valued forgiveness rows of ADR-0012 (`0012-symmetric-help-version-forgiveness.md`); extends ADR-0001 (`0001-empty-string-preserve.md`) with the empty `=`-form carve-out and ADR-0013 (`0013-unknown-beats-help-order-invariance.md`) with the single-dash token rule, without editing them in place. ADR-0001, ADR-0012, and ADR-0013 stay frozen; their superseded rows now read through this record. Precedence: this record wins on all four behaviors below.

## Context

ADR-0012 let help forgive any `InvalidValue` beside a help token, so a dangling valued option (bare `--config` with no value, or any `=`-form) plus `--help` rendered help instead of failing. ADR-0001 preserved empty strings with a deferred strict mode; the strict gate has now landed for non-string `=`-forms only. Single-dash `-help`/`-version` had no pinned token rule, and the zero-subcommand global help footer was unpinned.

## Decision

- **Error beats help for dangling valued options** (one rule, two hooks in `DanglingValuedOptionPolicy`: eager `IsEmptyEqualsForm` probed at `ArgumentParser.cs` value-error catch, late `HasBareValuedOccurrence` called from `CommandLineParser.cs:75-76`): a bare valued occurrence beside `--help` clears `ShowHelp`, so the run exits `2` instead of rendering help; an empty `=`-form on a non-string option (`--count=`, `-c=`) surfaces its `InvalidValue` immediately, and a `--config=` string empty still binds and renders help. Collection-valued options with merged values stay exempt.
- **Abstract early return is intentional**: the late hook returns `false` when `!HasImplementation` because abstract paths validate nothing late — abstract misuse fires eagerly (unknown/flag-literal/empty-`=`/bare probes) and `RequiresSubcommand` owns the rest, so a trailing-bare sentinel would misfire on help-forward paths.
- **Version beats dangling is intentional**: the late hook clears only `ShowHelp`, never `ShowVersion`, so `--timeout= --version` and `--config --version` exit `0` with version (parker-pinned; `ForgiveVersion` at `CommandLineParser.cs:85` + `ArgumentParser.cs:290-295` catch). Version forgiveness skips only eager value errors; unknown/misuse errors still beat version (distinct from error-beats-help).
- **Tail forgiveness is pre-computed once per concrete parse** (`ArgumentParser.cs:210-212` `helpWins`/`versionWins`); per-throw `RequestedHelp(args)` scans stay on the `RunAsync`/`ApplicationHost` catch paths because they hint from full argv, not the parse tail, so they cannot share the flags.
- **`VersionHint` keeps its overload**: `Exceptions/CommandErrorFooter.cs:60` retains the unused `commandName` parameter for signature compatibility (no break); version hint stays global-only per ADR-0012.
- **Empty `=`-form carve-out on ADR-0001** (`SubCommandOptionInfo.cs:254`, `:263`, `:318-328`): an empty literal in `=`-form (`--opt=`, `-o=`) throws `InvalidValue` (exit `2`) for every non-string target. Only `string` and `string`-element collections still bind empty per ADR-0001. Bare `""` positional handling is unchanged.
- **Single-dash long tokens are full-token unknowns, exact-only** (shared `SingleDashPolicy.IsReservedWord`; `ArgumentParser.cs` cluster guards, `HelpVersionGateway.cs` cluster exclusions): `-help` and `-version` never split as clusters and never read as `-e`; `-helpful` still clusters normally. Each reports `Unknown option` (exit `2`) with a did-you-mean pointer drawn from option candidates plus the reserved `--help`/`--version` entries (`DidYouMean.cs:87-91`). Neither token sets the help/version request signal, so the error footer keeps both hints (`Exceptions/CommandErrorFooter.cs:30-46`).
- **Zero-subcommand global help has no command footer** (`HelpContentProvider.cs:135-137`): the `Run '<exe> <command> --help'` footer renders only when top-level commands exist; otherwise it is null.

## Consequences

- `--config --help` exits `2`, even with the option's environment variable set — a typed bare scalar claims ownership and env rescues only omitted options; collections with merged values stay exempt. `--timeout=notanumber --help` still renders help (exit `0`) because deferred conversion errors keep forgiveness — only empty `=`-forms throw eager. Valid `--timeout=60 --help` still renders help (exit `0`). ADR-0012 forgiveness rows now read through this record.
- `--count= --help` on an `int` option exits `2`; `--name= --help` on a `string` option still binds empty and renders help.
- `-help` exits `2` as `Unknown option: -help` with a `--help` pointer and both footer hints; `-e` inside `-help` is never reported as a cluster char. `-help`/`-version` matching is exact-only: `-helpful` still tokenizes as a cluster.
- Current behavior lives in `docs/advanced.md#help-system`, `docs/commands.md#how-typing-works`, and `docs/api-reference.md#exit-contract`, then this ADR.
