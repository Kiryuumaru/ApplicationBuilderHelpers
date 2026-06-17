---
applyTo: '**'
---
# Agent Terminal Rules

## Prohibited in Terminal Commands

- NEVER use pipe `|` in terminal commands (triggers VS Code approval prompt)
- NEVER use semicolon `;` in terminal commands (triggers VS Code approval prompt)
- NEVER use `&&` in terminal commands (triggers VS Code approval prompt)
- NEVER use redirections `>`, `2>&1` in terminal commands (triggers VS Code approval prompt)

---

## Required Approach

Execute each command in a separate `run_in_terminal` call.

- Instead of `dotnet build; dotnet test`, USE two separate terminal calls
- Instead of `command | Select-String`, USE single command and let output truncate
- Instead of `git status && git add .`, USE two separate terminal calls

---

## Filtering

- USE tool-native filtering instead of pipes
- USE `dotnet test --filter "FullyQualifiedName~TestName"`
- USE `grep` with `--include` patterns
- USE command-specific filter flags

---

## Priority

This rule takes precedence over efficiency. Multiple separate commands are always preferred over chained commands.
