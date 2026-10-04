# ApplicationBuilderHelpers

Build command-line apps in .NET with plain classes. Mark a class as a command, add options and arguments as properties, then run it.

- **Targets**: `net6.0` through `net10.0` · **AOT-ready on `net8.0`+** · **Trimmable**
- **Packages**: `Microsoft.Extensions.Hosting`, `Microsoft.Extensions.DependencyInjection.Abstractions`, `AbsolutePathHelpers`

## Features

- Commands with automatic parsing. You write a class. The library reads the arguments.
- Services in your command. Ask for a service. The library provides it.
- Shared setup modules. Group common services once. Reuse them in every app.
- App settings that reuse another key with `@ref:` — see [Configuration](docs/configuration.md).
- Command markers: `[Command]`, `[CommandOption]`, `[CommandArgument]`.
- Subcommands like `deploy prod`. Nest as deep as you need.
- Styled `--help` with 6 color themes and adjustable width.
- Console and web hosts. Use the default for CLIs. Plug in your own for servers.

## Installation

```bash
dotnet add package ApplicationBuilderHelpers
```

## Quick Start

New here? Follow [Getting Started](docs/getting-started.md) for a full Hello sample. It prints `Hello, Alice!` and exits `0`. Prefer a full starter app? See [Starter Templates](templates/README.md).

`RunAsync` returns an exit code: `0` success, `2` bad input, `1` failure, `130` canceled. See [Commands](docs/commands.md).

## Core Concepts

Extend `Command` and override `Run`. Add options with `[CommandOption]` and positionals with `[CommandArgument]`. Put shared setup in [Application Dependencies](docs/application-dependencies.md).

Exit codes: `0` means success, `2` means bad input, `1` means failure, `130` means canceled. See [API Reference](docs/api-reference.md).

## How a Run Works

```text
Commands → Shared setup → Host build → Services → Your Run method
```

1. You register commands and shared setup modules.
2. The library builds the host once per run.
3. The library fills your options, arguments, and services.
4. Your `Run` method executes.

Full order lives in [Commands](docs/commands.md).

## Documentation

| Guide | Covers |
|---|---|
| [Getting Started](docs/getting-started.md) | Install, minimal app, first command, run it |
| [Starter Templates](templates/README.md) | Copy-only Plain CLI, WebApi, or WebApiAndWebApp starter |
| [Commands](docs/commands.md) | Commands, options, arguments, services, completion |
| [Application Dependencies](docs/application-dependencies.md) | Shared setup modules |
| [Configuration & Themes](docs/configuration.md) | App name, help width, themes, `@ref:` settings |
| [Custom Type Parsers](docs/custom-type-parsers.md) | Support your own option types |
| [Advanced Topics](docs/advanced.md) | Subcommands, web hosts, errors, help |
| [API Reference](docs/api-reference.md) | Every public class and method |

Secret values stay hidden. Help, errors, and completions never print them.

## Contributing

Contributions are welcome! Please submit a Pull Request.

## License

MIT — see the [LICENSE](LICENSE) file.
