---
name: triage-fix
description: Use when triaging multiple related issues into one structural fix and one PR. Enforces shared-root-cause clustering, failing-tests-first, and verified PR linkage.
---

# Triage Fix

Turn a cluster of related issues into one structural fix and one PR. Default branch is `master` (verified via `git remote show origin`: HEAD branch `master`).

## 1. Prime Directive

- MUST ship one shared root cause, one structural fix, one PR that closes many issues.
- MUST write failing tests proving every executable-code bug BEFORE any fix exists. Docs-only fixes get docs verification — NEVER invented unit tests asserting prose.
- MUST diagnose the shared defect, fix it structurally (never duct tape), and prove it with a green suite.
- MUST link every issue with a closing keyword and verify the linkage actually registered.

## 2. Scope First

Per `scope-guard` Scope Test and Scope Sweep sections: every issue MUST pass all six checks or get an individual scope-citing close. MUST load `scope-guard` before clustering

## 3. Triage

1. MUST sync-check first with read-only commands only: confirm branch state against `master` (`git status`, `git log`), confirm a clean working tree (dirty tree → STOP and surface it, NEVER stash/discard unfamiliar changes), record the starting commit SHA. Pulling latest is a git write per root `AGENTS.md` rule 9 — MUST ask the user for explicit authorization BEFORE pulling.
2. MUST list ALL open issues (number, title, labels, age) and read each full body plus comments with tools — NEVER triage from titles alone.
3. MUST pick a cluster sharing ONE of: the same code path, the same violated contract/invariant, the same missing abstraction, or the same upstream defect through different callers. MUST verify the shared-root-cause hypothesis with code search before committing to it.
4. MUST reject false clusters: a folder is not a root cause, a label is not a root cause, a date range is not a root cause. If the shared cause cannot be stated in one sentence, MUST split into separate sessions.
5. MUST create a feature branch off `master` with a descriptive name (e.g. `fix/command-dispatch-race`).

## 4. Failing Tests First

Per `disciplined-fix` Failing-Test-First section: one failing test per executable-code issue BEFORE any fix. MUST load `disciplined-fix` for the loop

Cluster deltas (unique to triage):

- MUST place each test in the unit suite per `qa-regression` Placement section
- MUST run them and confirm each FAILS for the expected reason (assertion mismatch, exception, wrong value) — NOT for import errors, fixture typos, or environment issues
- MUST commit the failing tests separately (`test: add failing regression tests for <cluster>`) BEFORE writing the fix — the failing-tests commit is the clean revert checkpoint
- MUST name tests as normal regression coverage — NEVER `Issue42ReproductionTest`, NEVER issue numbers in test source (issue linkage lives in the PR body, not test names)
- MUST record the docs verification used (build, lint, link-check) for docs-only issues instead of a test

## 5. Root-Cause Analysis

- MUST trace each failing test from entry point to failure point with code search and file reading, and record the call chain with file:line evidence.
- MUST find the convergence point where the failure chains meet — that is the shared root cause (flawed function, producer/consumer contract disagreement, missing invariant check, missing abstraction, over-permissive type, unconsidered boundary).
- MUST apply Chesterton's Fence: use `git log`/`git blame` to understand why the code exists this way. If a deliberate reason (constraint, prior bug, documented trade-off) exists, the fix MUST respect it or explicitly supersede it with justification.
- MUST state the violated contract in one sentence: "X is supposed to be Y, but under condition Z it becomes W."

## 6. Structural Fix

- MUST make the minimum coherent change that fixes the shared root cause for the whole cluster — NEVER per-issue special-case branches.
- MUST reject every duct-tape pattern per `disciplined-fix` Anti-Duct-Tape catalog in real time. If tempted, MUST stop and choose the structural alternative.
- MUST follow existing project patterns and layering (templates and harness depend on core; core depends only on declared packages plus `System.*`). MUST NOT expand scope into unrelated cleanup — record other bugs for a separate session.
- MUST enter behavior-preservation mode for restructures: frozen public surface unchanged, all pre-existing tests untouched and passing.
- MUST build (`dotnet build`, 0 warnings/0 errors) and test (`dotnet test`, 100% pass) after the fix. Every previously failing test MUST now pass. MUST NOT modify, skip, or delete a pre-existing test to get green — a regressing test means the fix broke behavior.
- If the same approach fails 3 times after clean reverts, MUST stop and re-examine the RCA — repeated fix failures mean the diagnosis was wrong, not the implementation.

## 7. Done Verification

Per `disciplined-fix` Definition of Done section: all four checks evidenced per issue before opening the PR — MUST load `disciplined-fix` for the bar

## 8. PR and Linkage

Per `github-workflow` Closing-Keyword Linking and PR Body Format sections: keyword block, summary, root cause, approach, testing, verified linkage. MUST load `github-workflow` for the discipline

Cluster deltas (unique to triage):

- MUST title the PR after the cause, not the symptoms (e.g. `fix(commands): enforce registration invariant on duplicate sub-command` — NEVER `Fix #12, #18` or `misc fixes`)
- MUST commit the failing-tests-plus-fix progression on the feature branch per `github-workflow` Branch and Commit Discipline section
- MUST show the user the PR URL plus the verified linked-issue list with test evidence

## 9. What NOT to Do

- NEVER start without syncing `master` and confirming a clean tree.
- NEVER cluster by folder, label, or date — only by shared root cause.
- NEVER write the fix before the failing tests exist and are committed.
- NEVER invent unit tests asserting documentation content.
- NEVER apply duct-tape patterns or per-issue special cases.
- NEVER modify a pre-existing test to make the suite green.
- NEVER expand scope into unrelated refactoring.
- NEVER open a PR with a vague title or unlinked issues.
- NEVER assume linkage worked — MUST verify both directions.
- NEVER push to `master`, force-push shared history, or bypass hooks.
- NEVER bulk-close issues or close ambiguous ones.
- NEVER leave `TODO`, `FIXME`, or placeholder code.
