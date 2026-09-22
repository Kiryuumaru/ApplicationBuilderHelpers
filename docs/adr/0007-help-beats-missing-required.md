# ADR-0007: Help Beats Missing Required (#509)

**Status**: Accepted.

## Context

Before #509, missing-required errors won over `--help` (`required-test mytarget --help` exited 2 with `Missing required option`), because `ParameterValidator.CollectRequiredErrors` ran unconditionally at Step 7 and the missing errors blocked help-with-values at Step 7b. Users asking for help on a partially-specified command got an error instead of help.

## Decision (per vitruvius)

- Gate the required-option and required-argument passes in `ParameterValidator.CollectRequiredErrors` on `!result.ShowHelp`; the optional-bare gate stays as-is. No signature change; no Step 6/7 reorder in `CommandLineParser.cs` (Step 7/7b comments updated only).
- Keep the binding probe (`ValueBinder.CollectBindingErrors` with `skipBareWhenHelpRequested: true`): under `--help`, missing is suppressed but binding errors still throw `InvalidValue` exit 2 — so `required-test mytarget --age abc --help` flips from missing-wins to invalid-wins (still exit 2).
- Footer circularity: `CommandErrorFooter.Resolve` gains a `showHelpRequested` signal (smallest seam: optional parameter, default `false` so existing callers/tests are untouched). When the failing invocation already requested help (detected in the `CommandException` catch via `HelpVersionGateway.RequestedHelp`, which mirrors the parser: per-token `IsHelpToken` plus mid-cluster `h`, scanning only up to the first bare `--`), the circular `--help` hint is suppressed and only the `--version` hint survives. The no-flag missing path keeps both hints. Carve-outs by design: the host path (`ApplicationHost.cs`) has no argv so it keeps both hints; the generic `Exception` path keeps the `--help`-only fault footer.
- Tests: `Help_Does_Not_Skip_Required_Validation` → `Help_Skips_Required_Validation` (exit 0 + USAGE) plus `Help_Skips_Required_Argument_Validation`, `Help_Skips_All_Required_Validation`, `Short_Help_Skips_Required_Validation` (`-h` variant), and `Version_Skips_Required_Validation` (Step 4b pin); `Missing_Required_With_Help_Beats_Conversion_Error` → `Conversion_Error_With_Help_Beats_Missing_Required` (invalid-wins); new footer truth-table rows for the `showHelpRequested` arm.

## Consequences

- `required-test mytarget --help` → exit 0 + help; `required-test mytarget --age abc --help` → exit 2 invalid-only with `--version`-only footer.
- Preserved: unknown-option/command, RequiresSubcommand, and duplicate still fail-fast exit 2 with `--help` (parse throws untouched in `ArgumentParser.cs`); #483 invalid+help no-missing still exit 2; valid+help exit 0; required validation with no help flag unchanged; version Step 4b unchanged; `--config --help` carve-out preserved.
