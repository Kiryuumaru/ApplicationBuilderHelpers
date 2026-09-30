# Domain Docs

How the engineering skills consume this repo's domain documentation when exploring the codebase.

## Before exploring, read these

- **`CONTEXT.md`** at the repo root. It defines the CLI vocabulary and points at the canonical doc for each term.
- **`docs/adr/`**: read ADRs that touch the area you work in. Start with the index at `docs/adr/README.md`. ADRs explain why. Current behavior lives in the guides under `docs/`.

This repo has one glossary. There is no `CONTEXT-MAP.md` and no second glossary under `src/`.

## File structure

```text
/
├── CONTEXT.md
├── docs/adr/ (one file per decision; see its README.md for the index)
└── src/
    └── ApplicationBuilderHelpers/
```

## Use the glossary's vocabulary

When your output names a domain concept, use the `CONTEXT.md` term. This covers issue titles, proposals, hypotheses, and test names. Avoid synonyms the glossary rejects.

If the concept is missing from the glossary, pause. You may invent unused language (reconsider). Or you found a real gap (note it for `/domain-modeling`).

## Flag ADR conflicts

If your output contradicts an existing ADR, surface it explicitly rather than silently overriding:

> _Contradicts ADR-0007a (abstract-root requires-subcommand), but worth reopening because…_
