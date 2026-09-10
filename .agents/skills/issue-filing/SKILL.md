---
name: issue-filing
description: Use when reproducing a bug and filing it as a GitHub issue. Enforces scope-check, dedupe, template body, and fetch-back verification.
---

# Issue Filing

Reproduce a bug, prove it, and file it as a GitHub issue worth a maintainer's time. You file issues; you NEVER fix code.

## 1. Prime Directive

- MUST file only reproduced, in-scope, non-duplicate bugs. No repro, no issue. Out-of-scope findings are discarded, not filed. Duplicates get a comment on the existing issue at most.
- MUST verify every claim with tools — code behavior, scope status, existing issues.
- MUST NEVER fix code in this skill. Hand proven work to `triage-fix` / `disciplined-fix`.
- MUST NEVER invent issues to hit a quota. Zero issues is a valid outcome.

## 2. Repo Scope Gate

Per `scope-guard` Scope Test section: every reproduced finding MUST pass all six checks or be discarded with a one-line reason. MUST load `scope-guard` before filing

## 3. Hunt and Reproduce (One Feature at a Time)

1. MUST hunt one feature at a time within its documented scope (command registration, parsing, modules, configuration, middleware, help, exit codes). NEVER fuzz outside what the code claims to support.
2. For each suspicion, MUST write the smallest runnable PoC (xUnit test in `src/ApplicationBuilderHelpers.Test.Cli.UnitTest/`, or a standalone script under local-only `investigate/`).
3. MUST actually run the PoC with execution tools and capture the literal failure output. MUST confirm the failure is in the code under test, NOT in the harness or environment (import errors, fixture typos, missing SDK/secrets do NOT count).
4. If the PoC does not reproduce, MUST park the suspicion and move on — MUST NOT file.
5. MUST sanitize every PoC before filing: no secrets, tokens, real user data, or production URLs in public issues.

## 4. Dedupe Before Filing

Before opening ANY issue, MUST search the tracker (open AND closed) by:

1. Error message or stack-trace snippet.
2. Affected file or type name.
3. Symptom keywords.

MUST read the top candidates. If any covers the same root cause and conditions, MUST NOT open a new issue. MAY add a comment on the existing issue only if the new repro adds materially new information — NEVER "+1" spam.

## 5. Pre-File Verification

Tracker-green checks (PoC ran, scope passed, dedupe ran) are necessary but NOT sufficient. Before invoking the open-issue tool, MUST confirm and record all four:

1. **Reproduction is real** — PoC run in this session; captured output matches the symptom in the issue body.
2. **Scope verified** — all six `scope-guard` Scope Test checks pass.
3. **Not a duplicate** — open AND closed issues searched by error string, file path, and symptom keyword; zero matches.
4. **Not a placeholder** — body is complete sentences describing a real defect; no draft/stray/partial content.

If any check fails, MUST NOT file. Park the finding and continue.

## 6. File the Issue

Per `github-workflow` Issue Body Template section: use the canonical template there. MUST load `github-workflow` for the template

Filing deltas (unique to issue-filing):

- MUST describe the bug with feature/component in the title (e.g. `command: sub-command match prefers shorter prefix over exact name`) — NEVER low-information titles (`bug`, `crash`, `it doesn't work`, `please fix`)
- MUST apply `bug` plus severity and area labels — NEVER `security`-style labels on a public issue without checking for a private channel first (verified: this repo has no `SECURITY.md`)

## 7. Verify the Open

After calling the open-issue tool, MUST fetch the issue back by number/URL and confirm:

- Title matches what was submitted.
- Body is fully rendered (code blocks closed, no truncation, no broken markdown).
- Labels are applied and the issue is in the correct repo.

If anything is wrong, MUST fix it immediately — MUST NOT leave malformed issues on the tracker. MUST capture the final issue number and URL.

## 8. Local-Only Artifacts

`investigate/` is local-only working output invisible to issue readers. It is NOT tracked and MUST NEVER be staged, committed, or pushed.

- MUST NEVER reference local paths (`investigate/...`, session notes) in issue bodies or comments — they are dead noise to readers.
- MUST inline the PoC code as a fenced block and paste the literal failure output into `## Actual behavior`.
- MUST cite only tracked source (`src/...` file:line) for root-cause notes.
- MUST NEVER commit or push `investigate/`. MUST NOT use `git add -A` / `git add .` without first confirming `git status` is clean of it.

## 9. What NOT to Do

- NEVER file out-of-scope findings, feature requests dressed as bugs, or category mismatches.
- NEVER file unreproduced suspicions or duplicate issues.
- NEVER file issues against explicitly disclaimed behavior or unsupported runtimes.
- NEVER open public issues for security-sensitive findings when a private channel exists.
- NEVER paste secrets, tokens, real user data, or production URLs into public issues.
- NEVER write low-information titles.
- NEVER batch-open issues at session end — MUST file each as soon as it is reproduced and dedupe-checked.
- NEVER skip the fetch-back verification.
- NEVER fix bugs here — find, reproduce, file, move on.
- NEVER inflate severity — MUST be honest about reachability and real impact.
- NEVER report style issues or code smells — a bug is broken behavior.
