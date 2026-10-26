---
name: bug-hunt
description: "Run a roundtable of expert subagents that each build a real app against a library, then synthesize repro-backed defects and declined items into one report."
argument-hint: "Library repo to hunt (path or name); blank uses this repo"
disable-model-invocation: true
---

# Bug hunt

Hunt defects in a library by making a roundtable of experts each build a real app end to end against it. You are the secretary. Spawn many more subagents like users on a roundtable — don't just hello-world test them. Default to 6–8 experts (plugin max 12), chosen for coverage.

## Target

- The target is the library repo named by the user, else the current repo. If neither is clear, ask before opening the roundtable.
- The target repo is read-only reference: no edits, commits, pushes, or tracker mutations.
- Experts build in `/tmp/abh/<agent-name>`, never inside the target repo.
- Read the target's docs for the planned feature slices before spawning; experts cite them by path + line.

## Roster

6–8 experts by default (plugin allows 2–12; the coverage bar in the roster list needs at least five), chosen for coverage, not the whole roster. At minimum: a builder, an adversarial bug hunter, a diagnostician, a docs/contract checker, and an independent verifier. Add performance, security, or frontend experts as the surface fits.

Give each expert a distinct app shape and feature slice, so findings are independent rather than N copies of one journey. A real app is a plausible small app with at least three documented features and one error path, actually run end to end — not hello world, not a compile-only check.

## Procedure

1. **Open and drive.** Open a roundtable with the roster agents as experts, and drive the roundtable state through the harness's tool-calling mechanisms (see Tool calling): `roundtable_open` -> `roundtable_set_round_agenda` -> spawn each expert -> read standings -> `roundtable_set_master_document` -> `roundtable_begin_round` -> `roundtable_set_round_agenda` -> spawn cross-examination -> read standings -> ... -> `roundtable_close`. You are the secretary.
   The agenda carries the shared brief, roster, rules, and current round; each expert's app shape and feature slice goes in that expert's own prompt, not the agenda. `roundtable_begin_round` sets `agenda_pending`, so set the new round's agenda before spawning or resuming any expert; it locks once any standing exists. Never stop before the roundtable is closed.
2. **Build.** Each expert builds a REAL app (not hello world) end to end in `/tmp/abh/<agent-name>`; the repo is read-only reference; actually exercise the documented features; file a standing reporting what they built, what broke with evidence, and what didn't.
3. **Defect rules.** Correct-use-first (cite doc line), minimal proof (command/output/exit/repro), bug-vs-by-design, reverse-bug test, red-team yourself; max 3 items, zero acceptable. Missing features, style preferences, and code smells are not bugs; only a broken documented contract with a repro counts.
4. **Rounds.** Round 0: each files their journey. Next round: experts cross-examine the findings and file GREEN/AMEND. Iterate until unanimous GREEN (or the 10-round cap; see the close gate), then close and report the outcome.

## Tool calling

Call `roundtable_*` through whatever tool-calling mechanism the harness exposes; never assume one harness's tool names or calling convention.

| Harness | Roundtable state | Spawn experts |
|---|---|---|
| OpenCode | script tool (`execute` / Code Mode) | `subagent` tool, exact agent name in `agent` |
| Task-style (e.g. Claude Code) | `roundtable_*` directly if exposed, else the harness tool bridge | task tool, `subagent_type: <agent name>` |
| Direct-tool / MCP | `roundtable_*` directly | the harness's subagent/task mechanism |

The expert name passed to `roundtable_open` must exactly match the spawn name. State calls and spawning may use different mechanisms; the script tool alone does not spawn experts.

## Expert prompts

Spawn each expert through the harness's subagent mechanism using exactly its roster name (see Tool calling). The prompt carries:

- the target repo path, the read-only rule, and the build directory
- its app shape and feature slice
- the defect rules and the standing format
- the current round and what to file

Before any filing, the expert reads the required state:

- round 0: the round agenda
- rounds 1+: the master document, the round agenda, and every peer's last standing

A filing without those reads is rejected.

## Expert standing

Each expert files via `roundtable_set_expert_standing` with an explicit `stance`, title, synopsis, and content: `SUBSTANCE` or `PENDING` in round 0; `GREEN` / `AMEND` / `PENDING` in later rounds. Omitting `stance` files `PENDING`. The content covers:

- what they built: app, features exercised, run evidence
- what broke: max 3 defect items, zero acceptable
- what did not break: coverage, not filler

Each defect item carries:

- severity (`Critical` / `High` / `Medium` / `Low`)
- doc line (path + line)
- minimal repro (command, output, exit)
- correct-use-first check
- bug-vs-by-design
- reverse-bug test
- self-red-team note
- confidence

One item per root cause; in rounds 1+, check peers' standings before filing a duplicate (round 0 is independent).

Unreproduced suspicions go in a separate "unreproduced" note — they are not findings and do not count toward the max 3.

## Rounds

- Each round starts with `agenda_pending`; call `roundtable_set_round_agenda` for that round before any expert files.
- **Round 0**: file `SUBSTANCE` (journey + findings) or `PENDING` (need clarification or confirmation from the secretary — file the question). Round 0 is blind: experts do not see peers' standings; read the round agenda first — it carries the shared brief, so keep peer findings out of round-0 prompts.
- **Round 1+**: cross-examine peers, re-run repros, then file `GREEN` (the master document, as written, fully reflects my position) / `AMEND` (I am filing a correction with evidence: exactly what should change and why) / `PENDING` (need clarification or confirmation from the secretary — file the question). No silent accepts; no `GREEN` without a re-run.
- At most 5 standings per expert per round; a `PENDING` expert that exhausts the cap is permanently blocked.
- Resume a stalled expert's same session rather than spawning fresh.

## Master document

After each round, resolve any `PENDING` expert first: answer its question, resume its same session, and have it re-file terminal (`SUBSTANCE` in round 0; `GREEN`/`AMEND` later) — the tool requires every expert terminal. Then re-read every expert's current standing via `roundtable_get_expert_current_standing` and synthesize ALL of them into the master document via `roundtable_set_master_document`; never synthesize from pre-resolution `PENDING` content. Start from the previous master; preserve all still-relevant content. If an expert is permanently blocked, `roundtable_close` with `force: true` is a last resort: report the unresolved gap, never fake `GREEN`.

Caps: standing content 16,000 chars, title 200, synopsis 560, round agenda 8,000, master document 32,000 — synthesize and compress evidence rather than pasting standings.

## Close gate

- Unanimous `GREEN`, or the 10-round cap with open `AMEND`.
- `roundtable_close` requires round ≥ 1, the master document set, and every expert terminal.
- At the 10-round cap with unresolved/permanently blocked `PENDING`, normal close is impossible; `roundtable_close` with `force: true` is the only exit. At the cap with open `AMEND`, normal close is allowed (`AMEND_AT_CAP`). Report unresolved items honestly and never fake `GREEN` or unanimity.

## Final report

- accepted defects grouped by severity, with evidence
- declined or withdrawn items with reasons
- apps built
- coverage gaps
- confidence

Synthesize; do not paste raw transcripts.

## Guardrails

- This skill hunts and proves; it does not fix. The target repo stays untouched.
- Zero accepted is a fine outcome; never manufacture defects.
- No mutating git/gh.
