# ADR 0019: Mixed-Root Positional Childless-Only Miss Gate (#631 follow-up)

**Status**: Accepted.

Supersedes the far-miss half of ADR-0014 (`0014-concrete-root-miss-gate.md`)
without editing it in place. ADR-0014, ADR-0008b (`0008b-positional-no-inherit.md`),
ADR-0009 (`0009-concrete-root-leading-help-first.md`), ADR-0011
(`0011-help-first-target-routing.md`), and ADR-0017
(`0017-leading-help-miss-uniformity.md`) stay frozen; their far-miss rows now
read through this ADR.

## Context

ADR-0014 kept a distance-coupled exemption on mixed concrete roots: a far miss
(no suggestion) bound the root positional (exit `0`) while a near miss threw
`No command found` (exit `2`). Routing therefore depended on
`DidYouMean.SuggestSubcommand` distance, so `zzzz` bound (exit `0`) while
`alph` rejected (exit `2`) on the same mixed shape.

## Decision

- **Childless-only exemption** (`ArgumentParser.cs`,
  `IsExemptRootPositional`): a root positional binds a bare word only when
  the root has no children (`Children.Count == 0`), alongside the existing
  root + implementation + dash/`--`/`/?`-form + help/version-token +
  position-0 + non-child guards. The `SuggestSubcommand` probe is deleted;
  `DidYouMean` no longer influences routing.
- **Gates `:155` and `:165-182` frozen in shape**: the zero-match gate and
  the concrete-root miss gate keep firing first; only the exemption
  predicate narrowed beneath them.

## Consequences

- On a mixed concrete root any bare pre-`--` non-child word exits 2 with
  `No command found` regardless of suggestion distance (`zzzz`, `alph`,
  `zzzz extra` fails on the first token, `--verbose zzzz`). `-- zzzz`
  still binds positionally (exit `0`); `--help zzzz` still exits 2;
  childless roots still bind bare words (exit `0`).
- Pinned by `RootPositionalBindingTests` (far-miss rows flipped to
  reject-2: `MixedRoot_DistantValue_RejectsUnknownCommand`,
  `MixedRoot_FarMissLeafName_RejectsUnknownCommand`,
  `MixedRoot_GlobalVerbose_BeforeMiss_RejectsUnknownCommand`; near-miss,
  help, sentinel, and childless rows unchanged).
