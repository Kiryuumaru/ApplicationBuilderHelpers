# ADR 0005: Positional-Argument Enum Auto-Population via Shared Predicate (Option B)

**Status**: Accepted (issue #478, Option B).

Supersedes nothing; extends ADR-0004 (descriptor-walk-enum-unification, which
unified the option-side predicate) without editing it in place. ADR-0003's
parser-layer policy stands unchanged.

## Context

Options auto-populate `ValidValues` from enum names when `FromAmong` is empty
(`SubCommandOptionInfo.ResolveEnumValues`, single predicate per ADR-0004);
positional arguments only honored explicit `FromAmong`
(`SubCommandArgumentInfo.FromProperty :108`, `FromDescriptor :135`). Issue
#478 asks for parity via one shared predicate. Owner decisions: suppression
symmetric (a live parser for the enum type suppresses both kinds); large enums
show the full list symmetrically (no truncation on either kind).

## Decision

- **Shared predicate**: new internal static `EnumValidValues`
  (`src/ApplicationBuilderHelpers/CommandLineParser/EnumValidValues.cs`) with
  live (`Resolve(Type, ...)`) and frozen (`Resolve(Type?, string[]?, ...)`)
  overloads. Semantics moved verbatim from
  `SubCommandOptionInfo.cs:192-210`: explicit `FromAmong` wins; else
  frozen/live enum names iff a candidate exists AND no live parser is
  registered for that enum type; else null.
- **Option rewire (behavior-neutral)**: `SubCommandOptionInfo`
  `ResolveEnumValues` overloads + `ResolveValidValues` (`:133-134, :180-210,
  :218-221`) delegate to the helper.
- **Argument threading**: `SubCommandArgumentInfo.FromProperty` gains an
  optional tail `ICommandTypeParserCollection?` param (resolves via the helper
  + `GetEnumCandidate` unwrap, replacing the `FromAmong`-only line `:108`);
  `FromDescriptor` takes a `resolvedValidValues` param (assigns like option
  `:160`); `FromProperties` + both obsolete shims gain the optional tail
  parser param and forward it. All internal.
- **Builder per-run resolution**: `CommandHierarchyBuilder.cs:62-64` and
  `:202-205` call
  `SubCommandArgumentInfo.ResolveValidValues(descriptor, typeParserCollection)`
  (new forwarder mirroring the option path).
- **Placement — why not the leaf**: parser policy stays in the parser layer
  per ADR-0003. The leaf (`CommandDescriptorReflection.GetEnumCandidate`)
  answers only "is this an enum, and what are its names"; the
  `TypeParsers.ContainsKey` check lives in `EnumValidValues`, next to its
  callers, not in the pure-reflection leaf.
- **Untouched**: `CommandDescriptorReflection.cs` (pure reflection),
  `CommandReflectionCache.cs` (already freezes both kinds' candidates),
  `ValueBinder`/`TypeConversion`/`CompletionEngine`/`HelpContentProvider`
  (already kind-symmetric — they read `ValidValues`, so parity flows through
  with no edits).

## Consequences

- Plain-enum (and nullable-enum) arguments now validate, list in help
  (`Possible values:`), and complete exactly like options. Explicit `FromAmong`
  still wins on both kinds; a live custom parser suppresses both kinds.
- Behavior change is intentional and pinned: the old
  `Argument_PlainEnum_LeavesValidValuesUnset` pin is replaced with parity
  asserts (plain enum → names; `FromAmong` wins; live parser suppresses;
  nullable unwraps), plus a builder-level late-parser-suppression test for
  arguments and help/completion coverage for a plain-enum argument.
- A latent secret-enum-argument message shift surfaces with parity: a
  secret plain-enum argument with unparseable input now reports the
  NotAmong shape (`Value provided for argument 'kind' is not valid. Must be
  one of: ...`, value omitted, list kept) instead of the bare conversion
  shape (`Cannot convert provided value to ...`), because auto-populated
  `ValidValues` routes the failure through the allowed-list fallback. This
  matches the existing secret-option-with-choices shape
  (`SecretOption_InvalidAllowedValue_OmitsValueKeepsValidList`) and the
  non-secret plain-enum argument shape (`Must be one of: ...` with the value
  echoed). `SecretEnumArgument_InvalidValue_OmitsValue` is updated to the
  NotAmong shape in the same pass.
