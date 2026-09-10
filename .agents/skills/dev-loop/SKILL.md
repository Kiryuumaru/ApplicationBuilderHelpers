---
name: dev-loop
description: Use when implementing a feature or change end to end. Enforces think-first planning, build-test loop, done checklist, and clean revert discipline.
---

# Dev Loop

Ship working, tested, verified code that is built to last. You build it, test it, debug it, prove it, structure it — the user sees only finished software, never half-baked attempts.

## 1. Prime Directive

- MUST run the closed loop on every unit of work: THINK → IMPLEMENT → BUILD → TEST → PRESENT, or REVERT → RETHINK on failure.
- MUST NEVER hand over anything unproven — no untested changes, no "this should work" guesses, no shortcuts that become next quarter's tech debt.
- MUST check scope first: MUST NOT implement anything outside the repo's documented scope without explicit user authorization (see `scope-guard`).

## 2. THINK (Before Any Code)

1. MUST re-read the actual source files to touch — NEVER work from memory of a file's contents.
2. MUST search for all usages before changing any type, method, or contract — find every caller, importer, and dependent.
3. MUST read the tests covering the code being changed.
4. MUST read project config with tools (TFMs, language version, dependencies) — NEVER assume APIs or language features exist; look them up every time.
5. MUST identify all cases: happy path, edge cases, error handling, boundary conditions.
6. MUST consider side effects and fit: the change MUST follow existing layering (templates and harness depend on core; core depends only on declared packages plus `System.*`) and existing patterns — MUST NOT invent a new architectural style for one feature.
7. MUST ask the structural "what if?" questions before non-trivial work: what 3 plausible features come next, where should seams sit, what would be expensive to undo. MUST prefer clean seams over premature interfaces.
8. MUST determine task mode: new feature, bug fix (failing test first per `disciplined-fix` Failing-Test-First section), or restructure (behavior-preservation mode per `disciplined-fix` Behavior Preservation section — see `arch-review` Handoff to Dev section for the handoff shape).
9. If an `investigate/arch-NNN-*/README.md` critique applies, its Handoff to Dev section is the input plan and its frozen surface is non-negotiable. Disagreements MUST be surfaced to the user BEFORE proceeding — NEVER silently deviated from.

## 3. IMPLEMENT

- MUST write clean code following root `AGENTS.md` standards: reliability first, XML docs on public surface only, no `TODO`/`HACK`/`FIXME`/pragma/`SuppressMessage` (except AOT `UnconditionalSuppressMessage` where the build requires it), why-comments only.
- MUST handle errors properly — NEVER swallow exceptions. MUST build for resilience and the real world, not just the happy path.
- MUST make the minimum coherent change. MUST NOT expand scope into unrelated cleanup.
- MUST NOT copy-paste without understanding; MUST NOT duplicate logic across files to save a refactor — extract the shared concept when the change demands it.
- MUST isolate environment specifics behind config/adapters — NEVER hardcode provider URLs or environment values into library logic.

## 4. BUILD and TEST

- MUST run `dotnet build` — 0 warnings, 0 errors. MUST NOT suppress warnings; fix root causes. MUST NOT proceed to TEST on red (see `dotnet-workflow` BUILD and TEST Phase sections).
- MUST run `dotnet test` — 100% pass, actually green in runner output, never inferred.
- MUST cover every change: happy path, edge cases (empty, null, boundary), error cases (invalid inputs, failure scenarios), integration with neighbors, and lock-in tests for new abstractions/invariants.
- MUST apply failure scenarios CONDITIONALLY — only where relevant, with judgment, never blindly. Relevant conditions for this repo:

  | Relevant when… | Consider |
  |---|---|
  | Touching argument parsing / dispatch | Missing args, unknown command, ambiguous sub-command, duplicate flags |
  | Touching DI lifecycle / modules | Missing registration, ordering of hooks, double-run, dispose timing |
  | Touching configuration / `@ref:` | Missing keys, circular refs, wrong types, env overrides |
  | Touching middleware pipeline | Ordering, short-circuit, exception mid-pipeline |
  | Touching help rendering | Redirected console, narrow width, theme overrides |

- MUST use proper diagnostic tools (runner output, debugger, traces, logs) — MUST NOT debug with print/log statements alone, and MUST NOT ask anyone else to build, test, or verify.

## 5. Debug Ladder

When something fails, MUST climb in order:

1. **Gather** — actual output: logs, stack traces, error messages, system state, runtime behavior.
2. **Reproduce** — minimal reproduction; failing test for code bugs (fails consistently), direct docs check for prose-only issues (never invented text-assertion unit tests).
3. **Root-cause** — trace execution with debugging tools; research similar issues. MUST fix the actual cause, never symptoms.
4. **Fix and verify** — failing test now passes; full suite still green.

## 6. REVERT and RETHINK

- MUST undo the specific changes from a failed attempt and return to a known-good state. MUST NOT stack fix B on failed fix A. MUST NOT leave broken code in the tree.
- Each retry MUST be a genuinely different approach, not a minor tweak.
- After 3 failures of the same approach through clean reverts, MUST stop: analyze the failure pattern with tools, research alternatives, reconsider whether the diagnosis (not the implementation) was wrong, then explain to the user what was tried, why it failed, and what is needed.
- Valid reasons to involve the user: ambiguous requirements, needed credentials/access (ask for access, not for them to check for you), third-party defects, design trade-offs, out-of-scope requests. NEVER "can you test this", "does this look right", "can you run the build", or "I'm not sure how this works" — those mean research more first.

## 7. Done Checklist

ALL boxes MUST hold before presenting:

- [ ] `dotnet build`: 0 warnings, 0 errors. `dotnet test`: 100% pass.
- [ ] Definition-of-done spot checks per `disciplined-fix` Definition of Done section — MUST load `disciplined-fix` for the bar
- [ ] New/changed behavior has coverage, including relevant conditional scenarios from §4.
- [ ] Behavior matches the request; no placeholder code; no hardcoded values that should be configurable; error handling complete; conventions followed.
- [ ] Behavior verified personally with runner evidence — MUST present test output, never ask the user to verify.

## 8. What NOT to Do

- NEVER present unbuilt, untested code.
- NEVER stack fixes, skip the loop early because "it looks right", or guess APIs/conventions/config from memory.
- NEVER apply failure scenarios blindly — conditional per §4 only.
- NEVER leave `TODO`/`FIXME`/placeholders, ignore warnings, or copy-paste uncomprehended code.
- NEVER hardcode environment specifics into logic, duplicate logic instead of extracting, or invent new architectural styles unprompted.
- NEVER skip the structural "what if?" questions on non-trivial work.
- NEVER expand into unrelated refactoring mid-change.
- NEVER ask the user to be your eyes — use tools to see for yourself.
