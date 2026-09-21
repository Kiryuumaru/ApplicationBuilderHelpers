using ApplicationBuilderHelpers.Exceptions;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// Direct truth-table tests for the shared CLI error footer.
/// Pins the exact <see cref="CommandErrorKind"/>-to-footer mapping owned by
/// <c>CommandErrorFooter.Resolve</c>: every kind with and without a command name,
/// plus the empty-name boundary on the subcommand arm. Pure string assertions,
/// so this class stays outside the <c>ConsoleDecoupling</c> collection.
/// </summary>
public sealed class CommandErrorFooterTests
{
    [Theory]
    [InlineData(CommandErrorKind.RequiresSubcommand, "config", "Run 'test config --help' to see available subcommands and options.")]
    [InlineData(CommandErrorKind.RequiresSubcommand, null, "Run 'test --help' to see available commands and options.")]
    [InlineData(CommandErrorKind.RequiresSubcommand, "", "Run 'test --help' to see available commands and options.")]
    [InlineData(CommandErrorKind.UnknownOption, null, "Run 'test --help' for more information on available commands and options.")]
    [InlineData(CommandErrorKind.UnknownOption, "", "Run 'test --help' for more information on available commands and options.")]
    [InlineData(CommandErrorKind.UnknownOption, "config", "Run 'test config --help' for more information on specific command options.")]
    [InlineData(CommandErrorKind.MissingRequired, null, "Run 'test --help' for more information on available commands and options.")]
    [InlineData(CommandErrorKind.MissingRequired, "", "Run 'test --help' for more information on available commands and options.")]
    [InlineData(CommandErrorKind.MissingRequired, "config", "Run 'test config --help' for more information on specific command options.")]
    [InlineData(CommandErrorKind.UnknownCommand, null, "Run 'test --help' for more information on available commands and options.")]
    [InlineData(CommandErrorKind.UnknownCommand, "", "Run 'test --help' for more information on available commands and options.")]
    [InlineData(CommandErrorKind.UnknownCommand, "config", "Run 'test config --help' for more information on specific command options.")]
    [InlineData(CommandErrorKind.Fault, null, "Run 'test --help' for more information on available commands and options.")]
    [InlineData(CommandErrorKind.Fault, "config", "Run 'test --help' for more information on available commands and options.")]
    [InlineData(CommandErrorKind.InvalidValue, null, "Run 'test --help' for more information on available commands and options.")]
    [InlineData(CommandErrorKind.InvalidValue, "", "Run 'test --help' for more information on available commands and options.")]
    [InlineData(CommandErrorKind.InvalidValue, "config", "Run 'test config --help' for more information on specific command options.")]
    [InlineData(CommandErrorKind.DuplicateOption, null, "Run 'test --help' for more information on available commands and options.")]
    [InlineData(CommandErrorKind.DuplicateOption, "", "Run 'test --help' for more information on available commands and options.")]
    [InlineData(CommandErrorKind.DuplicateOption, "config", "Run 'test config --help' for more information on specific command options.")]
    [InlineData(CommandErrorKind.NoImplementation, null, "Run 'test --help' for more information on available commands and options.")]
    [InlineData(CommandErrorKind.NoImplementation, "config", "Run 'test --help' for more information on available commands and options.")]
    public void Resolve_ReturnsExactFooter(CommandErrorKind kind, string? commandName, string expected)
    {
        Assert.Equal(expected, CommandErrorFooter.Resolve(kind, "test", commandName));
    }
}
