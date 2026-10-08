# ADR 0020: Flags Never Consult Env Fallback

**Status**: Accepted.

Supplements ADR 0015 (`0015-bare-only-boolean-flags.md`) without editing it in place. Frozen ADRs 0008a (`0008a-abstract-root-invalid-flag-literal.md`), 0014 (`0014-concrete-root-miss-gate.md`), and 0018 (`0018-reserved-help-version-bare-only.md`) stay frozen; their flag/env rows read through this record.

## Context

`EnvVarFallback.Apply` exits early for flags (`src/ApplicationBuilderHelpers/CommandLineParser/EnvVarFallback.cs:15-16`, `if (option.IsFlag) return false`). The required path also skips flags (`src/ApplicationBuilderHelpers/CommandLineParser/ParameterValidator.cs:47,72` filter on `IsRequired` / `!IsFlag`). The docs still said "an explicit flag beats env" (`docs/advanced.md:218`), which implied flags consult env at all.

## Decision

- **Flags never consult env**: `bool`/`bool?` options (`SubCommandOptionInfo.IsFlag`, `SubCommandOptionInfo.cs:41`) bind from bare tokens only. `EnvironmentVariable` on a flag is inert.
- **Env covers omitted valued options only**: typed valued options beat env; bare valued occurrences fail even with env set; required flags omitted always fail.

## Consequences

- An omitted required flag fails `MissingRequired` (exit 2) even when its `EnvironmentVariable` is set.
- Docs same pass: `docs/advanced.md` (global-options paragraph, tokenizer bullet), `docs/commands.md` (env-var fallback section, typing and precedence bullets), `docs/api-reference.md` (env/flag sentence), `CONTEXT.md` (supplied/missing entries).
- Pinned by `EnvVarFallback.cs:15-16`, `ParameterValidator.cs:47,72`, `ParseResult.cs:26` (env values never count as explicit occurrences).
