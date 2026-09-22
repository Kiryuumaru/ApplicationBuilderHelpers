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

**Member hiding (`new`)**:
A derived command property that hides a base property with the C# `new` modifier — distinct from gateway **Shadowing** above, which is about reserved words, not members. The reflection walk keeps both entries as base-first duplicates, so a hidden member never silently replaces the base one.

**Dual-marked**:
A property carrying both a CLI marker (`[CommandOption]` / `[CommandArgument]`) and a service marker (`[FromServices]` / `[FromKeyedServices]`). Always a configuration error: any dual-marked `PropertyInfo` in the walk chain throws `InvalidOperationException` (fault, exit 1) — member hiding never excuses the conflict.

**Injection plan**:
The cached per-`Type` service-injection target list (property plus optional keyed-service key) built once via the shared `TypePlanCache` double-checked-lock core and reused across runs; the CLI-bound set is hoisted into the cached plan so bound identity uses one canonical predicate.

**Reserved shorts**:
The `h` / `V` `ShortTerm` values owned by the help/version gateway. `-h` / `-V` win inside combined short clusters even mid-cluster (`ArgumentParser.cs:307-338`), and declaring either as a local `ShortTerm` throws `InvalidOperationException` at registration (fail-closed, `CommandHierarchyBuilder.cs:471-497`), unless `LongName` is `help` for `-h`; `-V` always throws (no version node, gateway-only). Affected options keep the long form only (e.g. `serve --host`).

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

**Holding scope**:
The command whose option list holds an option node. `SubCommandOptionInfo.OwnerCommand` is the definition site (the defining command); `SubCommandOptionInfo.BindTarget` is the holding scope (the defining command for a definition-site node, root or the per-command help scope for a global copy). Help default-value reads resolve definition-site first, then the holding scope.

**Abstract root**:
The root `SubCommandInfo` with no implementation (`IsRoot` at `src/ApplicationBuilderHelpers/CommandLineParser/SubCommandInfo.cs:130`) in a CLI that registers only leaf subcommands. Display name `"<root>"` (`:32`); `ToString()` renders `"<root>"` (`:301`).

**Bare run**:
Invoking with zero args (`[]`) on an abstract root: fails `RequiresSubcommand` (exit 2) with `'<root>' requires a subcommand. Available subcommands: ...` (`src/ApplicationBuilderHelpers/CommandLineParser/ArgumentParser.cs:93-105`); structured `CommandName` stays empty (`:95`) so the footer is global (`src/ApplicationBuilderHelpers/Exceptions/CommandErrorFooter.cs:28-55`).

**Help-first**:
A leading `--help`/`-h` (pre-`--` sentinel) on the abstract root or a known abstract parent (`IsRoot || argIndex > 0`, `ArgumentParser.cs:66-70`) sets `ShowHelp` without erroring, exit 0. Only the root globalizes: `HelpFormatter` branches on `IsRoot` alone (`src/ApplicationBuilderHelpers/CommandLineParser/HelpFormatter.cs:42-44`), so root renders the global help model (`COMMANDS:` section) while a named abstract parent keeps its parent-scoped view (`BuildCommandModel`).

**Term validation**:
The build-time `CommandAttribute.Term` guard: null merges at root; non-null empty/whitespace or dash-led throws `InvalidOperationException` (fault, exit 1); multi-space normalizes via `Split(' ', RemoveEmptyEntries)`. Enforced at both `SubCommandInfo.FromCommand` (`SubCommandInfo.cs:140-159`) and the hierarchy build (`src/ApplicationBuilderHelpers/CommandLineParser/CommandHierarchyBuilder.cs:82-88,193-204`).

**Positive-`=`-form**:
A known boolean flag in in-token `=`-form (`bool`/`bool?`, e.g. `--verbose=banana`, `-v=banana`) — distinct from the **`--no-`-form** (`--no-<name>=value`, which never accepts a value and is owned by the #508 `--no-` mirror). On an abstract prefix with no implementation, a positive-`=`-form with an invalid literal reports `InvalidValue` (exit 2) naming the option plus the valid literals (`true/false/yes/no/on/off/1/0` case-insensitive, delegated to `SubCommandOptionInfo.ExtractValue`) instead of `RequiresSubcommand` (#542 gate at `src/ApplicationBuilderHelpers/CommandLineParser/ArgumentParser.cs:473-501`, call at `:86`).

**InvalidValue-beats-RequiresSubcommand**:
The abstract-root precedence: #512 reserved help-word misuse, then #542 invalid flag literals, then the #508 unknown-option scan — each reporting `InvalidValue`/`UnknownOption` (exit 2) before the `RequiresSubcommand` throw at `ArgumentParser.cs:105`. Valid literals, bare flags, valued options, unknown bases (still `UnknownOption`), `--no-`-prefixed tokens, and post-`--` tokens fall through unchanged.
