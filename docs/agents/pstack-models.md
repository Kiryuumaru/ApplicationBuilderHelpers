# Pstack Models (opencode)

Pstack skills speak in Cursor model slugs. Under opencode no model slugs exist — variations are agents, not models. This file maps each role to its opencode agent.

All pstack skill roles run on the parent chat model via `inherit-parent` (omit Task `model`).

When a skill mentions a role (e.g. "your configured how-explorer model"), use `inherit-parent`. The role runs on the parent chat model (omit Task `model`).

Scope: what library users do with the library, plus its guides. No per-role overrides exist in this repo.

## Cursor file superseded under opencode

`~/.cursor/rules/pstack-models.mdc` is the Cursor-only mapping from `/setup-pstack`. Under opencode this file supersedes it. Keep it — Cursor sessions still read it — but opencode sessions read here.
