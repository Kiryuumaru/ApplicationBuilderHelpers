# ADR 0013: Unknown Beats Help Order-Invariance (#607)

**Status**: Accepted (issue #607).

Supersedes nothing; extends ADR-0009 (`0009-concrete-root-leading-help-first.md`), ADR-0011 (`0011-help-first-target-routing.md`), and ADR-0012 (`0012-symmetric-help-version-forgiveness.md`) without editing them in place.

## Context

ADR-0009 let a leading bare `--help`/`-h` on a concrete root render root help while ignoring trailing tokens, and ADR-0011 re-routed a valid trailing subcommand to its own help. Both help-first branches ran before any error scan, so `["--help", "--bogus"]` rendered help (exit `0`) on either root while the reverse order `["--bogus", "--help"]` already errored (exit `2`). The same asymmetry held for invalid flag literals (`--quiet=banana`) and reserved help/version misuse forms (`--help=x`, `--no-help`, `--version=x`): after help they were forgiven, before help they failed. ADR-0010 had already fixed the mirror asymmetry for version (`config --version --bogus` fails the same as `config --bogus --version`); help had no equivalent guard.

## Decision

- **One pre-scan gate before both help-first branches**: when the target is an abstract path (no implementation, has children) or `IsConcreteRootLeadingHelp` fires (root, `argIndex == 0`, leading `IsHelpToken`, no pre-`--` version token), run the existing misuse gate, then the existing invalid-literal gate, then the existing unknown-option gate over pre-`--` tokens only (`src/ApplicationBuilderHelpers/CommandLineParser/ArgumentParser.cs:47-55`; predicates at `:919-926`, `:928-1007`, `:549-578`, `:581-690`). The abstract help-first branch (`:57-76`) and the concrete help probe (`:98-107`) run only after the scan passes.
- **Order-invariant by construction**: all three scans range over the full pre-`--` tail (`Skip(argIndex).TakeWhile(t => t != "--")` / sentinel-bounded loop), never over positions relative to the help token — `["--help", "--bogus"]` fails the same as `["--bogus", "--help"]`, and likewise for invalid-literal and misuse forms in either order.
- **Sentinel still blocks, routing unchanged**: scans stop at `--`, and `IsConcreteRootLeadingHelp` / `ResolveHelpTargetCommand` (`:893-916`) stay sentinel-bounded, so `["--", "--help"]` stays a surplus-argument error and `["config", "--", "--help"]` stays `RequiresSubcommand`. A valid trailing subcommand still routes to its help (`["--help", "test"]` renders target help, exit `0`); a bogus bare word still returns `null` from target resolution and falls through to the normal error path.

## Consequences

- `["--help", "--bogus"]` and `["--bogus", "--help"]` report `Unknown option: --bogus` (exit `2`) on abstract and concrete roots alike; never help text. `["test", "mytarget", "--help=true"]` in either tail position reports `Invalid Boolean value 'true' for option '--help'` (exit `2`).
- `["--", "--help"]` stays exit `2` (surplus argument); `["config", "--", "--help"]` stays exit `2` (`'config' requires a subcommand`).
- `["--help", "<valid-sub>"]` still renders target help (exit `0`); pure leading help with a flag tail (`["--help", "--verbose"]`-shaped, no error token) still renders help (exit `0`); version still beats help via the unchanged version gate.
- ADR-0009 and ADR-0011 stay frozen; their `["--help", "--bogus"] renders root help` rows now read through this ADR.
- Docs same pass: none (behavior pin lives here plus test names).
- Pinned by `HelpUnknownOrderInvarianceTests.cs` (9 tests: concrete ×2, abstract-grouping ×2, leaf ×2, help-misuse ×1, sentinel ×2) plus renamed `AbstractRootRequiresSubcommandTests.Help_First_With_Unknown_Flag_Reports_Unknown_Option` and `RootRoutingDivergenceTests.ConcreteRoot_Help_WithTrailingUnknownOption_ReportsUnknownOption`.
