# ADR-0007: Reserved Help/Version Shorts `h` / `V` (#499)

Status: Accepted.

## Context

`-h` / `-V` win inside combined short clusters even mid-cluster
(`ArgumentParser.cs:307-338`; gateway ownership at
`HelpVersionGateway.cs:30-38`). Any local option declaring `ShortTerm`
`'h'` / `'V'` (e.g. `-h/--host` on `serve`) could therefore never bind —
`serve -hw` routes to help instead of `Host=w`. The shorts must be
reserved fail-closed at registration.

## Decision

- `CommandHierarchyBuilder.ValidateReservedShortNames`
  (`CommandHierarchyBuilder.cs:471-497`, called at `:460`) rejects
  `ShortName == 'h'` (unless `LongName == "help"`, `:487`) and
  `ShortName == 'V'` unconditionally (`:493`, no version-node
  exemption — version is gateway-only) with
  `InvalidOperationException` naming the option, command, and owner.
- Only the built-in `--help` owner may hold `-h`; `-V` is forbidden
  for all local options (no built-in version node exists).
- `serve --host` goes long-only
  (`ServeCommand.cs:12` — `[CommandOption("host", ...)]`, no short).

## Consequences

- Fail-closed registration: conflicts surface as faults (exit 1), never
  silently shadowed help/version routing.
- Cluster behavior unchanged: `-h` / `-V` still win mid-cluster.
- Docs: `docs/commands.md` + `docs/advanced.md` combined-shorts bullets,
  `docs/api-reference.md` `ShortTerm` note, `CONTEXT.md` glossary
  (`Reserved shorts`).
