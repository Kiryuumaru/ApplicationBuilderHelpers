---
applyTo: '**'
---
# Rule Style Guide

This document defines the style and accent for writing rules in `.github/instructions/` files.

---

## File Structure

Every instruction file MUST have:
- YAML frontmatter with `applyTo` pattern
- H1 title matching file purpose
- H2 sections separated by horizontal rules (`---`)
- H3 subsections when needed

---

## Writing Voice

Rules use imperative, declarative voice:
- Short sentences
- No filler words
- No explanations unless required for understanding
- State the rule, not the reasoning

| Avoid | Prefer |
|-------|--------|
| "You should always use..." | "MUST use..." |
| "It's recommended to..." | "MUST..." |
| "Try to avoid..." | "NEVER..." |
| "In most cases..." | State the rule directly |

---

## Keywords

Use uppercase keywords for requirements:

| Keyword | Meaning |
|---------|---------|
| `MUST` | Mandatory requirement |
| `MUST NOT` | Absolute prohibition |
| `NEVER` | Absolute prohibition (stronger emphasis) |
| `MAY` | Optional, permitted |
| `USE` | Recommended approach |
| `PREFER` | Preferred but not mandatory |

---

## Bullet Points

Rules as bullet points:
- Start with keyword (`MUST`, `NEVER`, `MAY`)
- No period at end
- One rule per bullet
- Parallel structure within lists

Example:

    Domain services:
    - MUST be stateless
    - MUST NOT perform I/O
    - MAY depend on other domain services

---

## Tables

Tables are for **supporting information only**, not for rules.

Use tables for:
- Mappings (type -> location)
- Reference data (commands, properties, examples)
- Comparisons (before/after)

Rules MUST be written as bullet points with keywords, not embedded in tables.

| Use Tables For | Do NOT Use Tables For |
|----------------|-----------------------|
| File locations | Requirements |
| Command reference | Prohibitions |
| Property lists | Mandatory behaviors |
| Examples | MUST/NEVER rules |

Tables MUST have:
- Header row
- Alignment (default left)
- Concise cell content

---

## Code Blocks

Code examples:
- Use fenced code blocks with language identifier
- Show minimal, focused examples
- No comments unless explaining non-obvious behavior
- No ellipsis (`...`) or placeholder code

---

## Diagrams

ASCII diagrams for:
- Layer dependencies
- Inheritance hierarchies
- Flow sequences

Use box-drawing characters:
- `-`, `|`, `+`, `+`, `+`, `+` for boxes
- `^`, `v`, `<-`, `->` for arrows
- `+`, `+`, `+`, `+`, `+` for tree structures

---

## Section Patterns

### Prohibition Section

    ## Prohibited Patterns

    - NEVER use `HttpClient` directly in Application layer
    - NEVER reference Infrastructure from Domain
    - NEVER use `Task.Delay()` in tests

### Required Approach Section

    ## Required Approach

    - MUST use repository interfaces for data access
    - MUST use `[data-testid]` for test selectors
    - MUST call `base.Method()` in overrides

### Placement/Mapping Section

    ## File Placement

    | Type | Location |
    |------|----------|
    | Entity | `Domain/{Feature}/Entities/{Name}.cs` |
    | Port | `Application/{Feature}/Ports/In/{Name}.cs` |

---

## Naming Conventions

Instruction file names:
- Use kebab-case
- End with `.instructions.md`
- Describe the topic: `{topic}.instructions.md`

| Topic | File Name |
|-------|-----------|
| Architecture rules | `architecture.instructions.md` |
| Code quality | `code-quality.instructions.md` |
| Workflow | `workflow.instructions.md` |

---

## Content Principles

1. **Atomic rules** - One concept per bullet/section
2. **No redundancy** - State each rule once, in one place
3. **Concrete examples** - Show, don't just tell
4. **Consistent terminology** - Same term for same concept
5. **Scannable** - Tables and bullets over paragraphs

---

## Anti-Patterns

- NEVER use passive voice ("should be used")
- NEVER use hedging ("usually", "generally", "often")
- NEVER explain why unless critical to understanding
- NEVER use numbered lists for unordered items
- NEVER nest bullets more than 2 levels
- NEVER write paragraphs when a table works
- NEVER repeat rules across multiple files
