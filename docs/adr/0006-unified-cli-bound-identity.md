# ADR 0006: Unified CLI-Bound Identity + Cached Injection Plan (#488)

**Status**: Accepted (issue #488).

Supersedes nothing; extends ADR-0004 (descriptor-walk-enum-unification,
which pinned the duplicate-preserving walk) without editing it in place.
ADR-0003's parser-layer policy stands unchanged.

## Context

`CommandReflectionCache.Build` (options/arguments snapshot at
`CommandReflectionCache.cs:131-162`, bound-gated via `IsCliBound` at
`:140-143`) and `ServiceInjectionGate`
(`ServiceInjectionGate.cs:40-126`) disagreed on what "CLI-bound" means:
the cache read attribute identity off each walk `PropertyInfo` while the
gate compared NAME strings against the per-run `AllOptions`/`AllArguments`
bound set (pre-fix `ServiceInjectionGate.cs:104-112`). Under member hiding
(`new`), `Walk` keeps both entries as base-first duplicates
(`CommandReflectionCache.cs:229-248`, pinned by
`Options_HiddenMember_CharacterizesCurrentWalk`), so a NAME-string check
could miss the hidden entry the identity check caught — or vice versa.
The injection plan also mirrored the descriptor cache's double-checked
lock instead of sharing it (pre-fix `ServiceInjectionGate.cs:44-98`).

## Decision

- **One canonical predicate**: `CommandReflectionCache.IsCliBound`
  (`CommandReflectionCache.cs:212-216`), owned next to `Walk` — a property
  is CLI-bound iff it carries `[CommandOption]` or `[CommandArgument]`.
  Both the reflection cache (`:131-162`) and the injection plan
  (`ServiceInjectionGate.cs:51-109`) call it; no second NAME-string
  comparison at inject time.
- **Always-error on dual-marked**: any dual-marked `PropertyInfo` (CLI
  marker plus `[FromServices]` / `[FromKeyedServices]`) anywhere in the
  walk chain throws `InvalidOperationException` from the gate's single
  fail-fast point (`ServiceInjectionGate.cs:133-139`, exact historical
  message preserved) — fault, exit 1, never a usage error. Member hiding
  (`new`) never excuses the conflict: the walk keeps hidden members as
  duplicates and the hide breaks attribute inheritance, so a same-name
  conflict spread across two entries (base CLI + derived service, or vice
  versa) throws via the hoisted bound set (`:53-70`) as well as the
  per-`PropertyInfo` check (`:87-90`).
- **Cached injection plan via a shared core**: `TypePlanCache<TValue>`
  (`TypePlanCache.cs`) owns the double-checked lock plus the per-kind
  `BuildCount`; both the per-builder reflection descriptors
  (`CommandReflectionCache.cs:107-125`, `BuildCount` at `:109-112`) and
  the global injection plan (`ServiceInjectionGate.cs:44-48`) share it via
  annotated method-group factories (`GetOrAdd(commandType, Build)`), so
  the `All`-annotated type flows via the annotated `PlanFactory<TValue>`
  delegate hop (method-group delegate creation reports IL2111, suppressed
  explicitly at each call site — the target is statically referenced,
  never reflection-invoked by name; a dedicated
  `PlanFactory<TValue>` delegate carries the annotation — generic
  `Func<Type, TValue>` cannot). `BuildCount` stays per-kind because each
  kind owns its `TypePlanCache` instance (DCL, not `Lazy<T>`).
- **Marker shim preserved narrowly**: matching stays by attribute simple
  name (`ServiceInjectionGate.cs:24-30,75-77` — reuse-only seam, no new
  library dependency) with the key read from attribute metadata
  (`:146-174`).

## Consequences

- One identity, two caches, zero NAME-string drift: hiding, override,
  and plain properties all resolve bound/unbound the same way in both
  layers.
- Docs same pass: `CONTEXT.md` (member-hiding vs gateway shadowing,
  dual-marked, injection plan), `docs/commands.md` (canonical predicate,
  always-error rule, cached-plan lifetimes, dual-marked example),
  `docs/advanced.md` + `docs/api-reference.md` (one-line cache-lifetime
  notes).
