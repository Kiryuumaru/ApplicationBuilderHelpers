# ApplicationBuilderHelpers — Library User Glossary

Words this library uses in help, errors, and guides. Each entry says what you see as an app builder and where to learn more.

## Settings that reuse another key (`@ref:`)

Use `@ref:OtherKey` when one setting should copy another. See [Configuration](docs/configuration.md).

- **Reference**: A setting whose value is `@ref:SomeKey`. It copies `SomeKey`.
- **Chain**: A reference that points to another reference, followed until a plain value is found.
- **Hop**: One step in that chain.
- **Final value**: The plain value at the end of the chain.
- **Circular reference**: A chain that loops back on itself. It fails to resolve.
- **Too-deep chain**: A chain that visits more than 32 keys. It fails to resolve.
- **Missing or empty key**: A chain that hits a missing or empty setting. It fails to resolve.
- **Case-insensitive**: Key matching ignores case while following a chain.

## Command-line input

See [Commands](docs/commands.md).

- **Supplied**: You typed the option, even as `""`. A non-blank env fallback also counts.
- **Missing**: You typed nothing and no usable fallback exists. Required inputs then fail with exit `2`.
- **Blank env**: An empty or whitespace-only env fallback counts as unset, never as supplied.
- **Empty string**: Typing `""` counts as supplied. It binds as `""` for text. Named `bool` flags reject it. Other types follow their own parser.
- **Omitted**: You supplied nothing, so an optional input keeps its starting value in code.
- **Trailing bare repeat**: A value-taking option typed with no usable next word (for example `--tag` after `--tag=a`). Required inputs fail. Optional inputs keep the earlier value, or fail when no earlier value exists.
- **Help**: `--help`, `-h`, `-?`, or `/?` before `--` usually shows help with exit `0`. Bad values still fail with exit `2`. On a concrete root (own run plus children) only a hit behind leading help forwards to target help: any miss reports `No command found` first. On a named grouping parent a near miss behind its help token reports `Unknown subcommand` with a pointer, while a far miss keeps the subcommand list. On Unix shells quote the `?` aliases (`'-?'`, `'/?'`) so the shell does not glob them.
- **Command-name text**: The name in `[Command("deploy prod")]`. Groups separated by spaces become subcommands. A root positional binds a bare word only when the root has no children.
- **Response file**: A text file of command-line words, referenced as `@path`. Words splice in before parsing.
- **Expansion fault**: A bad `@file` reference (missing, unreadable, over limits, cycle, lone `@`). It exits `1`.
- **Escaped `@`**: `@@x` means literal `@x`. An `@` inside a word stays literal.

## Services in commands

See [Commands](docs/commands.md).

- **Asking for a service**: Adding your `[FromServices]` shim so the library fills the property for you. Define the shims once; the gate matches by simple name in any namespace.
- **Asking for a keyed service**: Adding your property-capable `FromKeyedServicesAttribute` shim with its `object` key (`[FromKeyedServices("primary")]` or `Key = ...`). The built-in keyed marker targets parameters only and cannot sit on properties.
- **Marking for both input and service**: Putting a command-line marker (`[CommandOption]` or `[CommandArgument]`) and a service marker on the same property. This is always a build error with exit `1`.
- **Hiding with `new`**: A derived property that hides a base property. Both copies are still checked, so a clash on either one still errors.

## Reserved short flags

`h` and `V` belong to `--help` and `--version`. Do not declare your own `-h` or `-V`. See [Commands](docs/commands.md).

## Exit codes

`0` means success, help, version, or completion. `2` means bad input. `1` means failure. `130` means canceled (outer cancel, Ctrl+C, or SIGTERM). See [Commands](docs/commands.md).
