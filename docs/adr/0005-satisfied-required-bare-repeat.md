# ADR-0005: Satisfied-Required Bare Repeat Fails MissingRequired (#470)

**Status**: Accepted.

## Context

Valued repeats resolve last-wins (`DuplicateOptionTests.cs:71-119`; ADR-0004). The #470 gap: a satisfied required valued scalar repeated bare at end-of-line (`--name John ... --name`) previously slipped through, while single-trailing-bare, collection-between-flags, and optional-repeat behavior was already pinned (`ValuedOptionNeighborTests.cs:118-145,158-166`); bare-then-valued healing follows from the `AddOptionValue` eviction (`ParseResult.cs:33-43`) with no dedicated test.

## Decision

Required-only scope, enforced in `ParameterValidator.cs:31-36` on the `ParseResult.cs:18-24` bare ledger (`AddOptionValue` at `:31-52`, canonical key at `:59-60`):

- A satisfied required valued scalar repeated bare fails `MissingRequired` (exit 2) regardless of env fallback (`ParameterValidator.cs:31-36`; `ValuedOptionNeighborTests.cs:147-156`).
- A single trailing-bare stays env-rescuable (reject-by-default sentinel, `ArgumentParser.cs:135-140`).
- A satisfied optional repeated bare is ignored and the prior value stands (`ValuedOptionNeighborTests.cs:158-166`).
- Bare-then-valued heals via the same eviction (`ParseResult.cs:33-43`); collections accumulate; bare boolean flags stay idempotent.
- Error kind is `MissingRequired`, never `DuplicateOption` (defined at `Exceptions/CommandErrorKind.cs:42` with no throw sites).

## Consequences

- Last-wins stands for valued repeats with real values; the only exception is a trailing bare repeat in required scope.
- Optional/collection/flag behavior is unchanged.
- Docs: `docs/commands.md` tokenizer bullets, `docs/advanced.md` mirrors, `CONTEXT.md` CLI Presence Glossary (`Bare repeat`).
