# AGENTS.md — Agent Rules (override defaults; on conflict these rules win; on violation fail fast and state it)

1. **Read project docs before acting.** Source of truth: `README.md`, `docs/*`. Verify claims against code, never memory.
1. **Keep docs in sync.** If behavior changes, update the affected doc in the same pass. Stale docs mislead.

## Agent skills

### Issue tracker

Issues live as GitHub issues. See `docs/agents/issue-tracker.md`.

### Triage labels

Default five canonical labels. See `docs/agents/triage-labels.md`.

### Domain docs

Single-context layout (`CONTEXT.md` + `docs/adr/`). See `docs/agents/domain.md`.

### Pstack models

Pstack per-role model choices map to opencode agents. See `docs/agents/pstack-models.md`.
