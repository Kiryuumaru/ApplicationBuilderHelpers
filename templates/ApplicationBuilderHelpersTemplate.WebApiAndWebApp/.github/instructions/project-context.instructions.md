---
applyTo: '**'
---
# Project Context

## Project Status

This project is new and unreleased. No production users or deployments exist.

---

## Breaking Changes Policy

- Backward compatibility is not a concern
- Rename, restructure, or rewrite freely
- Database schemas, API contracts, and serialization formats may change without migration
- No deprecation warnings required

---

## Design Priority

Prioritize clean, correct design over compatibility. Fix naming mistakes and structural issues immediately. Reset test data and schemas as needed.

---

## Terminology

| Term | Definition |
|------|------------|
| "rules", "the rules" | Files in `.github/instructions/` |
| "check the rules" | Read relevant `.github/instructions/*.md` files |
| "add to rules" | Create or update file in `.github/instructions/` |

---

## Documentation Index

| Need | Location |
|------|----------|
| All documentation | `docs/index.md` |
| API contracts | `docs/features/*.md` |
| Architecture patterns | `docs/architecture/*.md` |
| Build commands | `.github/instructions/workflow.instructions.md` |
| File organization | `.github/instructions/architecture.instructions.md` |
| Architecture rules | `.github/instructions/architecture.instructions.md` |
