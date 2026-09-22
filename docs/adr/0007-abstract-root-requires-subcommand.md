# ADR 0007: Abstract-Root Requires-Subcommand + Help-First + Term Guard (#513)

**Status**: Accepted (issue #513).

Supersedes nothing; extends the exit-code contract (`RequiresSubcommand`, exit 2) and the help-precedence notes without editing earlier ADRs in place.

## Context

A CLI registering only leaf subcommands (e.g. only `[Command("greet", ...)]`) has no root implementation: the root `SubCommandInfo` is abstract (`IsRoot` at `src/ApplicationBuilderHelpers/CommandLineParser/SubCommandInfo.cs:130`). Before this change the bare-run path, the leading-`--help` path, the root display name, and `Term` hygiene were unpinned:

- Bare `[]` had no documented contract (which display name, which footer).
- `["--help", "greet"]` risked rendering a command-scoped view instead of global help.
- `CommandAttribute.Term` allowed empty/whitespace/dash-led names to reach the hierarchy.
- Multi-space terms (`"config  hub"`) had no pinned normalization shared by both call sites.

## Decision

- **Bare root → exit 2 naming `'<root>'`**: no-implementation + children throws `RequiresSubcommand` (exit 2) with `'<root>' requires a subcommand. Available subcommands: ...` (`src/ApplicationBuilderHelpers/CommandLineParser/ArgumentParser.cs:71-83`). Display name `"<root>"` lives at `SubCommandInfo.cs:32` (`ToString()` at `:301`); the structured `CommandException.CommandName` stays empty for root (`ArgumentParser.cs:73`) so the footer is the two-sentence global usage footer (`src/ApplicationBuilderHelpers/Exceptions/CommandErrorFooter.cs:20-23`).
- **Help-first carve-out (root-only globalization)**: `(IsRoot || argIndex > 0)` + help token pre-`--` sets `ShowHelp` without erroring, exit 0 (`ArgumentParser.cs:64-70`). Only `IsRoot` maps to the **global** model (`COMMANDS:` section): `HelpFormatter` branches on `IsRoot` alone (`src/ApplicationBuilderHelpers/CommandLineParser/HelpFormatter.cs:42-44`), so a named abstract parent keeps its parent-scoped help (`BuildCommandModel`, e.g. `["config", "hub", "--help"]` renders `"Spaced hub."`). Zero-match first tokens (`["bogus", "--help"]`) still error `UnknownCommand` (exit 2) via the pre-check at `ArgumentParser.cs:40-48`; the `--` sentinel blocks the carve-out (`["--", "--help"]` keeps `RequiresSubcommand`).
- **Term validation contract (build fault, exit 1)**: null `Term` merges at root; non-null empty/whitespace throws (`term must not be empty or whitespace`); any dash-led part throws (`command names must not start with '-'`, per-part `Ordinal`); multi-space normalizes via `Split(' ', RemoveEmptyEntries)` so abstract-base matching (`config  hub` ≡ `config hub`) agrees. Enforced at both `SubCommandInfo.FromCommand` (`SubCommandInfo.cs:140-159`) and the hierarchy build (`src/ApplicationBuilderHelpers/CommandLineParser/CommandHierarchyBuilder.cs:82-88,193-204`), including the abstract-base walk.

## Consequences

- Rootless CLIs fail loudly and helpfully on bare runs; leading `--help` at the root renders global help instead of erroring (named abstract parents keep their parent-scoped view).
- Invalid terms fail fast at build (fault, exit 1) instead of corrupting the hierarchy — never a usage error.
- Docs same pass: `README.md` (exit/pipeline), `docs/getting-started.md` (topology note), `docs/advanced.md` (Bare-root subsection + Help rewrite + footers), `docs/commands.md` + `docs/api-reference.md` (Term contract), `CONTEXT.md` (abstract root, bare run, help-first, Term validation).
- Pinned by `AbstractRootRequiresSubcommandTests.cs` (13 tests: bare-root + global footer, help-first ×2, unknown-with-help, separator-keeps-requires, null/empty/whitespace/tab/dash guards, double-space normalization, spaced-base match, dash-led build fault).
