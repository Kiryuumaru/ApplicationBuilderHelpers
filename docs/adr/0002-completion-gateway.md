# ADR-0002: CompletionGateway Extraction (Slot, Shadowing, Unknown-Shell Preserve)

Status: Accepted.

## Context

`CommandLineParser` handled the `complete` / `completions ...` pre-parse
gateway inline. The gateway must run after hierarchy build and before
help/parsing, and it shadows same-named registered commands.

## Decision

- Extract verbatim into internal sealed
  `CompletionGateway(ICommandBuilder, ConsoleOutput)`
  (`src/ApplicationBuilderHelpers/CommandLineParser/CompletionGateway.cs:17-19`),
  mirroring `HelpVersionGateway.cs:13-15`. No behavior change.
- Preserve pipeline slot: hierarchy build (`CommandLineParser.cs:65-66`)
  → gateway (`:68-70`) → bare `--help` (`:73`) → parse (`:80`) →
  post-parse version check (`:83-87`). Precedence: completion > help >
  parse > version.
- Preserve shadowing: `complete` and `completions install`/`uninstall`
  always handle (`CompletionGateway.cs:30-34,46-54`), so registered
  commands with those names never run (`:10-15`).
- Preserve unknown-shell asymmetry: `completions script <unknown>`
  returns `false` and falls through to the parse path (`No command
  found`, exit 2, `:213-215`), never the installer `Unknown shell`
  path, which belongs only to install/uninstall (`:218-230`).
- Preserve 4-way fall-through returning `false`: bare `completions`,
  `completions script` without shell, `completions script <unknown>`,
  `completions <unknown>` (`:36-37,:41-42,:56`).

## Consequences

- `CommandLineParser` stays a thin orchestrator; completion behavior is
  owned by `CompletionGateway` delegating to `CompletionEngine`,
  `CompletionScriptWriter`, and `CompletionInstaller`.
- Contract pinned by `CompletionGatewayTests.cs` (12 tests: precedence,
  4 fall-through edges, bare-`complete` handled vs bare-`completions`
  fall-through asymmetry, shadowing, unknown-shell preserve).
