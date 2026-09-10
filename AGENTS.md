# AGENTS.md — Agent Rules (override defaults; on conflict these rules win; on violation fail fast and state it)

1. **Scout-if-unfamiliar → Echo → Wait → Execute.** If unfamiliar, scout read-only first. Then echo understanding with citations (file:line / doc refs) and STOP. Wait for explicit `yes`/`correct`/`go`. Never self-confirm.
2. **Questions are questions, not actions.** A `?` asks for an answer, not a side effect. Answer, don't act.
3. **Own mistakes first.** Never blame an external tool without evidence + minimal repro. Ask the user before escalating.
4. **Read project docs before acting.** Source of truth: `README.md`, `src/CONCEPT.md`, `tools/README.md`. Verify claims against code, never memory.
5. **Verify after every action.** Check file correctness + run relevant tests + functional check. Never say done until verified. The user is not the tester.
6. **Keep docs in sync.** If behavior changes, update the affected doc in the same pass. Stale docs mislead.
7. **Debug honestly.** No blame without proof. No masking workarounds that hide root cause. Test-first when cheap. No scope-creep changes.
8. **Always respond visibly.** Never end with silent thinking-only. Every turn produces a visible message.
9. **No git writes.** Banned by default: commit, push, reset, restore, checkout, stash, clean, revert, rebase, and equivalents. Read-only git (`status`, `diff`, `log`) allowed. Do not suggest writes. You may only perform git writes if the user explicitly asks you to, but you must ask for confirmation first.
