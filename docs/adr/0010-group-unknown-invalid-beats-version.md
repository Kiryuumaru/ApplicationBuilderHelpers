# ADR 0010: Group-Path Unknown/Invalid Beats Version (#586)

**Status**: Accepted (issue #586).

Supersedes nothing; extends ADR 0008 (`0008-abstract-root-invalid-flag-literal.md`) and the precedence contract (`completion > help > parse > version`) without editing earlier ADRs in place.

## Context

On an abstract command with no implementation and children (bare root, `config` hub), a pre-`--` version token (`--version`/`-V`) set `ShowVersion` and returned before any error scan, so `config --bogus --version` and `config --quiet=banana --version` printed the version (exit `0`) instead of reporting the error. The sibling help path already refused to mask path errors: a mistyped subcommand plus `--help` stays an error (exit `2`, #558). The version path had no equivalent guard, so the same typo reported an error with `--help` but a version with `--version`.

## Decision

- **Narrow pre-version error gate, no new scan logic**: before the version check, run the existing invalid-literal gate then the existing unknown-option gate (`src/ApplicationBuilderHelpers/CommandLineParser/ArgumentParser.cs:67-72`, version check at `:74-78`). Both scans skip help/version tokens (`:428-429`, `:473-474`), scan pre-`--` tokens only, and run in either token order — `config --version --bogus` fails the same as `config --bogus --version`.
- **Scope mirrors the abstract-branch gates**: same `target.AllOptions` scope, same sentinel scan, same `--no-`/cluster/numeric handling as the post-version calls (`:92-93`); bare-valued, reserved help-word, and surplus-path handling stay at the post-version branch unchanged. Pure `--version` (no pre-`--` error token) falls through to `ShowVersion` (exit `0`); post-`--` unknowns stay silent for `RequiresSubcommand`.
- **Precedence**: completion > help > parse > version stands, with one group-path carve-out: a pre-`--` unknown/invalid token beats `--version` (exit `2`).

## Consequences

- `["config", "--bogus", "--version"]` and `["config", "--version", "--bogus"]` report `Unknown option: --bogus` (exit `2`); `["config", "--quiet=banana", "--version"]` in either order reports `Invalid Boolean value 'banana' for option '--quiet'` (exit `2`); never version text.
- `["config", "--version"]` still prints the version (exit `0`); `["config", "--version", "--", "--bogus"]` still prints the version (exit `0`); `["config", "--", "--bogus", "--version"]` stays `RequiresSubcommand` (exit `2`).
- Docs same pass: `docs/advanced.md` (tokenizer bullet + precedence/exit qualifiers), `docs/commands.md` (tokenizer bullet + abstract-gate/precedence/exit qualifiers).
- Pinned by `HelpVersionPrecedenceTests.cs` (group unknown/version ×2, group sentinel ×3, group pure-version ×1, group invalid-literal/version ×2).
