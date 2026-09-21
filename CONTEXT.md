# ApplicationBuilderHelpers

A .NET library for building command-line applications with a fluent API, dependency injection, and modular architecture.

## Language

**Reference**:
A single configuration value of the form `@ref:key` that points at another key.

**Chain**:
A sequence of hops followed from one reference to the next.

**Hop**:
One `@ref:` resolution from a reference to the key it points at.

**Terminal**:
The final non-`@ref:` value a chain resolves to.

**Cycle**:
Revisiting a key already seen in the same chain.

**Overflow**:
Exceeding the maximum depth of a chain.

**Completion gateway**:
The pre-parse stage (`CompletionGateway`) that intercepts `complete` and `completions script|install|uninstall` after hierarchy build, before help/parsing.

**Precedence**:
The gateway order completion > help > parse > version.

**Shadowing**:
The gateway handling its reserved words first, so same-named registered commands never run.

## CLI Presence Glossary

**Present**:
A CLI value whose token was supplied on the command line or via a non-blank environment-variable fallback — including `""`, which counts as present.

**Missing**:
A value with no supplied token and no applicable fallback; fails with `MissingRequired` (exit 2). Covers a required value with nothing supplied, a satisfied required valued scalar repeated bare (`--name John ... --name` at end-of-line, fails regardless of env fallback), and — per #503 — an unsatisfied bare optional valued scalar (`--config` at end-of-line or before a flag-looking neighbor), which fails even with env set, reported as `Missing value for option: <display-name>`. Env fallback rescues only omitted (never-typed) options, never a typed bare.

**Bare repeat**:
A trailing valueless occurrence of a valued scalar (`--config` at end-of-line, or satisfied-then-bare). Unsatisfied bare (no merged value anywhere): always fails with `MissingRequired` (exit 2) even with env set — required scope (`Missing required option: <display-name>`) and optional scope (`Missing value for option: <display-name>`) alike. Typing the option claims ownership; env fallback rescues only omitted (never-typed) options. Satisfied-then-bare repeat: required fails regardless of env; optional is ignored — the prior value stands. Bare-then-valued heals; collections accumulate; bare boolean flags stay idempotent.

**Omitted**:
A value with no supplied token (optional values keep their property default).

**Empty**:
The zero-length string `""` supplied as a token; counts as present and satisfies `Required`. For string-typed targets it binds verbatim as `""`; non-string targets follow per-type parser semantics (unparseable types report `InvalidValue`). Named `bool` options reject `""` (`InvalidValue`, exit 2); the `BoolTypeParser` empty-binds-`true` path applies only to positional `bool` arguments.

**Null**:
No value at all (`null` raw); distinct from `""`.

**Whitespace-only**:
A token of only whitespace (e.g. `" "`); for string-typed targets preserved verbatim, never trimmed — non-string targets follow per-type parser semantics.

**Defaulted**:
A property value left at its initializer because the CLI input was omitted.
