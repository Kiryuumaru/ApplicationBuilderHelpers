namespace ApplicationBuilderHelpers.Exceptions;

/// <summary>
/// Structured classification for <see cref="CommandException"/> errors.
/// Lets hosts and help rendering branch programmatically instead of sniffing message text.
/// </summary>
public enum CommandErrorKind
{
    /// <summary>
    /// Unexpected fault (developer error, host failure, or unclassified error). Maps to exit 1 unless a custom code is supplied.
    /// </summary>
    Fault = 0,

    /// <summary>
    /// An unrecognized option token (e.g. <c>--unknown-flag</c>). Usage error, exit 2.
    /// </summary>
    UnknownOption = 1,

    /// <summary>
    /// A required option or argument was not supplied. Usage error, exit 2.
    /// </summary>
    MissingRequired = 2,

    /// <summary>
    /// An abstract command was invoked without its required subcommand. Usage error, exit 2.
    /// </summary>
    RequiresSubcommand = 3,

    /// <summary>
    /// An option or argument value failed validation or conversion (including custom <c>ICommandTypeParser</c> errors). Usage error, exit 2.
    /// </summary>
    InvalidValue = 4,

    /// <summary>
    /// No command matched the supplied tokens. Usage error, exit 2.
    /// </summary>
    UnknownCommand = 5,

    /// <summary>
    /// The same non-flag option was supplied multiple times. Usage error, exit 2.
    /// </summary>
    DuplicateOption = 6,

    /// <summary>
    /// The matched command has no implementation (developer misconfiguration). Fault, exit 1.
    /// </summary>
    NoImplementation = 7,
}
