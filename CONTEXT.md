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
A required value with no supplied token and no applicable fallback; fails with `MissingRequired` (exit 2).

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
