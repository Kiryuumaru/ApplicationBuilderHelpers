# ADR 0004: Descriptor Walk + Enum-Predicate Unification

**Status**: Accepted (caller-provided context: internal-only refactor, zero behavior change — characterization 78/78, full suite 901/901 — cited as given, not re-run under docs-only restraint).

Supersedes the line-level references in ADR-0003 (which remains immutable history); its policy decisions stand unchanged.

## Context

ADR-0003 extracted three pure-reflection queries into the internal static leaf
`CommandDescriptorReflection` (`src/ApplicationBuilderHelpers/CommandLineParser/CommandDescriptorReflection.cs:15-66`)
but left three duplications in place around it: each of `SubCommandOptionInfo`
and `SubCommandArgumentInfo` owned an inline `DeclaredOnly` walk inside its own
`FromDeclaredType`, the single owned `BaseType` walk lived as
`CommandReflectionCache.GetAllProperties`, and the enum auto-populate decision
existed twice — `ShouldAutoPopulateEnumValues` + `GetEnumValues` on the live
`FromProperty` path alongside a separate frozen-candidate branch in
`ResolveValidValues`. Dead `DefaultValue` metadata on both internal info types
(meaningly always unset — help text reads live property initializers) added a
third copy site in `CommandHierarchyBuilder.CreateGlobalOptionCopy`.

## Decision

Unify the walk and the enum predicate; delete `DefaultValue`; demote the four
legacy entry points to `Obsolete` shims. No behavior change:

### What unified

- **Split walk: `Walk(Type)` + `WalkDeclaredOnly(Type)`**
  (`CommandReflectionCache.cs`) replaces `GetAllProperties` plus both inline
  `DeclaredOnly` walks. `Walk` is the old base-first `BaseType` chain;
  `WalkDeclaredOnly` is the old `DeclaredOnly | Public | NonPublic |
  Instance` branch. Six call sites: `Build` (`CommandReflectionCache.cs`),
  the four shims below, and the service-injection scan
  (`CommandExecutor.cs`).
- **Per-kind `FromProperties` cores** — options
  (`SubCommandOptionInfo.cs:226-241`, base-first, no sort) and arguments
  (`SubCommandArgumentInfo.cs:162-177`, `Position` sort) — each a single
  attribute-read loop shared by that kind's two shims. Per-kind because
  ordering and inheritance differ (see "stayed split").
- **`ResolveEnumValues` live + frozen merge** (`SubCommandOptionInfo.cs:168-198`)
  replaces `ShouldAutoPopulateEnumValues` + `GetEnumValues` (both deleted).
  Live overload (`:168-172`) unwraps via the leaf's `GetEnumCandidate` then
  delegates; frozen overload (`:180-198`) implements the single predicate once:
  explicit `FromAmong` wins, else frozen enum names iff a candidate exists and
  no live parser is registered for it, else null. `ResolveValidValues`
  (`:206-209`) is now a thin forwarder passing descriptor fields through.
  `FromProperty` calls the live overload (`SubCommandOptionInfo.cs:123`).

### What stayed split (deliberately)

- **Parser-layer policy per ADR-0003**: the live `TypeParsers.ContainsKey`
  check stays in `SubCommandOptionInfo.ResolveEnumValues`; the leaf's
  `GetEnumCandidate` (`CommandDescriptorReflection.cs:56-66`) still performs no
  parser check and answers only "is this an enum, and what are its names".
- **Per-kind ordering/inheritance**: options preserve base-first property order
  with the declaringType-vs-targetType check (`ApplyInheritanceScope`,
  `SubCommandOptionInfo.cs:274-292`); arguments sort by `Position`
  (`SubCommandArgumentInfo.cs:176`) with the name-based common-argument
  heuristic (`DetermineInheritanceScope`, `SubCommandArgumentInfo.cs:209-230`).
  Merging the cores across kinds would collapse two different inheritance rules
  into one loop that owns neither.
- **No descriptor filtering on the walk**: filtering by descriptor is
  deliberately not offered (`CommandReflectionCache.cs:218-229`) — see
  override/hide verdict below.

### DAM rationale

`Walk` carries `All` (`CommandReflectionCache.cs`) so the full `BaseType`
loop (which reflects off `BaseType` hops) and every full-walk caller flow
without trim warnings; `WalkDeclaredOnly` carries only
`PublicProperties | NonPublicProperties`, exactly what the declared-only
branch reflects (`DeclaredOnly | Public | NonPublic | Instance`) off the
passed type. Each shim retains its own narrow annotation —
`FromCommandType` keeps `All` (`SubCommandOptionInfo.cs`,
`SubCommandArgumentInfo.cs`), `FromDeclaredType` keeps
`PublicProperties | NonPublicProperties` — and each calls the walk whose
annotation matches, so zero suppressions are needed.

### DefaultValue deletion

`DefaultValue` is deleted from `SubCommandOptionInfo` (formerly adjacent to
`IsSecret`) and `SubCommandArgumentInfo` (formerly `SubCommandArgumentInfo.cs`
`DefaultValue` property), and the `DefaultValue = original.DefaultValue` copy
line is removed from `CreateGlobalOptionCopy`
(`CommandHierarchyBuilder.cs:362-379`, now copies without it).
Help text never read it: `HelpFormatter.GetOptionDefaultValue`
(`HelpFormatter.cs:763-792`) reads the live property value off the command
instance (`:769`, `:776`) and falls back to type-parser defaults
(`GetDefaultValueFromTypeParser`, `:797-831`). The *public* parser interface is
untouched — `ICommandTypeParser.GetDefaultValue` and
`CommandTypeParser<T>.GetDefaultValue` remain, and `docs/api-reference.md` /
`docs/custom-type-parsers.md` references to them stay accurate.

### Override/hide verdict

Duplicates preserved, no descriptor filter: override/hide members keep their
duplicate walk entries through the shared `Walk`, and per-kind parity tests
pin override-vs-hide field-by-field. Offering a descriptor-level filter on the
walk would silently drop the duplicates the characterization suite proves must
survive.

### Obsolete shim verdict

All four legacy entry points are `[Obsolete]` shims over the per-kind cores —
`SubCommandOptionInfo.FromCommandType` (`:247-251`),
`SubCommandOptionInfo.FromDeclaredType` (`:258-262`),
`SubCommandArgumentInfo.FromCommandType` (`:183-187`),
`SubCommandArgumentInfo.FromDeclaredType` (`:194-198`) — each carrying
`"Use CommandReflectionCache for cached descriptors or the per-run
FromDescriptor path instead. This member will be removed in a future major
version."` Internal callers are migrated (`Build`, `CommandExecutor`); the
shims exist only for external reflection callers and die on the next major.
`FromProperty` and `FromDescriptor` (options `:102-159`, arguments `:95-145`)
stay non-obsolete: they are the live and per-run paths the cache serves.

## Consequences

- One walk, one enum predicate, zero descriptor `DefaultValue`: the parser
  layer owns policy, the leaf owns pure reflection, kinds own ordering and
  inheritance — each rule in exactly one place.
- No observable CLI or public-API change: every touched type is `internal`;
  `README.md`, `docs/commands.md`, `docs/api-reference.md`, and `CONTEXT.md`
  are intentionally untouched (see report). ADR-0003 is not edited in place;
  its stale line-refs (`ShouldAutoPopulateEnumValues` `:472-492`,
  `ResolveValidValues` `:176-194`, `GetEnumValues` `:497-511`,
  `GetAllProperties` at `CommandReflectionCache.cs:223-228`) are superseded by
  this record.
- Contract pinned by caller-provided verification (not re-run here):
  `CliDescriptorCharacterizationTests.cs` 78/78 (walk parity, enum merge,
   override/hide, `NoDefaultValueMetadata` on all paths), full suite 901/901,
  plus `OptionDefaultTests.OmittedOption_NoDefaultValueMetadataOnDescriptor`
  (`OptionDefaultTests.cs:50-56`) pinning the member's absence while
  `OmittedOption_PreservesPropertyInitializer` (`:34-43`) pins the live
  initializer fallback.

> **Trim end-state reached (was: accepted IL2067 risk):** the preferred
> end-state is implemented — `Walk` is split into `Walk(Type)` (DAM `All`,
> full base-first `BaseType` chain) and `WalkDeclaredOnly(Type)` (DAM
> `PublicProperties | NonPublicProperties`, single-type declared-only walk)
> in `CommandReflectionCache.cs`. Full-walk callers (`Build`, both
> `FromCommandType` shims, the `CommandExecutor` service-injection scan) use
> `Walk`; both `FromDeclaredType` shims use `WalkDeclaredOnly`, whose
> annotation their own `PublicProperties | NonPublicProperties` annotations
> cover exactly. Both walk/shim `#pragma warning disable IL2067` sites are removed —
> zero suppressions remain on the walk/shim path (out of scope: the pre-existing
> test-CLI-helper `[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2067")]`
> at `src/ApplicationBuilderHelpers.Test.Cli/Commands/BaseCommand.cs:188`, which
> this ADR does not touch). Any future new declared-only caller must use
> `WalkDeclaredOnly` (not `Walk`); any widening of either method's reflected
> surface must widen its annotation to match.
