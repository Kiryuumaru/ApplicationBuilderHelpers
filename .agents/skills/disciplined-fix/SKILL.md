---
name: disciplined-fix
description: Use when diagnosing or fixing a bug, test failure, or build break in C#/.NET. Enforces evidence-first root cause, one change per attempt, verify, and clean undo.
---

# Disciplined Fix

Evidence-first bug fixing for this repo. Follow this skill end-to-end before writing any fix.

## 1. Fix Loop

Run every fix through this closed loop. NEVER skip BUILD or TEST.

1. **THINK** — Understand what is actually broken before touching code. Read the failure output, the issue body, and the surrounding source.
2. **DIAGNOSE** — MUST use diagnostic tools (build output, test runner, debugger, traces, structured logs) to produce evidence of the root cause. MUST NOT guess from reading code alone.
3. **IMPLEMENT** — MUST make ONE focused change per attempt. NEVER make multiple unrelated changes in one attempt.
4. **BUILD** — `dotnet build` MUST pass with 0 warnings, 0 errors.
5. **TEST** — Run the failing test, then the full suite (`dotnet test`). PASSED means actually green in the runner output — NEVER infer from absence of error.
6. **REVERT or PRESENT** — If the attempt failed, undo that attempt's changes and rethink. If green, present the fix.

### Undo Rules

- MUST undo the specific code changes from a failed attempt and restore files to their pre-attempt state before trying a different approach.
- MUST NOT use blanket `git revert` or `git checkout` that could destroy unrelated uncommitted work.
- NEVER leave non-working code from failed attempts in the tree.
- NEVER apply fix B on top of failed fix A.
- NEVER comment out failed code instead of removing it.

## 2. Failing-Test-First

For every executable-code bug fix:

1. MUST write the failing test first, expressing the desired behavior as an assertion.
2. MUST run it and confirm it fails for the right reason (not an import error or syntax bug).
3. MUST apply the smallest structural change that resolves the root cause.
4. MUST re-run the failing test — it MUST now pass.
5. MUST re-run the full suite — nothing else may break.
6. MUST ship the regression test together with its fix.

A bug without a failing test demonstrating it is a suspicion, not a reproduced bug. Do NOT claim it fixed.

Test locations in this repo:

- Unit suite: `src/ApplicationBuilderHelpers.Test.Cli.UnitTest/`
- Manual harness (not the regression gate): `src/ApplicationBuilderHelpers.Test.Cli/`

Docs-only and prose-only changes MUST NOT invent unit tests asserting documentation content. Verify those with docs build, lint, or link check instead.

## 3. Definition of Done

Build clean plus tests green is necessary but NOT sufficient. Before declaring done, MUST verify and record all four:

1. **Reachability** — The changed code is on the actual production call path of the reported problem. Trace the call chain from the entry point (public API call, CLI invocation, command dispatch) to the change. Cite file:line for each hop.
2. **Replacement** — The old broken path no longer executes under the problem's conditions. Dead code from the old path is removed.
3. **Scenario fidelity** — The test reproduces the literal scenario from the issue body, not a similar-shaped scenario invented for convenience.
4. **Observability of side effects** — Every new field is read somewhere. Every new event fires. Every new flag is checked. Every new public/internal method has at least one non-test caller, or the exception is explicitly justified.

If any check cannot be evidenced, the work is NOT done — return to THINK.

## 4. Anti-Duct-Tape

A duct-tape fix hides the symptom while the defect lives on. MUST reject duct tape categorically. The correct fix addresses the shared invariant, contract, or abstraction; resolves every related case as a natural consequence; survives the next plausible variation; and ships with regression tests.

### Forbidden Fixes

| # | Forbidden pattern | Why it is duct tape |
|---|---|---|
| 1 | `if (specificInputThatBroke) { handle differently }` | Patches one report; leaves the broken contract intact. |
| 2 | `try { ... } catch { /* ignore */ }` to silence a stack trace | Hides the defect; the system is still wrong, just quieter. |
| 3 | `try { ... } catch { return defaultValue }` without understanding the throw | Silences without diagnosing. |
| 4 | Adding `// TODO: fix properly later` next to a workaround | The workaround becomes permanent debt. NEVER use `TODO` (see root `AGENTS.md`). |
| 5 | Sprinkling null-checks at every call site instead of fixing the producer | Shotgun symptom-patching; new call sites repeat the bug. |
| 6 | Wrapping a deterministically-failing call in a retry loop | Makes the failure intermittent instead of fixing it. |
| 7 | Bumping a timeout to make a race "go away" | The race remains; it just loses less often. |
| 8 | Disabling, skipping, or deleting a test that catches the bug | The test was right; the code is wrong. |
| 9 | Pinning to an old dependency to dodge a breaking change | Defers the problem and accumulates security debt. |
| 10 | Adding configuration flags to opt out of broken behavior | Pushes the bug onto users. |
| 11 | Hardcoding a value because the dynamic lookup is broken | Loses the abstraction that made the code correct generally. |
| 12 | Fixing each issue in a cluster with its own special-case branch | Proves the shared root cause was never found. |

### When a Workaround Is Legitimate (Rare)

A workaround is acceptable ONLY when ALL of these hold:

1. The true root cause lives in code outside this repo (upstream library, vendor SDK, OS).
2. An existing upstream issue is linked, or one has been filed.
3. The workaround is isolated to a single adapter layer — NEVER scattered through library logic.
4. The workaround is labeled in code with the upstream link and removal conditions.
5. A regression test asserts the upstream bug exists, so the workaround can be removed once upstream is fixed.

Anything else is duct tape. MUST re-diagnose instead.

## 5. Behavior Preservation

When the task is a restructure (refactor, rename, move, extract, split, merge, deduplicate) with no intended behavior change, the bar is: the system does exactly what it did before, with a better internal shape.

1. MUST freeze the public surface: public APIs, exported types, CLI flags, config keys, error codes, log formats consumers depend on. NOTHING on that surface changes unless the plan explicitly permits it.
2. MUST preserve observable behavior (except the specific bug being fixed, if any): same inputs to same outputs, same side-effect order, same errors at the same boundaries.
3. MUST keep every pre-existing test passing. NEVER modify a pre-existing test to make it pass after a restructure — a broken test means broken behavior. Revert.
4. MUST NOT make drive-by behavior changes: no extra error handling, logging, or validation "while in there." Record other bugs found for a separate session.
5. MUST NOT add new dependencies unless explicitly required.
6. MUST preserve license headers, attribution, and intent-conveying comments — move them with the code they belong to.
7. MUST search-before-move: find EVERY reference (imports, string references, dynamic lookups, test and doc references) before moving or renaming anything, and update them atomically.
8. MUST prove dead code dead with exhaustive search (including reflection and string-based access) before deleting. If the search cannot be exhaustive, flag as blocked rather than guessing.
9. MUST commit incrementally: one coherent move/rename/extraction per unit with a full test pass between units. NEVER mix several refactors into one mega-change.
10. MUST write characterization tests FIRST when no tests cover the affected behavior, capturing current behavior before restructuring.

## 6. Pragmatism Guardrail (YAGNI)

- MUST add the abstraction when the second use case is plausibly visible, the dependency is external and likely to change, or deferring would force rewriting core logic later.
- MUST NOT add the abstraction when only one implementation is imaginable, refactoring later is cheap, and the abstraction would obscure rather than clarify.
- When in doubt, MUST prefer a clean seam (well-named function with a clear contract) over a premature interface.
- MUST record any structural trade-off that defers flexibility so it is not forgotten.

## 7. Done Checklist

- [ ] Failing test written first, failed for the right reason, now passes.
- [ ] Full suite (`dotnet test`) green; `dotnet build` 0 warnings, 0 errors.
- [ ] All four Definition of Done checks evidenced (§3).
- [ ] No forbidden pattern from §4 in the diff.
- [ ] No pre-existing test modified to pass (restructures).
- [ ] No unrelated changes in the same attempt.
