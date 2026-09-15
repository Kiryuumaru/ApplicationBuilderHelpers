# ADR 0001: Preserve Empty-String CLI Values

**Status**: Accepted (documents landed fix for #433; strict empty-rejecting mode is a follow-up).

## Context

A quoted empty positional (`""`) must be distinguishable from an omitted
argument: `ParameterValidator` checks required arguments by presence
(`ArgumentValues.TryGetValue` + count, `ParameterValidator.cs:33`), and
`ValueBinder` skips only when no values were recorded
(`values.Count == 0`, `ValueBinder.cs:71`) — otherwise the raw token,
including `""`, flows into `TypeConversion.Convert` (`ValueBinder.cs:91`)
and then to the per-type parser. For `string` targets,
`StringTypeParser.ParseValue` returns it verbatim
(`StringTypeParser.cs:9-13`) via the string passthrough
(`TypeConversion.cs:132-135`); non-string targets follow their own parser
semantics — e.g. named `bool` options reject `""` at the flag-literal
gate (`SubCommandOptionInfo.cs:399-400,466-470`, `InvalidValue`, exit 2),
while the `BoolTypeParser` empty-binds-`true` path (`BoolTypeParser.cs:12-16`)
is reachable only for positional `bool` arguments, which pass through with
no literal gate (`ArgumentParser.cs:340-346`); `FileInfo`/`Uri`/`int`
reject it as `InvalidValue`
(`FileInfoTypeParser.cs:12-20`, `UriTypeParser.cs:12-24`,
`IntTypeParser.cs:11-18`).

## Decision

Preserve: a supplied `""` counts as present and satisfies `Required`;
only an omitted value fails with `MissingRequired` (exit 2). Binding is
verbatim only for string-typed targets; non-string `""`
follows per-type parser semantics (unparseable types → `InvalidValue`,
exit 2). Path split for `bool`: named options reject `""` (`InvalidValue`,
exit 2, and space-form never consumes a following token); positional `bool`
arguments bind empty to `true` via `BoolTypeParser` (no literal gate;
pinned by `Argument_Boolean_EmptyValue_BindsTrue`, `ValueBindingTests.cs:806-818`). The same rule applies to named options (shared
pipeline, `ValueBinder.cs:58`), with one named-only divergence: an empty
or whitespace-only environment-variable fallback is treated as unset
(`EnvVarFallback.cs:33-35`), and CLI-wins precedence means an explicit
`--opt ""` downgrades a set env value. Whitespace-only tokens follow the
same per-type rule — no trimming exists in the conversion path.

## Options Considered

- **Preserve (chosen)**: `""` counts as present and passes required
  (verbatim binding for string targets); validator stays presence-only.
  Matches the pipeline above and keeps presence-level options/arguments
  parity.
- **Strict (deferred follow-up)**: an opt-in mode rejecting empty input
  (e.g. as `InvalidValue`). Not implemented; no such symbol exists in code.

## Industry Precedent

POSIX shells deliver `""` as a real zero-length argv entry (distinct from
absent), and mainstream CLI parsers (e.g. System.CommandLine, argparse,
Click) surface it as an empty string rather than null — so preserving is
the least-surprise behavior.

## Consequences

- Options/arguments parity is presence-level (`""` counts as present for
  both through the shared `TypeConversion` pipeline); binding itself is
  per-type, with the named-vs-positional `bool` split noted above.
- Validator stays presence-only; content rules (if any) belong to the
  opt-in strict follow-up, not to required-field validation.
- Env-var fallback remains null-or-whitespace-guarded, so `APP_X=""`
  behaves as unset while `--opt ""` binds `""` — documented in
  `docs/commands.md` Arguments section.
- **Breaking change**: consumers that relied on `""` arriving as `null`
  (e.g. `== null` sentinels) must migrate to `string.IsNullOrEmpty` —
  an explicitly supplied `""` now binds as `""`, never `null`.
