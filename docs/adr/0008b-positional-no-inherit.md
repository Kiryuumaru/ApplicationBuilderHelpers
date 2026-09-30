# ADR-0008: Positional Arguments Do Not Inherit (#543)

**Status**: Accepted.

## Context

`SubCommandArgumentInfo.DetermineInheritanceScope` auto-set `IsInherited = true` for six common positional names (`input`, `file`, `path`, `directory`, `target`, `source`, case-insensitive). A root-owned positional with one of those names therefore leaked into every leaf scope through the `AllArguments` walk (`src/ApplicationBuilderHelpers/CommandLineParser/SubCommandInfo.cs:101-115`, parent filter at `:110`), so `scopeleaf one` silently bound the root `target` value on the leaf instead of failing. `ValidateArgumentInheritance` (`SubCommandInfo.cs:271-294`, inherited-position scan at `:279-280`) likewise treated the leaked name as an inherited position. Uncommon names (e.g. `quasar`) never leaked, so behavior differed by name alone — a name-list tuning problem, not a scoping rule.

## Decision

Delete the common-name heuristic. `DetermineInheritanceScope` (`src/ApplicationBuilderHelpers/CommandLineParser/SubCommandArgumentInfo.cs:219-230`) now pins `IsGlobal = false, IsInherited = false` unless explicitly preset `true` before the call — positional arguments are per-command (leaf-local) by default, explicit opt-in only. Both call sites (`FromProperty` at `:96-119`, scope call at `:116`; `FromDescriptor` at `:126-146`, scope call at `:143`) flow through the narrowed method. The `AllArguments` walk (`:110`) and `ValidateArgumentInheritance` (`:279-280`) are untouched and inert-by-construction: with nothing ever auto-marked inherited, the `IsGlobal || IsInherited` parent filter matches nothing, so a common-name collision is two locals and no error.

## Options Considered

- **Chosen — delete heuristic, per-command positional by default**: one scoping rule for all names; `AllArguments`/`ValidateArgumentInheritance` need no edits (verified: no diff to `SubCommandInfo.cs`).
- **Rejected — tune the name list** (add/remove common names): preserves name-dependent behavior and re-arms the same leak for the next common word; rejected per #543 scope (no name-list tuning).
- **Rejected — per-consumer filters** ( Teach parse/bind/help to skip leaked nodes individually): scatters the scoping rule across merge sites instead of fixing it where the flag is set; rejected per #543 boundaries.

## Consequences

- A leaf invoked with a surplus token fails fast as a usage error: `scopeleaf one` exits `2` with `Unexpected argument 'one'` (`src/ApplicationBuilderHelpers/CommandLineParser/ArgumentParser.cs:261-280`), never silently binding the root value; leaf `--help` omits the root positional.
- Same-name root/leaf positionals are two independent locals (leaf-own binds leaf-local, exit 0); holds at 3-level depth (`chainmid leaf one` exits 2).
- Explicit opt-in preserved: a caller presetting `IsGlobal`/`IsInherited = true` before `DetermineInheritanceScope` still flows to child commands.
- Contrast with options: global options stay shared root-owned identity via the `AllOptions` ref-dedup view (ADR-0004, `SubCommandInfo.cs:72-96`); positionals are the opposite default — leaf-local unless opted in. Holding-scope (`OwnerCommand`/`BindTarget`) remains options-only.
- Contract pinned by `PositionalArgumentScopeTests.cs` (6/6, caller-provided; not re-run under docs-only restraint): root `target` P0 + leaf distinct class asserting leaf surplus exits 2 no-Fault, leaf `--help` omits `TARGET`, uncommon-name control, leaf-own same-name edge, 3-level chain surplus + help.
