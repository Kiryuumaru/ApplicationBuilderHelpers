# ADR 0014: Concrete-Root Miss Gate + Childless-Only Positional Exemption (#631)

**Status**: Accepted (issue #631).

Supersedes nothing; extends ADR-0008b (`0008b-positional-no-inherit.md`), ADR-0009 (`0009-concrete-root-leading-help-first.md`), and ADR-0011 (`0011-help-first-target-routing.md`) without editing them in place.

## Context

A concrete root (own run plus children) had no miss wording of its own: a bare non-child first word fell to the `RequiresSubcommand` guard, and the root positional exemption could bind a bare word on a root with children. Behind leading help, far and near misses shared one path, so `--help <far-miss>` forwarded instead of failing.

## Decision

- **Miss before requires-subcommand** (`ArgumentParser.cs:73-90`): first bare pre-`--` non-child token on a concrete root throws `No command found '<token>'` (`UnknownCommand`, exit 2), suggestion appended only when close. The zero-match gate (`:61-71`) keeps firing first at `argIndex == 0`.
- **Childless-only exemption** (`:924-934`): a root positional binds a bare word only when the root has no children; mixed roots check the miss first.
- **Far-miss-throws behind leading help** (`:97-98`, `:936-956`): a far miss (no suggestion) behind leading help tokens throws `No command found` before forwarding; a near miss returns null and keeps the legacy `Unknown subcommand` wording via the normal path; hits forward to target help (`:959-982`, gated hit-only).

## Consequences

- `zzzz`, `--verbose zzzz`, `--help zzzz` on a mixed concrete root exit 2 with `No command found`; `--help <valid-sub>` still forwards to target help (exit 0). ADR-0008b/0009/0011 stay frozen; their miss rows now read through this ADR.
- Pinned by `RootPositionalBindingTests` (distant bind 0→2, reject-2 rows, forward-0 row) + `RootRoutingDivergenceTests` (`--help false` → `No command found`).
