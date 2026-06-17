---
applyTo: '**'
---
# Documentation Rules

## Check Documentation First

Before implementing, modifying, or asking questions, check the relevant documentation.

| Task | Check |
|------|-------|
| New feature | `docs/features/*.md` for similar patterns |
| Modify endpoint | `docs/features/*.md` for current contract |
| Write tests | `.github/instructions/ui-test-practices.instructions.md` |
| Architecture question | `.github/instructions/architecture.instructions.md` |

Documentation index: `docs/index.md`

---

## Documentation Locations

| Content | Location |
|---------|----------|
| API contracts | `docs/features/*.md` |
| Architecture patterns | `.github/instructions/architecture.instructions.md` |
| Test conventions | `.github/instructions/ui-test-practices.instructions.md` |
| Implementation status | `TODO.md` |

---

## Required Updates

### API Changes

| Change | Update |
|--------|--------|
| New endpoint | Add to feature doc |
| Changed schema | Update examples |
| Changed path | Update all references |
| New parameters | Document in endpoint table |

### Test Changes

| Change | Update |
|--------|--------|
| New tests | Update test count in feature doc |
| Renamed tests | Update test list |
| Removed tests | Remove from test list |

### Architecture Changes

| Change | Update |
|--------|--------|
| New interfaces | Update architecture docs |
| Changed patterns | Update architecture docs |
| New infrastructure | Document behavior differences |

---

## Pre-Commit Documentation Check

- Does this change any API endpoint? -> Update feature doc
- Does this change request/response format? -> Update examples
- Does this add/remove/rename tests? -> Update test list
- Does this change architecture? -> Update architecture doc
- Does this complete a TODO? -> Update TODO.md
