# ADR 0012: Symmetric Help/Version Forgiveness (#595)

**Status**: Accepted (issue #595).

Supersedes nothing; extends ADR 0007b (`0007b-help-beats-missing-required.md`), ADR 0010 (`0010-group-unknown-invalid-beats-version.md`), and ADR 0011 (`0011-help-first-target-routing.md`) without editing them in place.

## Context

Help already skipped required validation, and version did the same on its own path, but the two forgiving paths lived in separate checks. Error footers also scoped the version hint to the failing command even though version only ever runs globally. The host error path had no help-request signal, so the same failure printed both hints from the host and a single hint from the parser.

## Decision

- **One pre-validation gate for both flags**: after parsing, `HelpVersionGateway.DecideValidationForgiveness` (`src/ApplicationBuilderHelpers/CommandLineParser/HelpVersionGateway.cs:80-87`) decides from the parse flags plus the raw tail. Either outcome runs before validation in `CommandLineParser` (`src/ApplicationBuilderHelpers/CommandLineParser/CommandLineParser.cs:74-85`) and skips all of it.
- **Help outranks version**: help forgiveness needs `ShowHelp` plus a requested help token; version forgiveness needs `ShowVersion`, no `ShowHelp`, a requested version token, and no help token. Parse errors (unknown, misuse, requires-subcommand, invalid-literal) still throw before either outcome.
- **Global-only version hint, route-relative help hint**: `CommandErrorFooter.VersionHint` (`src/ApplicationBuilderHelpers/Exceptions/CommandErrorFooter.cs:57-58`) ignores the command name and always renders the global form; `HelpHint` (`:52-55`) keeps the route-relative form.
- **Host threads the help signal like the parser**: `ApplicationHost.Run` (`src/ApplicationBuilderHelpers/ApplicationHost.cs:55`) passes `RequestedHelp` into the footer, matching the parser catch (`src/ApplicationBuilderHelpers/CommandLineParser/CommandLineParser.cs:114`). This narrows the ADR-0007b host carve-out for the help-requested case only.

## Consequences

- Current behavior lives in `docs/advanced.md#help-system` and `docs/advanced.md#error-footers`, then this ADR.
- Help or version with its flag present skips all validation and exits `0`; both present shows help; unknown, misuse, requires-subcommand, and invalid-literal inputs still exit `2`.
- Usage-error footers pair a route-relative help hint with a global version hint; a failing call that already requested help keeps only the version hint, from parser and host alike.
- Docs same pass: `docs/advanced.md` (footer block, forgiveness rules), `docs/api-reference.md` (footer scope, precedence, `CommandName` qualifier), `docs/commands.md` (footer-scope anchor).
- Pinned by `HelpVersionPrecedenceTests.cs` (help/version skip-required rows, help-over-version rows, unknown-beats-version rows) and `CommandErrorFooterTests.cs` (help-requested truth table).
