# ADR 0017: Leading-Help Miss Uniformity + Named-Parent Help Typo (#654)

**Status**: Accepted (issue #654).

Supersedes nothing; extends ADR-0011 (`0011-help-first-target-routing.md`) and ADR-0014 (`0014-concrete-root-miss-gate.md`) without editing them in place.

## Context

ADR-0014 gated only far misses behind leading help on a mixed concrete root
(`HasImplementation` + children + position-0 positional): a near-miss word
returned null from `ClassifyHelpTrailingMiss` and fell through to
`ParseOptionsAndArguments`, where it bound as the root positional and rendered
global help (exit 0). A named grouping parent (`deploy`) with `--help` before a
typo dropped the typo and reported only the generic `RequiresSubcommand` text,
because the abstract `Unknown subcommand` throw is suppressed whenever a help
token is present.

## Decision

- **Uniform concrete-root leading-help miss** (`ArgumentParser.cs`,
  `ClassifyHelpTrailingMiss`): any bare non-child word behind leading help
  tokens throws `No command found '<token>'` (`UnknownCommand`, exit 2) with a
  pointer only when close. Hits still forward to target help; flag tails and
  the `--` sentinel are untouched.
- **Named-parent near-miss behind help** (`ArgumentParser.cs`,
  `ClassifyNamedParentHelpMiss`): after the abstract help-first probe fails to
  resolve, a near miss (suggestion exists) behind the parent's help token
  throws `Unknown subcommand '<token>'` with its pointer (`UnknownCommand`,
  exit 2). Far misses keep the subcommand list; hits forward to leaf help.

## Consequences

- `--help <near-miss>` on a mixed concrete root exits 2 with `No command found`
  plus a hint; `--help <far-miss>` is unchanged; `--help <hit>` still forwards
  (exit 0). `deploy --help <typo>` reports `Unknown subcommand` with a hint;
  `deploy --help <hit>` forwards to leaf help; reversed `typo --help` behavior
  is unchanged.
- ADR-0011/0014 stay frozen; their miss rows now read through this ADR.
- Pinned by `RootPositionalBindingTests.MixedRoot_Help_BeforeNearMiss_RejectsWithSuggestion`
  plus `DeployInterleavedOptionTests.Two_Help_BeforeNearMiss_ReportsUnknownSubcommand`
  and `Two_Help_BeforeHit_ForwardsToLeafHelp`.
