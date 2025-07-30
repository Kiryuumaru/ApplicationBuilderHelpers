this project is not finished, many things is still not working, you are freely can edit everything you see fit.
i want to create a c# command args parser from scratch
i want to create something like:
---------------------------------------------------------------------
test.exe -h
---------------------------------------------------------------------
MyApp CLI v2.4.1 - Advanced Project Management Tool
A powerful command-line interface for managing development projects with built-in CI/CD integration.

USAGE:
    myapp [OPTIONS] <COMMAND> [ARGS]
    myapp [OPTIONS] <COMMAND> --help

GLOBAL OPTIONS:
    -l  --log-level <log-level>     Log level for logging
                                    Available: trace, debug, information, warning, error,
                                        fatal, none
                                    Default: information
    -h, --help                      Show this help message
    -V, --version                   Show version information

COMMANDS:
    init        Initialize a new project
    build       Build the project with optional deployment
    test        Run test suites with filtering options
    deploy      Deploy to specified environments
    config      Manage configuration settings
    plugin      Manage plugins and extensions

Run 'myapp <command> --help' for more information on specific commands.

---------------------------------------------------------------------
test.exe build -h
---------------------------------------------------------------------
USAGE:
    myapp build [OPTIONS] [GLOBAL OPTIONS] [ARGS]

OPTIONS:
    --target <target> (required)    Build target 
    -o, --output <TEXT>             Output directory
                                    Environment variable: TEST_OUTPUT
                                    Possible values: debug, release, test
                                    Default: debug
    -r, --release                   Build in release mode

ARGUMENTS:
    <project>                       Project file to build

GLOBAL OPTIONS:
    -l  --log-level <log-level>     Log level for logging
                                    Possible values: trace, debug, information, warning, error,
                                        fatal, none
                                    Default: information
    -h, --help                      Show this help message
    -V, --version                   Show version information

---------------------------------------------------------------------

Checklist:
* with proper width. make 2 column, left and right.
* left: -c, --config <FILE>
* rigth: Configuration file path [default: ~/.myapp/config.yml]
* left fit all its width, right will fit the rest.
* array options should be: --opt val1 --opt val2
* array elements must resolve its values to existing already-defined ICommandTypeParser list
* if option or argument have default value, show it to help
* if command name has spaces on it like "remote add" or "git.exe remote add", cascade subcommand with parent "remote". attribute will be like this: [Command(name: "remote add", description: "Remote add description")]
* use ApplicationBuilderHelpers.Test.Cli to create a test.exe app
* use ApplicationBuilderHelpers.Test.Playground to test the CLI output with all test cases.
* ApplicationBuilderHelpers.Test.Playground must test directly using the test.exe file of ApplicationBuilderHelpers.Test.Cli
* if all subcommand have a same command option, make it a global option in help. just like --log-level in BaseCommand, it should be listed as global option because it is available in all subcommand including main one
* properly structure everything, separate things if they get crowded
* add colors to help, beautify it
