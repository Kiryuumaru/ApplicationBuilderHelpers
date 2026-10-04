# ADR Index

Decision records are immutable. Do not edit a record after it lands.

## Records

| Number | Title | Status |
|---|---|---|
| 0001 | Preserve Empty-String CLI Values | Accepted |
| 0002 | Completion Gateway | Accepted |
| 0003 | Command Descriptor Reflection | Accepted |
| 0004a | Descriptor Walk Enum Unification | Accepted |
| 0004b | Global Option Identity | Accepted |
| 0005a | Argument Enum Auto-Population | Accepted |
| 0005b | Satisfied Required Bare Repeat | Accepted |
| 0006 | Unified CLI-Bound Identity | Accepted |
| 0007a | Abstract Root Requires Subcommand | Accepted |
| 0007b | Help Beats Missing Required | Accepted |
| 0007c | Reserved Help/Version Shorts | Accepted |
| 0008a | Abstract Root Invalid Flag Literal | Accepted |
| 0008b | Positional Arguments Do Not Inherit | Accepted |
| 0009 | Concrete Root Leading Help First | Accepted |
| 0010 | Group Unknown/Invalid Beats Version | Accepted |
| 0011 | Help-First Target Routing | Accepted |
| 0012 | Symmetric Help/Version Forgiveness | Accepted |
| 0013 | Unknown Beats Help Order-Invariance | Accepted |
| 0014 | Concrete-Root Miss Gate + Childless-Only Positional Exemption | Accepted |
| 0015 | Bare-Only Boolean Flags | Accepted |
| 0016 | Dangling-Valued Error Beats Help, Empty `=`-Form Carve-Out, Single-Dash Long Tokens | Accepted |

Suffixed letters (`0004a`/`0004b`) resolve filename collisions. Numbers never shift after landing.

Records stay frozen; suffix splits resolve collisions. Map old `0004-...` stems to `0004a-...` / `0004b-...`. Map old `0005-...` stems to `0005a-...` / `0005b-...`. Map old `0007-...` stems to `0007a-...` / `0007b-...` / `0007c-...`. Map old `0008-...` stems to `0008a-...` / `0008b-...`. ADR-0011:5 cites `0007-abstract-root-requires-subcommand.md`; read it as `0007a-...`. Never rewrite a frozen body to fix a stem.

## Contributor rule

File the next record at max+1. Never renumber a landed record.

Supplements never substitute: an ADR explains why a behavior exists. The current behavior lives in `docs/` with symbol-qualified pins. Cite the doc anchor first, then the ADR.
