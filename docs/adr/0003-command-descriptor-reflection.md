# ADR 0003: CommandDescriptorReflection Leaf Extraction

**Status**: Accepted (per Vitruvius decision; caller-provided context: zero behavior change — characterization 57/57, full 858/858 — cited as given, not re-run under docs-only restraint).

## Context

`SubCommandOptionInfo`, `SubCommandArgumentInfo`, and `CommandReflectionCache` each performed the same pure-reflection queries inline: the C# `required`-keyword check, the display type-name mapping, and the nullable-unwrap/enum-candidate snapshot. The queries were duplicated across the descriptor path (`FromProperty`, `GetTypeName`, `FromOptionProperty`/`FromArgumentProperty`) with no behavioral difference between call sites.

The new internal static leaf `CommandDescriptorReflection` (`src/ApplicationBuilderHelpers/CommandLineParser/CommandDescriptorReflection.cs:15-66`) consolidates exactly those three queries, with delegation from:

- `SubCommandOptionInfo.FromProperty` (`SubCommandOptionInfo.cs:110`) and `GetTypeName` (`SubCommandOptionInfo.cs:199-204`, delegation at `:203`);
- `SubCommandArgumentInfo.FromProperty` (`SubCommandArgumentInfo.cs:103`) and `GetTypeName` (`SubCommandArgumentInfo.cs:155-160`, delegation at `:159`);
- `CommandReflectionCache.FromOptionProperty` (`CommandReflectionCache.cs:177-196`, delegation at `:179,:193`) and `FromArgumentProperty` (`CommandReflectionCache.cs:198-216`, delegation at `:200,:213`).

## Decision

Extract verbatim into internal static `CommandDescriptorReflection` — pure reflection only, no behavior change:

- `IsPropertyRequired` (`CommandDescriptorReflection.cs:20-31`): the C# `required`-keyword query via `RequiredMemberAttribute` (`NET7_0_OR_GREATER` direct check, name-fallback below). Leaf owns this because it is a context-free `PropertyInfo` predicate — same answer regardless of caller.
- `GetTypeDisplayName` (`CommandDescriptorReflection.cs:37-50`): the already-resolved target-type → display-name mapping (`TEXT`/`NUMBER`/`BOOL`/`DATE`/`DIR`/`FILE`, else upper-invariant). Leaf owns this because it is a pure `Type` function; per its docstring (`:34-36`), callers resolve collections (`IsCollection ? ElementType : PropertyType`) themselves (`SubCommandOptionInfo.cs:201`, `SubCommandArgumentInfo.cs:157`).
- `GetEnumCandidate` (`CommandDescriptorReflection.cs:56-66`): `Nullable` unwrap plus `Enum.GetNames` snapshot. Leaf owns this because it answers only "is this an enum, and what are its names" — no parser-derived state, per its docstring (`:55`).

Policy stays in the parser layer (deliberately not moved into the leaf):

- The live type-parser check (`TypeParsers.ContainsKey`) remains in `SubCommandOptionInfo.ShouldAutoPopulateEnumValues` (`SubCommandOptionInfo.cs:472-492`) and `ResolveValidValues` (`SubCommandOptionInfo.cs:176-194`); the leaf's `GetEnumCandidate` performs no parser check. The parser layer decides auto-population; the leaf only supplies the candidate.
- `GetEnumValues` (`SubCommandOptionInfo.cs:497-511`) stays with its policy caller (`FromProperty`, `SubCommandOptionInfo.cs:129-132`).

Inheritance scope excluded (Chesterton fence):

- `ApplyInheritanceScope` (`SubCommandOptionInfo.cs:260`), `DetermineInheritanceScope` (`SubCommandOptionInfo.cs:283`, `SubCommandArgumentInfo.cs:215`), and the single owned `BaseType` walk `GetAllProperties` (`CommandReflectionCache.cs:223-228`) stay where they are. The leaf owns no walk, no freeze, no ordering, and no parser-derived state (leaf docstring, `CommandDescriptorReflection.cs:13`). Moving the walk or the declaring-type-vs-target-type inheritance check into the leaf would collapse the one place the inheritance rule lives into a utility that has no business owning it.

## Consequences

- `SubCommandOptionInfo`, `SubCommandArgumentInfo`, and `CommandReflectionCache` delegate the three pure-reflection queries to the leaf; all policy (parser suppression, `FromAmong` precedence, inheritance scope, walk ownership) stays in the parser layer.
- No observable CLI or public-API change: `CommandDescriptorReflection` is `internal static`, so `docs/commands.md` and `docs/api-reference.md` are intentionally untouched (see report). `README.md` (176 lines, no internals section) and `CONTEXT.md` (no descriptor terms; no new shared user-facing term entered the language) are likewise untouched.
- Contract pinned by caller-provided verification (not re-run here): characterization 57/57, full suite 858/858.
