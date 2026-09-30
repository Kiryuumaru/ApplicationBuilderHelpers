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
- **Empty string**: Typing `""` counts as supplied. It binds as `""` for optional text and for arguments; a required text option rejects exactly `""` with exit `2` (`InvalidValue`). Named `bool` flags reject it. Other types follow their own parser.
- **Omitted**: You supplied nothing, so an optional input keeps its starting value in code.
- **Trailing bare repeat**: A value-taking option typed with no usable next word (for example `--tag` after `--tag=a`). Required inputs fail. Optional inputs keep the earlier value, or fail when no earlier value exists.
- **Help**: `--help` or `-h` before `--` usually shows help with exit `0`. Bad values still fail with exit `2`.
- **Command-name text**: The name in `[Command("deploy prod")]`. Groups separated by spaces become subcommands.

## Services in commands

See [Commands](docs/commands.md).

- **Asking for a service**: Adding `[FromServices]` so the library fills the property for you.
- **Asking for a keyed service**: Adding a property-capable attribute named `FromKeyedServicesAttribute` with its key.
- **Marking for both input and service**: Putting a command-line marker (`[CommandOption]` or `[CommandArgument]`) and a service marker on the same property. This is always a build error with exit `1`.
- **Hiding with `new`**: A derived property that hides a base property. Both copies are still checked, so a clash on either one still errors.

## Reserved short flags

`h` and `V` belong to `--help` and `--version`. Do not declare your own `-h` or `-V`. See [Commands](docs/commands.md).

## Exit codes

`0` means success, help, version, or completion. `2` means bad input. `1` means failure. `130` means canceled. See [Commands](docs/commands.md).
