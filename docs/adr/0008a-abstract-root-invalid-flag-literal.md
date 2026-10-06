# ADR 0008: Abstract-Root Invalid Flag Literals Beat RequiresSubcommand (#542)

**Status**: Accepted (issue #542).

Supersedes nothing; extends ADR 0007 (`0007-abstract-root-requires-subcommand.md`) and the exit-code contract (`InvalidValue`, exit 2) without editing earlier ADRs in place.

## Context

On an abstract command with no implementation (bare root, `config` hub), a known boolean flag in in-token `=`-form with an invalid literal (e.g. `--verbose=banana` where `verbose` is a root-visible `bool`/`bool?`) fell through the #508 unknown-option skip-known rule (`MatchesArgument`, lexical) to the `RequiresSubcommand` throw. The sibling `=`-form paths already reported `InvalidValue`: the dispatch path (`ExtractValue` → `ValidateFlagLiteral` → `SecretRedaction.InvalidFlagLiteralMessage`, e.g. `--verbose=maybe` on a leaf) and the `--no-` mirror (`--no-verbose=x` on an abstract prefix). The abstract root had no equivalent gate, so the same typo reported `RequiresSubcommand` at the root but `InvalidValue` on a leaf.

## Decision

- **Narrow standalone gate after #512, before #508**: `ThrowOnInvalidFlagLiteralPreSentinelOption` (`src/ApplicationBuilderHelpers/CommandLineParser/ArgumentParser.cs:473-501`, call at `:86`). Scans pre-`--` leftovers for a known flag in in-token `=`-form whose literal is invalid; the literal check delegates to `SubCommandOptionInfo.ExtractValue(token, null)` (no literal-table copy), which throws `InvalidValue` naming the option plus the valid literals (`true/false/yes/no/on/off/1/0` case-insensitive) via the secret-aware helper. Rethrown with the per-command name attached (`target.FullCommandName`), same pattern as the dispatch-path rethrow.
- **Scope and exemptions mirror the #508 scan**: scope is `target.AllOptions` via `MatchesArgument` (same scope as the skip-known rule, so leaf-only bases stay `UnknownOption`); flags plus in-token `=` only; same sentinel scan (`TakeWhile t != --` shape via `Array.IndexOf`), `IsNumericValue` (`:431-444`), and help/version (`HelpVersionGateway.IsHelpToken/IsVersionToken`) exemptions; `--no-`-prefixed tokens excluded (owned by the #508 `--no-` mirror, which runs later). Valid literals, bare flags, valued options (`--data=x`), unknown tokens, and post-`--` tokens fall through to the #508 scan / `RequiresSubcommand` path unchanged.
- **Precedence**: #512 reserved help-word misuse → #542 invalid flag literals → #508 unknown-option scan → `RequiresSubcommand` throw (`:105`). An invalid literal beats both `RequiresSubcommand` and `UnknownOption`; a valid literal keeps `RequiresSubcommand`.

## Consequences

- `["--verbose=banana"]` on an abstract root with a root-visible `verbose` flag reports `Invalid Boolean value 'banana' for option '--verbose'. Expected ...` (`InvalidValue`, exit 2) with the `InvalidValue` footer, never `RequiresSubcommand`. Secret flags redact (`Invalid Boolean value provided for option '--secure'. ...`); short `-v=banana` names `--verbose`; `["hub", "--verbose=banana"]` attaches the `hub` command name.
- Docs same pass: `docs/advanced.md` (`=`-form bullet + bare-root table), `docs/commands.md` (`=`-form bullet), `CONTEXT.md` (positive-`=`-form vs `--no-`-form, InvalidValue-beats-RequiresSubcommand).
- Pinned by `AbstractRootFlagLiteralTests.cs` (17 tests: invalid/empty/short-form literals, valid-literal fall-through ×6, secret redaction, valued/bare/cluster fall-through, sentinel silence, `--no-` unchanged, leaf-only unknown, abstract-prefix command name).
