# ADR-0002: CompletionGateway Extraction (Slot, Shadowing, Unknown-Shell Preserve)

Status: Accepted; amended by #485 (unknown-shell asymmetry superseded — see Amendment).

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

## Amendment (#485 — script unknown-shell now handled)

- Supersedes the preserve-asymmetry decision above: `completions script
  <unknown>` no longer returns `false` to the parse path. The script arm
  (`CompletionGateway.cs:39-45`) reuses the shared canonicalizer
  (`CompletionInstaller.TryCanonicalizeShell` at
  `CompletionInstaller.cs:85`; script call at `CompletionGateway.cs:198`)
  and on failure reports `Unknown shell '<shell>'. Expected bash, zsh,
  pwsh, or fish.` on stderr, exit `2` (`:200-201`) — handled (`true`),
  never parsed as a subcommand, never `No command found`.
- Symmetry restored: script unknown-shell now matches the
  install/uninstall `TryResolveShell` path (`:225-237`, canonicalize at
  `:228`) instead of diverging from it.
- Fall-through shrinks 4-way → 3-way: bare `completions`
  (`:36-37`), `completions script` without shell (`:41-42`),
  `completions <unknown>` (`:56`). `completions script <unknown>`
  is handled (`:43-44`).
- Original preserve-asymmetry text retained above as history.

## Consequences

- `CommandLineParser` stays a thin orchestrator; completion behavior is
  owned by `CompletionGateway` delegating to `CompletionEngine`,
  `CompletionScriptWriter`, and `CompletionInstaller`.
- Contract pinned by `CompletionGatewayTests.cs` (precedence,
  3 fall-through edges after #485 — bare-`completions`, `script` without
  shell, `completions <unknown>` — bare-`complete` handled vs
  bare-`completions` fall-through asymmetry, shadowing, script
  unknown-shell handled `true` / exit 2 / `Unknown shell` stderr).
