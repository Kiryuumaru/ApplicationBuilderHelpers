---
name: bug-hunt
description: Use when hunting bugs or auditing behavior within declared scope. Enforces what-if analysis, runnable reproduction, and PoC-backed findings.
---

# Bug Hunt

Systematically prove that library features break — or prove they hold. You find bugs; you NEVER fix code.

## 1. Prime Directive

- MUST hunt only within declared scope (the repo's documented feature surface plus root `AGENTS.md`). Findings outside scope are NOT bugs.
- MUST prove every finding with a runnable reproduction. No repro, no bug — a suspicion is not a bug and a code-trace alone is not a bug.
- MUST NOT fix code. Document what is broken and move on.
- MUST NOT report missing features as bugs — if the code does not claim to support something, its absence is not a bug.
- MUST NOT report stylistic issues or code smells — a bug is broken behavior.

## 2. Scope and Trust Boundaries (This Repo)

Per `scope-guard` Scope Source section: this repo is a .NET CLI-builder library (command registration, argument parsing, DI modules, configuration, middleware, help rendering, exit codes)

- MUST treat command dispatch, option/argument parsing, lifecycle hooks, configuration resolution, middleware ordering, and exit-code behavior as in-scope audit surface.
- MUST NOT pursue out-of-scope concerns as defects — per `scope-guard` Scope Source section for the non-goal list
- MUST confirm reachability for every suspicion: can the problematic input actually reach this path? Search all call sites. If validation elsewhere blocks it, drop the suspicion.

## 3. Recon (One Area at a Time)

Before hunting, MUST map the area under audit with tools:

1. MUST list the relevant `src/` project (`ApplicationBuilderHelpers/` core library, `ApplicationBuilderHelpers.Test.Cli/` harness) and read its entry points.
2. MUST trace data flow from entry to output: where input enters, what transforms it, where it is validated, where it ends up.
3. MUST read the existing tests covering the area (`src/ApplicationBuilderHelpers.Test.Cli.UnitTest/`) to learn intended behavior.
4. MUST check dependency versions from manifest files with tools — NEVER from memory. MUST search vulnerability databases with tools for known CVEs — NEVER recall CVEs from memory.

Prioritize audit order by risk: external-input handlers (argv parsing) first, then command dispatch/matching, DI lifecycle, configuration resolution, middleware ordering, help rendering, exit codes, dependencies.

## 4. What-If Discipline

At every decision point in the audited code, MUST ask adversarial questions within the feature's documented scope:

1. What if the input is empty — null, `""`, `[]`, zero args?
2. What if the input is at the boundary — max args, exactly-at-limit, one-over-limit?
3. What if the input is the wrong type or shape?
4. What if a service resolution fails — missing registration, disposed provider, partial setup?
5. What if this runs concurrently — two commands at once, race between registration and execution?
6. What if the state is unexpected — already run, half-configured, partially initialized?
7. What if the order is wrong — called before registration, after dispose, twice, out of sequence?
8. What if the data is inconsistent — unknown command name, option for another command, config changed mid-run?
9. What if the happy-path assumption fails — host unavailable, console redirected, resource missing?
10. What if the error handler itself fails — exception in catch, retry loop that never terminates, swallowed exception?

MUST NOT fuzz with random garbage outside the feature's scope. Each question targets whether the feature handles its documented contract.

## 5. Audit Categories

For each area, MUST check the applicable categories:

| Category | What to look for in this repo |
|---|---|
| Input validation | Missing sanitization, unhandled empty/duplicate args, command-name confusion |
| State machine | Command lifecycle violations, registration/execution ordering, dispose-then-use |
| Concurrency | Races on shared builder state, out-of-order middleware, deadlock potential |
| Error handling | Swallowed exceptions, wrong exit codes, verbose errors leaking internals |
| Resource management | Unbounded allocation, providers or handles leaked on all paths |
| Configuration | Insecure defaults, hardcoded values that should be options, missing validation of option ranges |
| Dependencies | Known CVEs (searched, not recalled), outdated packages |
| Data exposure | Sensitive data in logs or help output, secrets mishandled |
| Business logic | Sub-command mismatch, middleware bypass, config `@ref:` mis-resolution |

MUST skip no area because it "looks safe" — audit it with tools.

## 6. Reproduction Bar

Before a finding graduates from suspicion to confirmed bug, MUST:

1. MUST write a failing test in the project's test framework when feasible (so the fixer inherits a regression gate); otherwise write a minimal standalone PoC under `investigate/bug-NNN-<slug>/poc.*`.
2. MUST actually run it — NEVER infer failure from reading.
3. MUST confirm it fails for the predicted reason (wrong value, exception, wrong exit code, missing side-effect). Failures from import errors, fixture typos, or environment mismatch do NOT count.
4. MUST capture the literal failure output (stack trace, assertion message) into the finding's `README.md` under `## Evidence`.
5. MUST confirm the failure is in the code under test, not the environment (missing deps, wrong SDK, missing secrets mean the PoC is broken, not the code).

If reproduction is impossible with available tools (needs production data, paid credentials, hardware, unforceable timing), MUST park it under `## Unreproduced Suspicions` with what was tried and why it did not trigger — and MUST NOT create an `investigate/bug-NNN-*/` entry for it.

Docs-only or prose-only findings MUST be handed off as docs issues directly — MUST NOT invent a unit test asserting text content.

## 7. Hunt Loop (One Feature at a Time)

```
for each feature in ordered list:
    READ      → read the feature's implementation completely, every line
    HUNT      → walk every code path with §4 what-if questions and §5 categories
    REPRODUCE → prove it per §6, or park as suspicion
    REPORT    → write investigate/bug-NNN-<slug>/README.md + poc.*
    ↓
    next feature
```

MUST NOT batch-analyze multiple features. One feature, start to finish, then the next.

## 8. Output (Local Only)

All findings live under `investigate/` at the repo root. That directory is local-only working output — it is NOT tracked and MUST NEVER be staged or committed.

```
investigate/
├── bug-001-<short-description>/
│   ├── README.md
│   └── poc.* (proof-of-concept, where feasible)
├── bug-002-<short-description>/
│   ├── README.md
│   └── poc.*
└── ...
```

Each `README.md` MUST follow this template:

```markdown
# [BUG] <Title>

## Severity
<!-- Critical | High | Medium | Low -->

## Category
<!-- e.g. State Machine, Concurrency, Input Validation, Resource Management, Dependency, Error Handling -->

## Affected Feature
<!-- Which feature/scope this bug lives in -->

## Summary
<!-- One paragraph: what breaks, under what conditions -->

## The "What If?" That Found It
<!-- The specific adversarial question that exposed this bug -->

## Evidence
<!-- Step-by-step trace: file paths and line numbers, code path taken,
     where the assumption fails, plus the literal failure output captured on run -->

## Proof of Concept
<!-- Failing test path + name, or poc.* file in this folder -->

## Root Cause
<!-- What assumption was wrong, what check was missing -->

## Impact
<!-- What goes wrong: crash, wrong exit code, lost config, deadlock, silent failure, etc. -->

## Suggested Fix
<!-- Brief description of what should change. Do NOT write the fix. -->
```

## 9. What NOT to Do

- NEVER hunt outside declared scope.
- NEVER promote a suspicion to a bug without a runnable reproduction.
- NEVER guess behavior from names — MUST read the actual implementation.
- NEVER verify from memory — MUST use tools for every check.
- NEVER inflate severity — MUST be honest about reachability and real impact.
- NEVER duplicate a known finding — MUST check existing `investigate/` entries first.
- NEVER assume tests cover a path — MUST read the tests.
- NEVER fix bugs in this skill — hand off proven failures to `disciplined-fix`.
- NEVER commit `investigate/`.
- NEVER fabricate references — MUST only cite sources actually found via search.
