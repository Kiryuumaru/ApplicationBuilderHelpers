# ADR 0024: Parser Defaults for Omitted Options (Option B, #762)

**Status**: Accepted (issue #762, Option B).

Supersedes the help-only parser-default fallback described in ADR-0004a
(`0004a-descriptor-walk-enum-unification.md`, "DefaultValue deletion"); that
record carries a pointer note without a body rewrite. ADR-0004b's
`OwnerCommand`/`BindTarget` identity policy stands unchanged.

## Context

`ICommandTypeParser.GetDefaultValue` was public but had no binding role. An
omitted optional valued option kept its property initializer or CLR default;
help consulted a parser default only through a narrow fallback
(`HelpContentProvider.GetDefaultValueFromTypeParser`, removed in #762). Help
and binding could therefore disagree, and the parser's omitted-value contract
was only partly honored.

#762 chooses Option B: the registered parser default participates in
omitted-option resolution while explicit input and real initializers stay
authoritative. The policy must work for both registration kinds:
`AddCommand<T>()` creates a fresh instance per run, while `AddCommand(instance)`
keeps one caller-supplied object whose values may change between runs.

## Decision

- **One resolver per run** — `CommandLineParser` constructs
  `OptionDefaultResolver` once and passes it to both `ValueBinder` and
  `HelpVersionGateway` (`CommandLineParser.cs:42-45`). Help and binding
  therefore use the same parser-default cache and the same registration
  snapshot.
- **Binding precedence** — for an omitted optional valued option: explicit CLI,
  then a non-blank environment fallback (both already merged into
  `OptionValues`), then a non-CLR-default initializer, then the exact-type
  parser default, then the CLR default
  (`OptionDefaultResolver.TryResolveOmittedValue`,
  `OptionDefaultResolver.cs:28-71`). `ValueBinder` applies the result after
  explicit binding (`ValueBinder.cs:90,121-138`).
- **Exclusions** — required options, flags, and collections never receive
  parser defaults (`OptionDefaultResolver.cs:31-32`). A required option still
  fails as missing; a flag keeps its bare/initializer value; a collection is not
  filled.
- **Initializer precedence is observational** — the resolver treats a property
  value equal to the CLR default as "no initializer". Non-nullable value types
  compare against a zeroed instance created without running constructors
  (`RuntimeHelpers.GetUninitializedObject`), never against a constructed
  instance (`IsClrDefault`/`GetClrDefault`,
  `OptionDefaultResolver.cs:262-275`); nullable value types and reference types
  are CLR-default only when the value is null. Hence `int? = 0` and
  `bool? = false` are real initializers and win over a parser default; only
  `int? = null`, `bool? = null`, and a reference `= null` are indistinguishable.
  On a non-nullable value type, `= 0`, `= false`, a zero-valued enum such as
  `MyEnum.None`, and a zero-valued struct are indistinguishable. Any other
  initializer stays authoritative, including `= ""`.
- **Instance registrations** — `TypedCommandHolder` snapshots every
  `[CommandOption]` property at registration (`TypedCommandHolder.cs:30-34,60-80`)
  and records explicit/environment binds (`MarkOptionBound`,
  `OptionDefaultResolver.cs:170-180`, called from `ValueBinder.cs:87`). Binding
  applies a parser default only while the live value still equals the snapshot
  and no bind was recorded (`OptionDefaultResolver.cs:38-54`). Help shows the
  snapshot for an initializer-sourced property; for a parser-default-sourced
  property it shows the live value once the instance was explicitly bound or
  otherwise changed, and the parser default while untouched
  (`TryGetHelpDefault`, `OptionDefaultResolver.cs:73-124`).
- **Help formatting** — when the option type has a registered parser, `GetString`
  formats the `Default:` text for both initializer and parser values
  (`FormatDefault`, `OptionDefaultResolver.cs:126-154`). Null, empty,
  whitespace-only, all-control, or throwing formatters suppress the line;
  control characters and U+2028/U+2029 become spaces before display
  (`SanitizeDefaultDisplay`, `OptionDefaultResolver.cs:156-168`;
  `HelpContentProvider.cs:382-397`). Secret optionals still show `[REDACTED]`
  (`HelpContentProvider.cs:384-387`).
- **No-op defaults** — a parser default equal to the CLR default or empty is
  skipped (`IsNoOpDefault`, `OptionDefaultResolver.cs:258-259`), so it neither
  binds nor contributes a `Default:` line.
- **Type gate and allowed values** — a parser default must be runtime-assignable
  to the declared property type (`IsCompatibleDefault`,
  `OptionDefaultResolver.cs:207-215`); a wrong-typed default is cached as absent:
  no binding, no parser `Default:` line, no error. `FromAmong` is checked only
  after the type gate (`TryGetParserDefault`,
  `OptionDefaultResolver.cs:217-256`; `TypeConversion.IsAllowedValue`,
  `TypeConversion.cs:164`). A value outside the allowed list is likewise cached
  as absent. Explicit invalid input still fails validation.
- **Nullable lookup is exact-type** — the resolver reads
  `TypeParsers[option.PropertyType]` (`OptionDefaultResolver.cs:222`), so a
  `Port?` property does not borrow a `Port` parser. A `null` parser default is
  a no-op.
- **Inheritance** — one shared `CommandReflectionCache.ResolveHolderProperty`
  (`CommandReflectionCache.cs:140-172`; canonicalization `:174-186`) serves the
  resolver and the promotion gate on both registration kinds. It follows a
  derived property only for a real override of an option-bearing base property.
  An attribute-less shadow does not replace the base descriptor, and an
  independent `new` declaration with its own attribute stays a separate option
  instead of replacing it. The result is canonicalized to the `Walk` declaration
  by declaring type and property name. Instance snapshots and mutation keys
  therefore match through Base -> Mid (repeated `[CommandOption]` override) ->
  Leaf. Any inherited private-base declaration (with or without a derived
  shadow) is rejected for parser defaults; no holder property resolves. A real
  initializer, if any, is preserved and shown in help; on an instance
  registration, help does not follow a later explicit binding.
- **Parser-default lifetime** — `GetDefaultValue` is called once per option per
  run and its return value is stored directly on the property. The public
  contract (`ICommandTypeParser.cs:33-39`; `CommandTypeParser.cs:54-62`)
  requires a fresh instance for mutable reference types; the library binds the
  returned object directly and does not clone it.
- **No public API change** — `ICommandTypeParser.GetDefaultValue` stays as-is,
  and `CommandTypeParser<T>.GetDefaultValue` remains non-virtual. A parser built
  on the base class still returns `default(T)` and contributes no non-default
  binding; implement `ICommandTypeParser` when a custom default is needed.

## Options Considered

- **A — help-only default (status quo)**: rejected because binding ignored the
  parser's omitted-value contract and help and binding could disagree.
- **B — runtime resolver shared by help and binding (chosen)**: no public API
  change, works for type and instance registrations, and can compare the actual
  runtime value against the registration snapshot.
- **C — source generator to detect initializers**: rejected. The library ships
  as a normal package and cannot analyze consumer initializers at build time
  without every consumer opting into a generator; generated members would also
  drive partial-type or attribute machinery. A generator still could not know
  values supplied to `AddCommand(instance)` or mutated between runs.
- **D — IL inspection of constructors**: rejected. Reading constructor IL is
  compiler-shape dependent, fragile under trimming/AOT, and still cannot
  distinguish a runtime instance registration's current state from its
  registration value. The runtime CLR-default comparison gives the required
  observable semantics without that fragility.
- **E — always apply the parser default**: rejected; a real initializer stays
  authoritative, and explicit CLI/environment values must win.

## Consequences

- On type registrations, omitted optional valued options can bind parser
  defaults; help shows the same default the binder uses where the value is
  displayable. Instance registrations follow the snapshot rules below.
- An initializer writing the CLR default is observationally absent: non-nullable
  value type zero values, and `null` for nullable and reference types. Use a
  non-CLR-default initializer to pin a value. `= ""` is still a real
  initializer, even though help prints no `Default:` line for it.
- Instance registrations preserve explicit state across runs and apply parser
  defaults only while untouched and equal to the registration snapshot; help
  reflects registration initializers or live state as described above.
- Mutable parser defaults must be fresh instances. A shared static instance
  would leak mutations between options and runs.
- A parser default that is not runtime-assignable to the property type, or is
  outside `FromAmong`, is silently non-effective by design; explicit invalid
  input still errors.
- Docs same pass: `docs/custom-type-parsers.md#defaults-for-omitted-options`,
  `docs/commands.md#omitted-values-and-parser-defaults`,
  `docs/configuration.md#required-options-in-help`,
  `docs/api-reference.md` (parser table), `CONTEXT.md` (Omitted/Parser
  default), `CHANGELOG.md` (Unreleased). ADR-0004a carries a pointer note.
- Pinned by `OptionParserDefaultTests`
  (`src/ApplicationBuilderHelpers.Test.Cli.UnitTest/OptionParserDefaultTests.cs`):
  binding and help for struct/class defaults, precedence, exclusions,
  initializer caveat (including nullable zero/false), instance-registration
  state, inheritance (including private-base fail-closed), nullable exact-type,
  wrong-typed defaults, `FromAmong` rejection after the type gate, secret
  masking, formatting suppression, sanitization, and per-option freshness.
