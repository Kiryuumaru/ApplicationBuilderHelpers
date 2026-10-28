# ADR 0023: Version Transparent to Live Help Target Resolution (#761)

**Status**: Accepted (issue #761).

Supersedes nothing; extends ADR 0010 (`0010-group-unknown-invalid-beats-version.md`),
ADR 0012 (`0012-symmetric-help-version-forgiveness.md`), and ADR 0017
(`0017-leading-help-miss-uniformity.md`) without editing them in place. ADR 0009,
ADR 0011, and ADR 0013 stay frozen: their "version beats help" lines
(`0009:37-38`, `0011:14`, `0013:21`) read through ADR 0012 (`0012:14`), and this
record reads ADR 0011's dash-led-next-token row (`0011:13`) only for exact
`--version`/`-V` tokens when help is live on a grouping node. ADR 0011 still
applies to every other dash-led token.

## Context

On a grouping node (a named grouping parent such as `config`, or an unnamed
abstract root), a live pre-`--` help token plus an exact version token
(`--version`/`-V`) could change the help outcome. `ResolveHelpTargetCommand`
skipped only help tokens, so a version token was a dash-led breaker that
returned the current target. `ClassifyNamedParentHelpMiss` likewise stopped at
the version token, and the abstract version short-circuit ran before the help
block. Help therefore rendered group help and dropped the surplus, so
`config --help --version bogus` and `config --version --help bogus` exited `0`
where `config --help bogus` exited `2`, and `config --help --version get`
rendered group help instead of `get` help. A version prefix also blocked the
leaf path: `config --version get mykey --help` treated `mykey` as a surplus
before the leaf could bind it.

Two leaf cases sit on the same boundary. A dangling valued option beside leaf
help normally clears help and fails validation. When the version token was
skipped on the grouping path, that request must stay available to the leaf
decision so help can still win. Separately, a required valued option can consume
a following bare help token as its value, so that token is not a live request;
a separate version request must still be able to win. The implementation
therefore keeps two facts about the parse: the version request was skipped on the
path, and a help token was consumed as a value.

ADR 0012 (`0012:14`) already decided that help outranks version. This decision
makes the grouping help path follow that precedence without widening any other
path.

## Decision

- **Boundary: live help path, with a grouping-only prefix skip.** The path-walk
  prefix skip applies only when the parser is on a grouping node (no
  implementation, at least one child), meets an exact `--version`/`-V` token,
  and a live help token also occurs before the `--` sentinel. The tokens may
  appear in either order, with path tokens between them. That prefix skip is
  grouping-node-only. Help-target resolution and surplus classification
  separately skip exact version tokens anywhere in the live help path, including
  after the leaf name: `config --help get --version extra` exits `2` with
  `Unexpected argument 'extra'`, matching `config --help get extra`, and
  `config --help get --version` shows `get` help, matching
  `config --help get`. "Beside" is not the boundary; co-occurrence in the
  pre-`--` segment is. Tokens after `--` are positional and stay outside the
  rule. `--version=x`, `-V=x`, `-version`, and `-vV` clusters are not exact
  reserved tokens and keep their existing rules.
- **Path walk skips the reserved token.** When the walk is on such a grouping
  node and a help token appears later before `--`, the exact version token is
  skipped (`ArgumentParser.cs:29-38`). The walk records `ShowVersion` and
  `VersionSkippedOnPath` (`ParseResult.cs:15-16`) and continues into child
  commands. A version prefix therefore no longer stops the parser before a
  leaf. This preserves leaf positionals: `config --version get mykey --help`
  reaches `config get`, binds `mykey` to its optional positional, and shows
  `get` help, matching `config get mykey --help`. The same holds for
  `config --version get extra --help` and
  `config --version set mykey myval --help`. The skip also keeps the version
  request available for the leaf validation and forgiveness decision.
- **Leading version prefixes are transparent to abstract-root misses.** On an
  abstract root, leading exact version tokens before a bare miss are treated as
  transparent by the root miss gate (`ArgumentParser.cs:91-108`), so
  `--version bogus --help` matches the help-only miss. A concrete root keeps its
  own leading-help rules.
- **Help resolution and miss classification skip the reserved token.**
  `ClassifyNamedParentHelpMiss` skips exact version tokens before and after the
  child walk (`ArgumentParser.cs:1162`, `:1166`). `ResolveHelpTargetCommand`
  skips them in its leading scan (`:1186`) and inside the child walk
  (`:1196-1199`), including after the resolved leaf, and excludes them from
  surplus classification (`:1210-1223`). So
  `config --help get --version extra` exposes `extra` as a surplus, while
  `config --help get --version` resolves to `get` help.
  `HelpVersionGateway.IsVersionToken` (`HelpVersionGateway.cs:34-37`) is the
  exact-token test. Other dash-led tokens still take the `StartsWith('-')`
  breaker (`ArgumentParser.cs:1201-1202`); this decision does not make them
  transparent.
- **Grouping precedence: help wins; the version-only carve-out stays.** The
  abstract version short-circuit fires only when the pre-`--` tail has a version
  token and no help token (`ArgumentParser.cs:171-180`). With help present, the
  help and miss gates decide first, which matches ADR 0012's
  help-outranks-version rule. The version-only carve-out stays:
  `config --version bogus` still prints the version (exit `0`), and
  `config --version -- --help` still prints the version (exit `0`). This is the
  ADR 0010/#768 behavior, unchanged.
- **Leaf version-forgiveness retention.** On a command with its own run, a
  dangling valued option beside help normally clears help and exits `2`
  (`CommandLineParser.cs:75-76`, `:156-157`). With no version token,
  `config get --format --help` exits `2` with
  `Missing value for option: --format`. When `VersionSkippedOnPath` is set, the
  parser instead leaves `ShowHelp` live for that leaf, so
  `DecideValidationForgiveness` returns `ForgiveHelp`
  (`HelpVersionGateway.cs:80-88`) and `ShowCommandHelp` renders the resolved
  leaf (`CommandLineParser.cs:81-84`). `config --version get --format --help`
  and `config -V get --format --help` exit `0`, leave stderr empty, and write
  stdout byte-identical to `config get --help`. The page is `config get` leaf
  help, never group help. The version request is retained, but help still wins.
- **Consumed-help rule.** A required valued option on a command with its own run
  consumes a following bare help token as its value (`ArgumentParser.cs:301-302`,
  `:347-352`; combined short cluster `:548-551`, `:566-570`) and records
  `HelpTokenConsumed` (`ParseResult.cs:15`). That help token is not live.
  Without a separate version request, the consumed token is data, not a live
  help request. A string-valued required option executes and binds the token: on
  a grouping node with a leaf that requires a string-valued `--name` option,
  `app group leaf target --name --help` exits `0` with `name="--help"`. A typed
  required option still validates and converts the consumed token, so it may
  exit `2` with `Invalid value '--help'` instead of executing. With a separate
  live `--version`/`-V` request before `--`, version wins over the consumed token
  and the command does not execute:
  `app group --version leaf target --name --help` prints the version.
  `HelpVersionGateway.cs:85` permits version forgiveness when
  `HelpTokenConsumed` is set even though the raw tail still contains a help
  token. Pass flag-looking values explicitly with the `=` form
  (`--name=--help`).
- **Scope of the guarantee.** For exact reserved-token cases in a live help
  path, skipping version tokens leaves the help target and the miss/surplus
  classification at their help-only result. This includes exact version tokens
  after the leaf name inside that scan. This is the whole guarantee. It does not
  promise parity for any other dash-led token or for option-interleaved forms.
- **Outside the boundary, existing rules stay.** Concrete-root leading help,
  leaf option parsing beyond the help-target and surplus scans above, `=`-form
  misuse (`--help=x`, `--version=x`), unknown/misuse errors, and the `--` sentinel
  keep their existing rules. The path-walk prefix skip is grouping-node-only;
  help-target/surplus scanning still skips exact version tokens inside a
  resolved leaf path.

## Consequences

- `config --help --version` and `config --version --help` show group help
  (exit `0`). `config --help --version get` and `config --version --help get`
  show `get` help (exit `0`).
- `config --help --version bogus` and `config --version --help bogus` exit `2`
  with the same `'config' requires a subcommand. Available subcommands: ...`
  error as `config --help bogus`.
- On an abstract root, `--version bogus --help` matches `bogus --help` in error
  text and exit code.
- `config sett --help` and `config --version sett --help` (likewise `-V`) exit
  `2` with the same `RequiresSubcommand` text, subcommand list, and
  `Did you mean 'set'?` pointer.
- `config --help sett` and `config --help --version sett` keep the same
  `Unknown subcommand 'sett'` pointer. Hit-plus-extra rows keep
  `Unexpected argument`.
- `config --help get --version extra` and `config --help get -V extra` exit
  `2` with the same `Unexpected argument 'extra'` text as
  `config --help get extra`. `config --help get --version` and
  `config --help get -V` show `get` help, matching `config --help get`.
- `config get mykey --help`, `config --version get mykey --help`,
  `config get extra --help`, `config --version get extra --help`,
  `config set mykey myval --help`, and
  `config --version set mykey myval --help` all exit `0` and show the same leaf
  help for the same leaf command.
- `config get --format --help` exits `2` as
  `Missing value for option: --format`. `config --version get --format --help`
  and `config -V get --format --help` exit `0` and show `config get` leaf help,
  not group help.
- A required valued option on a command with its own run consumes a following
  bare help token. Without a separate version request, the consumed token is
  data, not a live help request. A string-valued required option runs with that
  token as the value; a typed required option still validates and converts it
  and may exit `2` with `Invalid value ...` instead of running. With a separate
  live `--version`/`-V` request before `--`, the version prints and the command
  does not run.
- `config --version bogus` still prints the version (exit `0`).
  `config --version -- --help` still prints the version (exit `0`).
- A concrete root is outside this rule. Its leading-help and miss behavior keep
  their existing results; do not infer grouping-node parity there.
- `--help=x` / `--version=x` still exit `2` as `InvalidValue`; unknown options
  and misuse still exit `2`.

### Known limitation: non-reserved dash-led tokens

Only exact `--version`/`-V` tokens are transparent. A non-reserved dash-led
token still acts as a dash-led breaker when help-target resolution reaches it.
Option-interleaved inputs are therefore outside the guarantee; #761 does not
claim parity for them.

## Read-through

- ADR 0009 (`0009:37-38`) says version beats help for `["--help",
  "--version"]`. ADR 0012 (`0012:14`) supersedes that precedence; help now wins.
  The concrete-root miss rows are otherwise unchanged.
- ADR 0011 (`0011:14`) says version still beats help. ADR 0012 supersedes it.
  ADR 0011's dash-led-next-token row (`0011:13`) still holds for non-reserved
  dash-led tokens; this record overrides it only for exact version tokens when
  help is live before `--` and the help path starts on a grouping node.
- ADR 0013 (`0013:21`) says version still beats help via the unchanged version
  gate. ADR 0012 supersedes that line; the rest of ADR 0013 stays.
- ADR 0010 (`0010:14`, `0010:20`) and #768 keep the version-only surplus
  carve-out. This record leaves that carve-out intact. The grouping help path
  uses help precedence, and the consumed-help case keeps version forgiveness
  when the help token is not live.

## Docs and tests

Implementation: `ArgumentParser.cs` (grouping-path skip, help target and miss
scans, consumed token), `CommandLineParser.cs` (dangling-help gate and help
rendering), `HelpVersionGateway.cs` (exact-token tests and forgiveness), and
`ParseResult.cs` (`HelpTokenConsumed`, `VersionSkippedOnPath`).

- Current behavior lives in `docs/advanced.md#help-system`,
  `docs/commands.md#exit-contract`, `docs/api-reference.md`, and `CONTEXT.md`
  ("Help"), then this ADR.
- Docs same pass: `docs/advanced.md` (help-system precedence, dangling and
  consumed-help rules, non-reserved scope), `docs/commands.md` (grouping-parent
  help and exit contract), `docs/api-reference.md` (error/exit gate),
  `CONTEXT.md` (Help), and `CHANGELOG.md`.
- Pinned by `HelpVersionSurplusTests` and
  `HelpVersionSurplusAbstractRootTests`
  (`src/ApplicationBuilderHelpers.Test.Cli.UnitTest/HelpVersionSurplusTests.cs`):
  both token orders against help-only controls, group far-miss/near-miss/
  hit-plus-extra rows, leaf positional binding with and without a version
  prefix, typo-before-help parity, the abstract-root rows, and the version-only
  and separator carve-outs. `HelpTokenFallbackConsumptionTests`
  (`:215-240`) pins the consumed-help rows, and
  `ConsumedHelpVersionTokenTests` pins the six consumed help/version tokens.
  `RequiredDateOptionHelpConsumptionTests` (`:33-40`) pins the typed conversion
  failure: a required `DateOnly` option consuming `--help` exits `2` with
  `Invalid value '--help'` and does not run.
