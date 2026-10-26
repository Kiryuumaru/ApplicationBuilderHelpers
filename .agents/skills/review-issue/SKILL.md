---
name: review-issue
description: "Adversarially judge a GitHub issue against the docs and the code, then return exactly one verdict."
argument-hint: "GitHub issue number, URL, or pasted issue body"
disable-model-invocation: true
---

# Review issue

Judge one attached GitHub issue. The output is a verdict, not a fix.

## Before you start

- **Input**: the attached GitHub issue (number, URL, or body). If no issue is attached, ask for it. Never review from memory.
- **Repo ground rules**: read the project docs relevant to the touched area first, per `AGENTS.md`. To fetch an issue by number or URL, use read-only `gh issue view` (see `docs/agents/issue-tracker.md`). Do not mutate tracker state.
- **Repro**: reproduce in `/tmp/abh/<agent-name>` with a minimal generic project. Create the directory if missing. Never reproduce inside the repo under review.
- **Done when**: the output block is complete, every gate has PASS/FAIL, and exactly one verdict is chosen.

## Procedure

You review an attached GitHub issue. Your job is to JUDGE it, not to agree with it. Default stance: the reporter is confused until the evidence says otherwise. Most issues should end DOCS-NOTE or DECLINE, not ACCEPT.

Read the issue body, the docs pages it cites (or should cite), and the code it touches. Reproduce the claim yourself in `/tmp/abh/<agent-name>` with a minimal generic project — never trust the reporter's transcript alone.

REVIEW IN THIS ORDER. Stop at the first gate that fails.

### GATE 1 — CORRECT USE

Did the reporter follow the docs exactly? Quote the doc lines (path + line) for setup, usings, and that feature. If the repro skips a step, misuses an API, or pastes a snippet wrong — verdict DECLINE-MISUSE. Say the correct usage in one line.

### GATE 2 — DOC PROOF

Does the issue quote a doc line the behavior breaks? If the "Expected" is the reporter's habit (git-style, POSIX nostalgia, "should") with no doc quote — verdict DECLINE-NO-CONTRACT. Name what docs actually promise.

### GATE 3 — BOTH-SIDES TABLE

Write it:

- BUG: broken contract in 1 line (doc quote).
- BY-DESIGN: why a sane library does this in 1 line (POSIX / System.CommandLine / ambiguity with two legal readings / stability over time).
- DISCONFIRMING fact against the bug in 1 line.
- If by-design wins or ties → verdict DECLINE-BY-DESIGN + one-sentence docs note at most.

### GATE 4 — REVERSE-BUG TEST

State who breaks if we "fix" it, with an example input. If the fix mirrors the wrongness (value-vs-command, order-vs-order, spelling-vs-spelling) — verdict DECLINE-AMBIGUOUS. Ambiguous grammar is not a defect.

### GATE 5 — SIBLING SPELLINGS

Run = vs space vs short vs cluster vs order-swapped. If the complaint holds for one spelling only, it is a partial story — verdict NEEDS-NARROW-SCOPE (state the exact boundary) or DECLINE if siblings prove convention.

### GATE 6 — FIX SHAPE (only if gates 1–5 pass)

State the SMALLEST boundary: exact inputs affected, exact behavior change, what must NOT change. Forbid tree-coupled value parsing, order-dependent rules, and spelling-specific guards unless System.CommandLine does the same. If no narrow shape exists — verdict DECLINE-UNFIXABLE-AS-ASKED.

## Output

- **REPRO result**: your commands + outputs + exits (confirm or contradict reporter)
- **Gates 1–6**: PASS/FAIL each, 1 line why
- **VERDICT (one)**: ACCEPT-DEFECT (gates all pass, state narrow fix boundary) | DOCS-NOTE (one sentence) | DECLINE-MISUSE | DECLINE-NO-CONTRACT | DECLINE-BY-DESIGN | DECLINE-AMBIGUOUS | NEEDS-NARROW-SCOPE | DECLINE-UNFIXABLE-AS-ASKED
- **CONFIDENCE**: high / medium / low + what would flip it (1 line)

## Rules

- Never accept on transcript alone — always reproduce.
- Never accept "uniformity" without stability + coupling + standard checks.
- Never propose a guard broader than the proven case.
- Zero accepted is a fine outcome.
