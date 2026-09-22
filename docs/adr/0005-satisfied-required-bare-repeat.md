# ADR-0005: Satisfied-Required Bare Repeat Fails MissingRequired (#470)

**Status**: Accepted — scope extended by #503 (see Addenda below; original decision text preserved).

**Superseded in scope by #503 with explicit-bare hard-fail**: the required-only scope below now covers unsatisfied bare optional valued options too (fail `MissingRequired` exit 2 even with env set). Satisfied-optional repeat stays ignored. Env fallback rescues only omitted (never-typed) options, never a typed bare.

## Context

Valued repeats resolve last-wins (`DuplicateOptionTests.cs:71-119`; ADR-0004). The #470 gap: a satisfied required valued scalar repeated bare at end-of-line (`--name John ... --name`) previously slipped through, while single-trailing-bare, collection-between-flags, and optional-repeat behavior was already pinned (`ValuedOptionNeighborTests.cs:118-145,158-166`); bare-then-valued healing follows from the `AddOptionValue` eviction (`ParseResult.cs:33-43`) with no dedicated test.

## Decision

Required-only scope, enforced in `ParameterValidator.cs:31-36` on the `ParseResult.cs:18-24` bare ledger (`AddOptionValue` at `:31-52`, canonical key at `:59-60`):

- A satisfied required valued scalar repeated bare fails `MissingRequired` (exit 2) regardless of env fallback (`ParameterValidator.cs:36-41`; `ValuedOptionNeighborTests.cs:147-156`).
- A single trailing-bare fails even with env set (explicit bare claims ownership; reject-by-default sentinel, `ArgumentParser.cs:135-140`).
- A satisfied optional repeated bare is ignored and the prior value stands (`ValuedOptionNeighborTests.cs:158-166`).
- Bare-then-valued heals via the same eviction (`ParseResult.cs:33-43`); collections accumulate; bare boolean flags stay idempotent.
- Error kind is `MissingRequired`, never `DuplicateOption` (defined at `Exceptions/CommandErrorKind.cs:42` with no throw sites).

## Consequences

- Last-wins stands for valued repeats with real values; the only exception is a trailing bare repeat in required scope.
- Optional/collection/flag behavior is unchanged.
- Docs: `docs/commands.md` tokenizer bullets, `docs/advanced.md` mirrors, `CONTEXT.md` CLI Presence Glossary (`Bare repeat`).

## Addendum — #503: Unsatisfied Bare Optional Fails Unless Env-Rescued (superseded — see follow-up addendum below)

Unsatisfied bare optional valued options (`--config` at end-of-line or before a flag-looking neighbor, no merged value anywhere) now fail `MissingRequired` (exit 2) as `Missing value for option: <display-name>` unless `EnvironmentVariable` fallback rescues them first (`ParameterValidator.cs:50-71`, env-first via `EnvVarFallback.Apply` with `requiredOnly: false`). An option with no declared `EnvironmentVariable` has no rescue and always fails.

Unchanged: satisfied-optional repeat stays ignored (merged-values gate at `ParameterValidator.cs:62-63`); `--help`/`--version` neighbors keep their carve-out (optional-bare pass skipped when `ShowHelp`/`ShowVersion`); unknown neighbors still error first in the parser before validation runs; error kind stays `MissingRequired`, never `DuplicateOption`.

## Addendum — #503 follow-up: Explicit Bare Always Fails Even With Env Set

Typing the option claims ownership: any explicit bare valued occurrence (end-of-line or before-flag, optional or required, single or repeat) now fails `MissingRequired` (exit 2) even with env set. Env fallback rescues only omitted (never-typed) options — via the required rescue gated on no bare mark (`ParameterValidator.cs:29-31`) and the binder omitted path (`ValueBinder.cs:27`, unreachable once validation throws on bare). The optional-bare pass (`ParameterValidator.cs:52-71`) carries no rescue call. The required single-bare rescue and the optional single-bare rescue are both removed; satisfied-optional repeat stays ignored (merged-values gate at `ParameterValidator.cs:64-65`); `--help`/`--version` carve-out, unknown-first precedence, and `MissingRequired` kind are unchanged. Migration: remove the flag to use the env value — `--config --verbose` with `TEST_CONFIG` set now fails instead of binding env.
