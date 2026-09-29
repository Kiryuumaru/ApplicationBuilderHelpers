# ADR 0011: Help-First Target Routing (#591)

**Status**: Accepted (issue #591).

Supersedes nothing; extends ADR-0007 (`0007-abstract-root-requires-subcommand.md`) and ADR-0009 (`0009-concrete-root-leading-help-first.md`) without editing them in place.

## Context

ADR-0007 pinned help-first for the abstract root: a leading bare `--help`/`-h` set `ShowHelp` and returned the root target, so `["--help", "greet"]` rendered global help. ADR-0009 widened the same shape to the concrete root via `IsConcreteRootLeadingHelp`, so `["--help", "test"]` and `["--help", "false"]` rendered global help. Both branches ignored the trailing token, so a valid subcommand name never reached its own help.

## Decision

- **Route a valid trailing subcommand to its help**: both help-first branches (abstract at `src/ApplicationBuilderHelpers/CommandLineParser/ArgumentParser.cs:60-69`, concrete at `:85-94`) resolve the target through `ResolveHelpTargetCommand` (`:759-782`). It skips leading help tokens, walks `FindChild` over dash-free pre-`--` tokens, and returns the deepest matched child; bare help or a dash-led next token keeps the current target; a bogus bare word returns `null` so the branch falls through to normal error handling.
- **Bogus bare words error, flags still globalize**: `--help <valid-sub>` renders target help (exit `0`); `--help <bogus-word>` falls through and errors (concrete root: `Unexpected argument`, exit `2`); `--help --<flag>` keeps the current target and renders its help (exit `0`). Version still beats help; the `--` sentinel still blocks the gate.

## Consequences

- `["--help", "greet"]` on an abstract root renders `greet` help (exit `0`); `["--help", "test"]` on a concrete root renders `test` help (exit `0`, no `COMMANDS:` list); `["--help", "false"]` on a concrete root errors `Unexpected argument 'false'` (exit `2`); `["--help", "--bogus"]` on either root renders root help (exit `0`).
- ADR-0007 and ADR-0009 stay frozen; their `["--help", "<name>"]` rows now read through this ADR.
- Docs same pass: `docs/advanced.md` (bare-root rows, concrete-root matrix, carve-out, exit/tokenizer lines), `docs/commands.md` (Term `null` row, tokenizer/exit lines).
 - Pinned by `AbstractRootRequiresSubcommandTests.Help_First_With_Command_Name_Shows_Target_Help` (target assertions) + `RootRoutingDivergenceTests.ConcreteRoot_Help_WithTrailingCommandName_ShowsTargetHelp` (target assertions).
