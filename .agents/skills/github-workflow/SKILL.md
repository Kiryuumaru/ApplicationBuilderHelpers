---
name: github-workflow
description: Use when opening issues, authoring PRs, or managing tracker linkage on GitHub. Enforces keyword discipline, verification, and branch hygiene.
---

# GitHub Workflow

Shared tracker discipline for `issue-filing` and `triage-fix`. Default branch is `master` (verified via `git remote show origin`: HEAD branch `master`).

## 1. Closing-Keyword Linking (`Fixes #N`)

GitHub auto-closes linked issues on merge ONLY when the PR body uses a recognized closing keyword with a `#N` reference, one per line, in the PR description.

- MUST use `Fixes #N` as the house style (`Closes`, `Resolves` also work; case-insensitive).
- MUST put one keyword plus `#N` on its own line per issue:

  ```markdown
  Fixes #12
  Fixes #34
  Fixes #56
  ```

- MUST place keywords in the PR description — NOT only in commit messages, NOT inside fenced code blocks, blockquotes, or HTML comments (GitHub ignores them there).
- MUST target the same repo (cross-repo references do NOT auto-close) with the repo default branch (`master`) as the PR base.
- MUST NEVER write comma-separated tails (`Fixes #12, #34` links only `#12`), `Also closes #34` ("Also" is not recognized), or bare mentions (`see #12`, `related to #12` do NOT link or auto-close).
- MUST NOT expect draft PRs to auto-close — only on mark-ready and merge.

## 2. PR Body Format

Every fix PR MUST open with the keyword block, then summary, root cause, approach, and testing:

```markdown
Fixes #12
Fixes #34

## Summary
<One paragraph: what changed and why.>

## Root cause
<The shared invariant/contract the bug cluster violated.>

## Approach
<The structural fix — why it is not duct tape.>

## Testing
<Failing tests before + full suite after for code changes;
 docs build/lint/link check or `Not run (docs-only)` for docs-only PRs.>
```

## 3. Issue Body Template

MUST follow the repo's `.github/ISSUE_TEMPLATE/` when one exists. Verified: this repo has none (`.github/` holds only `dependabot.yml` and `workflows/`), so MUST use this default:

```markdown
## Summary
<One sentence.>

## Steps to reproduce
1. <step>
2. <step>
3. <step>

## Expected behavior
<What should happen, citing the doc/source that promises it.>

## Actual behavior
<What happens, with literal error output if any.>

## Proof of Concept
<Minimal runnable PoC, inlined — never a local path.>

## Environment
- <OS, runtime version, dependency versions.>

## Affected files
- `<path>:<line>` — <one-line note>
```

## 4. Post-Open Verification

MUST NEVER assume a tracker action succeeded. MUST fetch the artifact back and verify.

After opening an issue, MUST confirm: title matches, body is fully rendered (repro steps and PoC present, code blocks closed, no truncation), labels match the template's requirements, URL is reachable. MUST fix malformations immediately — MUST NOT move on.

After opening a PR, MUST confirm: title matches, body contains the intended `Fixes #N` lines, the Linked Issues sidebar (`closingIssuesReferences`) lists EXACTLY the intended issues, base branch is `master`, CI started. If any expected issue is missing from linkage, the keyword formatting is wrong — MUST fix the PR body and re-verify. MUST NOT merge with broken linkage.

After posting a comment, MUST fetch it back and confirm the body matches, and that any `@mentions` resolved to real users.

## 5. Issue Closure Rules

- MUST NEVER close in-scope issues manually — let the merged PR close them via `Fixes #N`. Temptation to hand-close means the linkage is broken; fix the PR instead.
- MUST close out-of-scope issues only via an individual polite scope-citing comment, reason `not planned` (NOT `completed`). MUST NEVER bulk-close.
- MUST close duplicates with reason `not planned` plus a comment linking the canonical issue.
- MUST leave cannot-reproduce issues open until reproduced or the user explicitly authorizes closure.

## 6. Branch and Commit Discipline

- MUST branch off `master`: `fix/<short-slug>`, `refactor/<short-slug>`. MUST avoid generic names like `patch-1`.
- MUST NEVER push directly to `master`.
- MUST NEVER `--force` push a branch with collaborators or once a PR is opened, unless every reviewer agreed.
- MUST NEVER `--no-verify` to skip hooks. A failing hook means fix what it caught.
- MUST write commit messages with a short imperative summary on line 1, blank line, then a paragraph explaining WHY (not what). MUST reference the cluster where applicable.
- MUST keep one coherent change per commit during behavior-preserving restructures.

## 7. Write-Access Pre-Flight

Before ANY tracker/repo mutation (open issue, open PR, comment, close, push):

1. MUST verify the tool/credentials have write permission on the target repo.
2. If permission is missing, MUST NOT retry blindly — MUST record the exact intended action and body, surface it to the user, and stop.

## 8. Local-Only Artifacts

`investigate/` is local-only — outside readers cannot see it. It is NOT tracked and MUST NEVER be staged, committed, or pushed.

- MUST NEVER `git add`, commit, or push anything under `investigate/`. MUST NOT use `git add -A` / `git add .` without first confirming `git status` is clean of it. If `git status` shows it staged, MUST unstage and surface the finding — MUST NOT silently commit it.
- MUST NEVER reference local paths in issue/PR bodies or comments — inline the content instead: failing-test code as a fenced block, literal failure output under `## Actual behavior` / `## Testing`, root-cause citations as tracked `src/...` file:line. Oversized PoCs go in a gist or a `<details>` fold — NEVER a local-path link.

## 9. PR-Done Checklist

- [ ] PR body has `Fixes #N` on its own line for every issue in the cluster.
- [ ] `closingIssuesReferences` lists exactly those issues.
- [ ] Base branch is `master`.
- [ ] Previously failing tests now pass; full suite green (or docs-only verification recorded, no invented unit tests).
- [ ] Full CI suite green, not just local.
- [ ] No `--force`, `--no-verify`, or amend-after-push.
- [ ] No `investigate/` paths in bodies, commits, or comments; nothing under `investigate/` staged or pushed.
- [ ] Out-of-scope issues (if any) closed via individual scope-citing comments, not by this PR.
