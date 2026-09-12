# ApplicationBuilderHelpers

A .NET library for building command-line applications with a fluent API, dependency injection, and modular architecture.

## Language

**Reference**:
A single configuration value of the form `@ref:key` that points at another key.

**Chain**:
A sequence of hops followed from one reference to the next.

**Hop**:
One `@ref:` resolution from a reference to the key it points at.

**Terminal**:
The final non-`@ref:` value a chain resolves to.

**Cycle**:
Revisiting a key already seen in the same chain.

**Overflow**:
Exceeding the maximum depth of a chain.
