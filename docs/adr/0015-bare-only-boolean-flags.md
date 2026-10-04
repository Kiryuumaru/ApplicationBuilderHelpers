# ADR 0015: Bare-Only Boolean Flags (#602)

**Status**: Accepted (issue #602).

Supersedes ADR 0008a (`0008a-abstract-root-invalid-flag-literal.md`) for user flags without editing it in place. Reserved help/version misuse forms keep their own messages.

## Context

ADR 0008a split `=`-form flag literals into valid (kept `RequiresSubcommand` on abstract paths) and invalid (reported `InvalidValue`). The parser now takes no `=`-form on flags: `SubCommandOptionInfo.ExtractValue` rejects every `--verbose=<anything>` and `-v=<anything>` (`src/ApplicationBuilderHelpers/CommandLineParser/SubCommandOptionInfo.cs:247-266`), and every `--no-verbose=<anything>` (`:268-277`). A bare flag followed by a boolean-looking word is also rejected (`src/ApplicationBuilderHelpers/CommandLineParser/ArgumentParser.cs:258-269`). The live docs still taught the old split (`--verbose=off` for values, a `true/false/yes/no/on/off/1/0` literal table).

## Decision

- **Bare only**: bare `--verbose` (or `-v`) binds `true`; bare `--no-verbose` binds `false` (`SubCommandOptionInfo.cs:284-295`, negation return at `:276`). Negation exists only for `bool`/`bool?` with a long name (`:40-42`).
- **Any `=` on a flag fails** (`InvalidValue`, exit 2) via `SecretRedaction.NoValueAcceptedMessage` (`src/ApplicationBuilderHelpers/CommandLineParser/SecretRedaction.cs:134-155`): `Option '--verbose' does not accept a value 'x'. Use bare '--verbose' (or '--no-verbose' for false).` The abstract pre-scan (`ArgumentParser.cs:590-619`, calls at `:82`, `:116`, `:147`) runs the same gate, so leaf and abstract paths agree.
- **Space form also fails**: bare `--verbose` followed by `off` (or any boolean word) reports `does not accept a value 'off'` (exit 2) instead of binding or leaving it positional (`ArgumentParser.cs:258-269`).
- **Valued options unchanged**: in-token `=` stays valid for non-flags (`--level=02`, `docs/custom-type-parsers.md:158` untouched). `BoolTypeParser` keeps its literal table for type conversion; it is unreachable via flag tokens and out of scope to remove.

## Consequences

- `["--verbose"]` binds `true`; `["--no-verbose"]` binds `false`. `["--verbose=true"]`, `["--verbose=off"]`, `["--verbose=maybe"]`, and `["--verbose", "off"]` all fail (exit 2) with `does not accept a value`. `["--no-verbose=x"]` fails with `Use bare '--no-verbose' to set the flag to 'false'`.
- Docs same pass: `docs/commands.md` (typing bullets), `docs/advanced.md` (tokenizer short version). `docs/configuration.md:55` already bare-only. Frozen ADRs 0008a, 0009/0010/0013 keep their `=`-form rows as history and read through this ADR.
- Pinned by `TokenizerTruthTableTests.cs` (equals-form true/false/invalid rejects, space-word reject, bare/negated binds), `AbstractRootFlagLiteralTests.cs`, and `AbstractRootNegatedValueTests.cs`.
