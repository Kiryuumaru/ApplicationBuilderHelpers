# ADR 0021: Restore Boolean Flag Value Forms (#697)

**Status**: Accepted (issue #697).

Supersedes the user-flag `=`-form and space-form rows of ADR 0015
(`0015-bare-only-boolean-flags.md`) without editing it in place.
ADR 0015 stays frozen; its bare-only rows now read through this record.
Frozen ADRs 0008a (`0008a-abstract-root-invalid-flag-literal.md`) and 0018
(`0018-reserved-help-version-bare-only.md`) keep their history and read
through this record where they taught the bare-only split.

## Context

ADR 0015 (issue #602) over-corrected. #602 complained only about space-form
mis-blame — a bare flag followed by a boolean-looking word was blamed instead
of left positional — and cited the `=`-form as working. The guard ADR 0015
landed removed both: it rejected every `--verbose=<literal>` even though the
in-token form is unambiguous, and it rejected `--verbose off` instead of
leaving `off` positional. The live docs before the guard taught the `=`-form
with a `true/false/yes/no/on/off/1/0` literal table.

The space form had never bound values before the guard either: pre-guard the
parser took no space-form value on flags, so `["--verbose", "off"]` left both
tokens alone under the old split. Space-form binding is therefore new
behavior, not a restore. It follows System.CommandLine's only-if-convertible
semantics: bind the neighbor only when it converts, else leave it positional.
That keeps the #602 fix (no mis-blame) while adding the convenience.

## Decision

- **Bare forms unchanged**: bare `--verbose` (or `-v`) binds `true`; bare
  `--no-verbose` binds `false` (`SubCommandOptionInfo.cs:286-297`,
  negation return at `:278`). Negation exists only for `bool`/`bool?` with
  a long name (`:43`).
- **`=`-form binds again**: `--verbose=<literal>` validates the literal via
  `ValidateFlagLiteral` (`SubCommandOptionInfo.cs:326-331`) against the
  `IsBooleanValue` set (`:302-312`: `true`/`yes`/`on`/`1` bind `true`;
  `false`/`no`/`off`/`0` bind `false`; case-insensitive), which matches the
  `BoolTypeParser` conversion set
  (`src/ApplicationBuilderHelpers/ParserTypes/BoolTypeParser.cs:19-34`).
- **Invalid `=`-literal fails fast**: an invalid literal, including empty
  `--verbose=`, throws `InvalidValue` (exit 2) via
  `SecretRedaction.InvalidFlagLiteralMessage`
  (`src/ApplicationBuilderHelpers/CommandLineParser/SecretRedaction.cs:118-126`):
  `Invalid Boolean value '<literal>' for option '--verbose'. Expected
  'true', 'false', 'yes', 'no', 'on', 'off', '1', or '0'` (redacted for
  secrets) — never a later positional. The dispatch path rethrows with the
  command name attached; the abstract pre-scan
  (`ThrowOnFlagEqualsPreSentinelOption`, `ArgumentParser.cs:701-730`)
  runs the same `ExtractValue` gate, so a valid literal falls through to
  `RequiresSubcommand` while an invalid one reports `InvalidValue`.
- **Space form binds only valid literals**: `--verbose <literal>` binds when
  the neighbor passes `IsBooleanValue` and consumes it, on both the
  dispatch path (`ArgumentParser.cs:278-287,315-317`) and the path-walk
  (`:39-46`); a non-literal neighbor stays positional (`--verbose Alice`
  binds `true`, `Alice` stays positional). This is new behavior with
  only-if-convertible semantics.
- **Negation stays bare-only**: `--no-verbose=<anything>` fails (exit 2,
  `does not accept a value`; `SubCommandOptionInfo.cs:270-279`) and
  `--no-verbose` followed by a boolean word fails the same way (dispatch
  `:280-284`; path-walk `:41-42` breaks so the word stays positional and
  the downstream bare-negation/positional rules apply). Unchanged
  from ADR 0015.
- **`-v` mirrors the long forms**: `-v=true` binds via the short `=` branch
  (`SubCommandOptionInfo.cs:260-268`); `-v true` binds via the same
  space-form gate (`IsBareFlagToken` covers `-v`, `ArgumentParser.cs:941-950`).
  Attached `-vtrue` never binds as a boolean; it stays on the cluster path
  (`TryHandleCombinedShortCluster`, `ArgumentParser.cs:431+`;
  `IsClusterToken` excludes `=`-forms). Unchanged.
- **Valued options and reserved words unchanged**: in-token `=` stays valid
  for non-flags; `--help=<anything>` / `--version=<anything>` misuse forms
  keep their own messages (ADR 0018).

## Consequences

- `["--verbose"]` binds `true`; `["--no-verbose"]` binds `false`.
  `["--verbose=off"]` binds `false`; `["--verbose=maybe"]` and
  `["--verbose="]` fail (exit 2, `InvalidValue`). `["--verbose", "off"]`
  binds `false`; `["--verbose", "Alice"]` binds `true` with `Alice`
  positional. `["--no-verbose=x"]` and `["--no-verbose", "off"]` fail
  (exit 2, `does not accept a value`).
- Docs same pass: `docs/commands.md` (typing bullets),
  `docs/advanced.md` (tokenizer short version), `docs/api-reference.md`
  (env/flag sentence), `CONTEXT.md` (supplied entry plus boolean-flag
  entry). ADR 0015 keeps its bare-only body as history and reads through
  this record.
- Pinned by `ValidateFlagLiteral` (`SubCommandOptionInfo.cs:326-331`),
  `IsBooleanValue` (`:302-312`), `BoolTypeParser.cs:19-34`, the dispatch
  space gate (`ArgumentParser.cs:278-287,315-317`), the path-walk space gate
  (`:39-46`), and the abstract `=`-form pre-scan (`:701-730`).
