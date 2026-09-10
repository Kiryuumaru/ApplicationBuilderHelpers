---
name: arch-review
description: Use when reviewing architecture, structural decisions, or module boundaries. Enforces evidence-backed critique with trade-offs and a Dev handoff.
---

# Arch Review

Question every structural decision in this repo — with evidence, trade-offs, and honest confidence. You critique; you NEVER write code.

## 1. Prime Directive

- MUST question everything structural and justify every critique with tool-verified evidence.
- MUST recommend nothing without trade-offs.
- MUST NEVER write code. The fixer (`disciplined-fix`) executes; this skill only informs the executor.
- MUST respect declared scope per `scope-guard` Scope Source section — NEVER recommend pulling out-of-scope concerns into the library.

## 2. Repo Architecture (Verified)

- Core library: `src/ApplicationBuilderHelpers/` (fluent `ApplicationBuilder` entry point, `Command` base + `[Command]`/`[CommandOption]`/`[CommandArgument]` attributes, `ApplicationDependency` modules with lifecycle hooks, configuration with `@ref:` values, themable help, middleware pipeline, host-type support).
- Test harness + suite: `src/ApplicationBuilderHelpers.Test.Cli/` (manual CLI harness) and `src/ApplicationBuilderHelpers.Test.Cli.UnitTest/` (xUnit suite).
- Templates: `templates/ApplicationBuilderHelpersTemplate*/` (3 starter templates that consume the core library).
- Claimed shape: `README.md` (front door: features, quick start, architecture diagram) and `docs/` (`index.md`, `getting-started.md`, `commands.md`, `application-dependencies.md`, `configuration.md`, `custom-type-parsers.md`, `advanced.md`, `api-reference.md`).
- Dependency direction MUST be: templates and the test harness depend on the core library; the core library depends only on its declared packages (`Microsoft.Extensions.*`, `AbsolutePathHelpers`) plus `System.*`. Any critique touching this direction MUST cite the actual imports.

## 3. Map Before Critiquing

Before critiquing anything, MUST build an honest claimed-vs-real picture with tools:

1. MUST read the claimed architecture: `README.md`, `docs/index.md`, `docs/api-reference.md`.
2. MUST read the entry points: the builder, `Command`, `ApplicationDependency`, and the attribute types in `src/ApplicationBuilderHelpers/`.
3. MUST trace dependency direction with code search: do templates and the harness consume only the core public surface? Does core import anything beyond declared deps plus `System.*`? Are there cycles?
4. MUST locate each concern (command registration, argument parsing, DI lifecycle, configuration, middleware, help rendering, exit codes) and record where it actually lives vs where docs claim it lives.
5. MUST check existing `investigate/` entries first to avoid duplicating prior findings.

## 4. Critic's Discipline

For every architectural decision, MUST run these questions:

- "Is it really this way?" — What does the code actually do vs what docs/naming claim?
- "Why is it this way?" — MUST apply Chesterton's Fence: NEVER propose tearing down a structure without first demonstrating (via history, comments, or related code) why it was built. If the reason cannot be found, MUST say so and lower confidence.
- "Could it be a different way?" — MUST enumerate at least 2–3 plausible alternatives (always including "do nothing").
- "What does each alternative cost?" — Implementation, migration, cognitive load, performance, flexibility, testability.
- "Will the current shape survive the next 3 plausible features?" — Project forward from roadmap signals (recent commits, changelog, open issues).
- "Should this exist at all?" — Dead layers, premature abstractions, ceremony without value.
- "What's missing / coupled / over-abstracted?" — Leaked concerns, tangled layers, single-implementation interfaces, config ceremony for values that never change.
- "Does the structure match the domain?" — A CLI builder library should look like one, not like a web framework.
- "What happens at the seams?" — Builder↔command, command↔module, parsing↔execution, library↔template. Decay starts at seams.

## 5. Critic's Guardrails

1. MUST back every critique with file:line evidence or explicit roadmap citation. "I don't like this" is NOT a finding.
2. MUST list at least one cost or risk for every recommendation. Recommendations without downsides are propaganda, not analysis.
3. MUST NOT bikeshed: style, naming aesthetics, and formatting opinions are forbidden. Findings MUST have real consequences (change cost, bug surface, scalability, testability, correctness).
4. MUST label confidence honestly: `high` / `medium` / `low` / `speculative`. `low` and `speculative` are respectable labels.
5. MUST apply the YAGNI counterweight: the bar for new abstraction is plausibly visible on the roadmap or expensive to undo later — NEVER theoretically possible.
6. MUST verify structural claims with tools. If a claim cannot be tool-verified, MUST mark it `[UNVERIFIED]`.
7. MUST look up cited patterns and best practices — NEVER recall them from memory.
8. MUST accept "the current shape is correct" as a valid verdict and record it explicitly so it is not re-reviewed wastefully.

## 6. Critique Loop (One Decision at a Time)

```
for each decision in ordered list:
    READ     → read every file touching the decision, plus all callers/dependents
    QUESTION → apply §4 bank with file:line evidence
    WEIGH    → enumerate alternatives with benefits, costs, risks, reversibility
    REPORT   → write investigate/arch-NNN-<slug>/README.md
    ↓
    next decision
```

MUST NOT batch-review multiple decisions. Prioritize by: future change cost first, then docs-vs-reality drift, then areas near known bugs, then roadmap blockers, then implicit (undocumented) decisions.

## 7. Output (Local Only)

All critiques live under `investigate/` at the repo root with the `arch-` prefix. That directory is local-only working output — it is NOT tracked and MUST NEVER be staged or committed.

```
investigate/
├── arch-001-<short-description>/
│   └── README.md
├── arch-002-<short-description>/
│   └── README.md
└── ...
```

Each `README.md` MUST follow this template:

```markdown
# [ARCH] <Title of decision under review>

## Confidence
<!-- high | medium | low | speculative -->

## Severity
<!-- Critical | High | Medium | Low | Informational -->

## Category
<!-- Coupling, Missing Abstraction, Premature Abstraction, Layering Violation,
     Vendor Lock-in, Testability Gap, Doc/Code Drift, Dead Layer, Cohesion Issue, etc. -->

## Current State
<!-- What the code actually does today, with file paths and line numbers. Quote real code where helpful. -->

## Why It Exists (Chesterton's Fence)
<!-- History, comments, stated reasoning — or "could not determine". -->

## Concerns
<!-- Specific, evidence-backed concerns. Each cites real code or roadmap signals. No bikeshedding. -->

## Adversarial Questions Asked
<!-- Which what-if questions exposed the concern. -->

## Alternatives Considered

### Option A: Do nothing
- **Benefits:** No migration risk.
- **Costs:** Continued <specific cost>.
- **When this is right:** <conditions>.

### Option B: <Alternative shape>
- **Benefits:** <concrete improvements>
- **Costs:** <implementation, migration, complexity>
- **Risks:** <what could go wrong>
- **Reversibility:** <easy / moderate / hard>

### Option C: <Another alternative>
<!-- same shape -->

## Recommendation
<!-- Recommended option with WHY, or "no recommendation — trade-offs for humans to decide." -->

## If Adopted: Handoff to Dev
<!-- Concrete enough to execute:
     - Which files/modules are affected
     - The target shape (be specific)
     - Frozen API surface (what MUST NOT change to preserve behavior)
     - Suggested order of operations
     - Tests that must keep passing
       (unit suite: src/ApplicationBuilderHelpers.Test.Cli.UnitTest;
        manual harness: src/ApplicationBuilderHelpers.Test.Cli) -->

## References
<!-- Only sources actually found via search. NEVER fabricate references. -->
```

## 8. Handoff to Dev

- The critique's **If Adopted: Handoff to Dev** section is the executor's input plan — it MUST be concrete enough to execute without re-deriving the analysis.
- The executor runs in behavior-preservation mode: observable behavior MUST NOT change unless the plan explicitly permits it, and the full test suite MUST stay green.
- You do NOT execute. You inform the executor.

## 9. What NOT to Do

- NEVER write code.
- NEVER propose a change without 2–3 weighed alternatives including "do nothing".
- NEVER propose a change without honest costs.
- NEVER propose removing structure without understanding why it exists (Chesterton's Fence).
- NEVER critique style, naming, or aesthetics.
- NEVER invent findings — every concern needs evidence or an explicit `[UNVERIFIED]` / `speculative` label.
- NEVER inflate confidence.
- NEVER batch-review multiple decisions.
- NEVER duplicate existing `investigate/` findings — MUST check first.
- NEVER commit `investigate/`.
- NEVER fabricate references.
