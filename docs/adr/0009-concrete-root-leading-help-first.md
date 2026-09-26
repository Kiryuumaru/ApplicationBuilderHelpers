# ADR 0009: Concrete-Root Leading-Help-First (#559)

**Status**: Accepted (issue #559). Extends ADR-0007 without editing it in place.

## Context

ADR-0007 pinned help-first for the **abstract** root only: the help-first branch
required `!HasImplementation`
(`src/ApplicationBuilderHelpers/CommandLineParser/ArgumentParser.cs:53`), so it
never fired when a description-only `[Command]` merged at the root
(`Term` null, `SubCommandInfo.cs:146`) giving the root an implementation
(`HasImplementation` true at `SubCommandInfo.cs:133` — the test CLI's
`MainCommand` at `src/ApplicationBuilderHelpers.Test.Cli/Commands/MainCommand.cs:6-8`
+ `Program.cs:14`). On that concrete root a leading bare `--help`/`-h` fell
through to `ParseOptionsAndArguments` (`:85`), where the trailing token
(`false` surplus argument, `test` child name, `--bogus` unknown option) failed
exit `2` before `ShowHelp` ever rendered.

## Decision

- **Single-predicate widening, no parallel gate**: `IsConcreteRootLeadingHelp`
  (`ArgumentParser.cs:512-519`) — `IsRoot` + `argIndex == 0` + leading
  `IsHelpToken` (`--help`/`-h` only, `HelpVersionGateway.cs:26-29`) + no
  pre-`--` version token — widens the existing branch condition (`:53`) to
  `(!HasImplementation || IsConcreteRootLeadingHelp(...))`. The inner help check
  (`:55`), the globalization rule (`HelpFormatter.cs:40-43`, `IsRoot` alone maps
  to the global `COMMANDS:` model), and the fall-through validation order are
  untouched.
- **Leading-only, side-effect-free**: only a *leading* bare help token fires;
  non-leading help (`["--bogus", "--help"]` → `UnknownOption`, exit `2`),
  reserved forms (`--help=x`, `--helpful`), `-?` (not a help token), and
  post-`--` tokens stay on the normal parse path. Trailing tokens are ignored,
  never validated, never reach `Run` — `MainCommand.Run` never executes on the
  help path. Version beats help (`["--help", "--version"]` prints version),
  mirroring the abstract branch where the version check (`:47-51`) runs first.
- **Precedence unchanged**: completion > help > parse > version
  (`CommandLineParser.cs:67-82`: gateway → bare `--help` → parse → version
  check). The gate sits inside parse, strictly after completion and before
  trailing validation.

## Consequences

- `["--help", "false"]`, `["--help", "test"]`, `["--help", "--bogus"]`,
  `["-h", "false"]` on a concrete root render root/global help, exit `0`;
  `--help --help=x`, `--help --no-help`, `--help -- --bogus` likewise exit `0`.
  `["--help", "--verbose=banana"]` renders help (exit `0`) — the #542
  `InvalidValue` gate lives inside the abstract branch and never runs once
  leading help fires.
- `["--", "--help"]` on a concrete root stays exit `2` (surplus-argument error,
  not `RequiresSubcommand` — the gate scans pre-`--` tokens only and requires
  `argIndex == 0`).
- Docs same pass: `README.md` (exit row + bare-root note), `docs/getting-started.md`
  (topology note), `docs/advanced.md` (concrete-root matrix + carve-out scope +
  exit/footers/tokenizer line updates), `docs/commands.md` (Term `null` row +
  tokenizer/exit/precedence updates).
- Pinned by `RootRoutingDivergenceTests.cs` (4 tests:
  `ConcreteRoot_Help_WithTrailingValue_ShowsRootHelp`,
  `ConcreteRoot_Help_WithTrailingCommandName_ShowsRootHelp`,
  `ConcreteRoot_Help_WithTrailingUnknownOption_ShowsRootHelp`,
  `Leaf_Help_WithTrailingValue_ShowsLeafHelp`).
