# Triage Labels

The skills speak in five canonical triage roles. This file maps each role to its label string in this repo's tracker.

| Label in mattpocock/skills | Label in our tracker | Meaning                                  |
| -------------------------- | -------------------- | ---------------------------------------- |
| `needs-triage`             | `needs-triage`       | Maintainer needs to evaluate this issue  |
| `needs-info`               | `needs-info`         | Waiting on reporter for more information |
| `ready-for-agent`          | `ready-for-agent`    | Fully specified, ready for an agent      |
| `ready-for-human`          | `ready-for-human`    | Requires human implementation            |
| `wontfix`                  | `wontfix`            | Will not be actioned                     |

When a skill mentions a role (e.g. "apply the ready triage label"), use the corresponding label string from this table.

Scope: what library users do with the library, plus its guides. Docs-only fixes keep the `ready-for-agent` path; behavior changes need a maintainer decision first.
