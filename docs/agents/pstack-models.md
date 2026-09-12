# Pstack Models (opencode)

Pstack skills speak in Cursor model slugs. Under opencode there are no model slugs to resolve — variations are agents, not models. This file maps those roles to the opencode agent to use.

| Role in pstack skills | Agent in opencode | Meaning |
| --------------------- | ----------------- | ------- |
| `feature, refactoring` | `inherit-parent` | Run on the parent chat model |
| `bug-fix` | `inherit-parent` | Run on the parent chat model |
| `perf-issue` | `inherit-parent` | Run on the parent chat model |
| `hillclimb` | `inherit-parent` | Run on the parent chat model |
| `judgment and prose` | `inherit-parent` | Run on the parent chat model |
| `hardest tasks` | `inherit-parent` | Run on the parent chat model |
| `how explorer` | `inherit-parent` | Run on the parent chat model |
| `how explainer` | `inherit-parent` | Run on the parent chat model |
| `why investigators` | `inherit-parent` | Run on the parent chat model |
| `why synthesizer` | `inherit-parent` | Run on the parent chat model |
| `reflect tooling` | `inherit-parent` | Run on the parent chat model |
| `reflect judgment, divergent, synthesizer` | `inherit-parent` | Run on the parent chat model |
| `arena runners` | `inherit-parent` | Run on the parent chat model; list length sets fan-out |
| `arena cross-judge pool` | `inherit-parent` | Run on the parent chat model; arena selects one entry |
| `swarm workers` | `inherit-parent` | Run on the parent chat model; default for every worker |
| `architect runners` | `inherit-parent` | Run on the parent chat model; list length sets fan-out |
| `interrogate reviewers` | `inherit-parent` | Run on the parent chat model; list length sets reviewer count |

When a skill mentions a role (e.g. "your configured how-explorer model"), use the corresponding agent string from this table. `inherit-parent` means the role runs on the parent chat model (omit Task `model`).

Edit the middle column to name a repo-verified opencode agent when one exists. Never write a model slug you have not confirmed is resolvable in this environment.

## Cursor file superseded under opencode

`~/.cursor/rules/pstack-models.mdc` is the Cursor-only mapping written by `/setup-pstack`. Under opencode it is superseded by this file. Do not delete it — Cursor sessions still read it — but opencode sessions read here.
