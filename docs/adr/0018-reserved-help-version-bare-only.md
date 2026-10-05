# ADR 0018: Reserved Help/Version `=`-Forms Use Bare-Only Text (#655)

**Status**: Accepted (issue #655).

Extends ADR 0015 (`0015-bare-only-boolean-flags.md`) to reserved help/version words without editing it in place. Frozen ADRs 0013 (`0013-unknown-beats-help-order-invariance.md`) keeps its `=`-form row as history and reads through this record.

## Context

ADR 0015 unified every user-flag `=`-form onto `SecretRedaction.NoValueAcceptedMessage` (bare-only wording: `does not accept a value ... Use bare ...`), but explicitly left reserved help/version misuse forms on their own messages. Those forms (`--help=<anything>`, `-h=<anything>`, `-?=<anything>`, `/?=<anything>`, `--version=<anything>`, `-V=<anything>`) kept reporting `Invalid Boolean value '<literal>' for option '<token>'. Expected 'true', 'false', ...` — a self-contradicting affordance, since reserved words accept no boolean literal at all.

## Decision

- **Single error-semantics truth**: the six positive `=`-form misuse branches (`HelpMisuseError` / `VersionMisuseError` in `src/ApplicationBuilderHelpers/CommandLineParser/ArgumentParser.cs`) route through `SecretRedaction.NoValueAcceptedMessage` with `isFlag: true, isNegated: false`, token-preserving display (`--help`, `-h`, `-?`, `/?`, `--version`, `-V`), and no `--no-` hint. Exit 2 and `InvalidValue` kind are unchanged.
- **Negated/unflagged branches untouched**: `--no-help=<anything>` / `--no-version=<anything>` keep the existing `isFlag: false` bare-only wording; bare `--no-help` / `--no-version` keep their `is not valid` pointers.
- **No scan or gateway change**: scan loops, gateway ordering, and strict error-beats-help precedence are untouched in this step.

## Consequences

- `["test", "mytarget", "--help=x"]` reports `Option '--help' does not accept a value 'x'. Use bare '--help'.` (exit 2, `InvalidValue`); `-h=x`, `-?=x`, `/?=x`, `--version=x`, `-V=x` name their own token the same way. Empty `=`-forms (`--help=`) report the value-less variant.
- Docs same pass: `docs/advanced.md` (help-system misuse sentence).
- Pinned by `HelpReservedValueTests.cs`, `VersionReservedValueTests.cs`, `HelpQuestionMarkAliasTests.cs` (`=`-form rows), and `HelpUnknownOrderInvarianceTests.cs` (`HelpEquals_Value_Reports_Invalid`).
