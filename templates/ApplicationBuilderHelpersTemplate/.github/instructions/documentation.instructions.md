---
applyTo: '**'
---
# Documentation Rules

## Check Documentation First

Before implementing, modifying, or asking questions, check the relevant documentation.

| Task | Check |
|------|-------|
| New feature | `.github/instructions/architecture.instructions.md` for patterns |
| Write tests | `.github/instructions/ui-test-practices.instructions.md` |
| Architecture question | `.github/instructions/architecture.instructions.md` |
| Build/workflow | `.github/instructions/workflow.instructions.md` |

---

## Documentation Locations

| Content | Location |
|---------|----------|
| Project overview | `README.md` |
| Architecture patterns | `.github/instructions/architecture.instructions.md` |
| Test conventions | `.github/instructions/ui-test-practices.instructions.md` |
| Build commands | `.github/instructions/workflow.instructions.md` |
| Agent guidance | `AGENTS.md` |

---

## Required Updates

### API Changes

| Change | Update |
|--------|--------|
| New endpoint | Update `README.md` if user-facing |
| Changed schema | Update examples |
| Changed path | Update all references |
| New parameters | Document in relevant instruction file |

### Test Changes

| Change | Update |
|--------|--------|
| New tests | Verify test conventions followed |
| Renamed tests | Update any references |
| Removed tests | Remove from any references |

### Architecture Changes

| Change | Update |
|--------|--------|
| New interfaces | Update architecture docs |
| Changed patterns | Update architecture docs |
| New infrastructure | Document behavior differences |

---

## Pre-Commit Documentation Check

- Does this change any user-facing API? -> Update `README.md`
- Does this change request/response format? -> Update examples
- Does this add/remove/rename tests? -> Verify conventions
- Does this change architecture? -> Update architecture doc
