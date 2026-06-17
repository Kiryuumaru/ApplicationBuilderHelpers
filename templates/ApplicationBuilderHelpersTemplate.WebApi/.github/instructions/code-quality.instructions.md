---
applyTo: '**'
---
# Code Quality Rules

## Fix Hygiene

### Fix Procedure

1. **Find and analyze the problem** — MUST understand what is actually broken before touching code
2. **Use proper diagnostic tools** — MUST use available tools (tracing, structured logs, debuggers, profilers, network tools, build diagnostics) to produce evidence of the root cause. MUST NOT guess based on reading code alone
3. **Fix one thing at a time** — MUST make ONE focused change per attempt
4. **Verify** — MUST confirm the fix works (build, test, or runtime check). If confirmed working, proceed to next issue
5. **Undo on failure** — If the fix does NOT work, MUST undo the specific code changes from that attempt. MUST NOT use blanket `git revert` or `git checkout` that could destroy ongoing uncommitted work. MUST restore files to their pre-attempt state before trying a different approach

### Prohibited

- NEVER leave non-working code from failed fix attempts
- NEVER apply fix B on top of failed fix A
- NEVER comment out failed code instead of removing
- NEVER guess at root cause without diagnostic evidence
- NEVER make multiple unrelated changes in one fix attempt

---

## Zero Warnings

Build MUST complete with 0 warnings and 0 errors. Every warning is a potential bug.

---

## Nullable Handling

The null-forgiving operator (`!`) is prohibited except when ALL conditions are met:
1. Value is proven non-null at that point
2. Compiler cannot infer this due to analysis limitations
3. Comment explains why it is safe
4. No reasonable restructuring alternative exists

Nullable handling patterns:
- For possibly null value, USE `?? throw new InvalidOperationException()`
- For null check, USE `if (x is null) return;`
- For optional value, DECLARE as nullable type `T?`
- For constructor parameter, USE `?? throw new ArgumentNullException(nameof(param))`

---

## Reliability Principles

- MUST make illegal states unrepresentable using the type system
- MUST validate at boundaries by checking all inputs at system edges
- MUST fail fast by throwing early rather than propagating bad state
- MUST be explicit and never rely on implicit behavior or defaults

---

## Verify Before Claiming

- NEVER claim code is broken without running it to confirm
- NEVER assume a fix is needed without reproducing the issue
- NEVER guess at behavior without checking the actual implementation
- MUST read existing code before claiming it needs changes
- MUST run tests or build before stating something fails

---

## Code Longevity

- MUST use stable, documented APIs
- MUST avoid implementation-specific tricks
- MUST document reasoning, not mechanics

---

## Primary Constructors

When a constructor only stores parameters without additional logic, USE primary constructors (C# 12).

Traditional constructor (avoid when only storing):
```csharp
internal sealed class DomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IEnumerable<IDomainEventHandler> _handlers;

    public DomainEventDispatcher(IEnumerable<IDomainEventHandler> handlers)
    {
        _handlers = handlers;
    }

    public void DoWork() => _handlers.ToList();
}
```

Primary constructor (preferred):
```csharp
internal sealed class DomainEventDispatcher(IEnumerable<IDomainEventHandler> handlers) : IDomainEventDispatcher
{
    public void DoWork() => handlers.ToList();
}
```

Primary constructor rules:
- USE when constructor only assigns parameters to fields
- USE parameter name directly in methods (no underscore prefix)
- KEEP traditional constructor when validation or transformation logic is needed
- KEEP traditional constructor when multiple constructors are required

---

## Commenting

### Core Principle

Comments exist for future readers with no conversation context. The codebase stands alone.

### Prohibited Comments

- NEVER use `// TODO:` comments
- NEVER use `// HACK:` comments
- NEVER use `// FIXME:` comments
- NEVER use `#pragma warning disable`
- NEVER use `[SuppressMessage]` attributes (exception: `[UnconditionalSuppressMessage]` for AOT/trimming when no workaround exists)
- NEVER use `var x = value!;` without justification comment
- NEVER write comments referencing prompts: "As requested", "Per instruction", "Added because asked"
- NEVER write comments referencing conversation: "As discussed", "Per our conversation", "Following the plan"
- NEVER write meta-comments: "NEW:", "CHANGED:", "This is the fix"
- NEVER write obvious descriptions: "Loop through list", "Return result", "Check if null"
- NEVER write comments that would not make sense 2 years later without conversation context
- NEVER write XML documentation on internal implementation classes
- NEVER describe what code does when code is self-explanatory
- NEVER ignore compiler warnings
- NEVER suppress warnings instead of fixing root cause

### Required Comments

- MUST document non-obvious behavior: security implications, order dependencies
- MUST document external requirements: RFC references, spec compliance
- MUST document edge cases: intentional empty returns, fail-safe defaults
- MUST document reasoning: why this approach, not what it does

### Comment Decision

1. Is this obvious from the code? -> No comment
2. Does this reference conversation? -> No comment
3. Would future reader need this context? -> Comment
4. Is there non-obvious reasoning? -> Comment

---

## XML Documentation

XML docs (`///`) are required for public types shared across layers. Internal types do not need XML docs.

Public cross-layer types MUST have XML docs:
- Interfaces (public by design, used across layers)
- Domain entities, value objects, records, enums
- Application DTOs/models used by other layers

Internal types MUST NOT have XML docs:
- Interface implementations (internal)
- Infrastructure models/entities (internal)
- Layer-internal helper classes

Quick rule:
- `public` + cross-layer = XML docs required
- `internal` = no XML docs
