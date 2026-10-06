namespace ApplicationBuilderHelpers.Exceptions;

/// <summary>
/// Usage-vs-fault classification selecting the <see cref="CommandException"/> exit code and error footer.
/// </summary>
public enum CommandErrorKind
{
    /// <summary>
    /// Unexpected fault; exits 1 unless a custom code is supplied.
    /// </summary>
    Fault = 0,

    /// <summary>
    /// Unrecognized option token; usage error, exit 2.
    /// </summary>
    UnknownOption = 1,

    /// <summary>
    /// Required option or argument omitted; usage error, exit 2.
    /// </summary>
    MissingRequired = 2,

    /// <summary>
    /// Abstract command invoked without its required subcommand; usage error, exit 2.
    /// </summary>
    RequiresSubcommand = 3,

    /// <summary>
    /// Option or argument value failed validation or conversion; usage error, exit 2.
    /// </summary>
    InvalidValue = 4,

    /// <summary>
    /// No command matched the supplied tokens; usage error, exit 2.
    /// </summary>
    UnknownCommand = 5,

    /// <summary>
    /// A scalar valued option repeated explicitly with <c>SetRejectDuplicateOptions(true)</c>. Usage error, exit 2.
    /// </summary>
    DuplicateOption = 6,

    /// <summary>
    /// Matched command has no implementation (misconfiguration); fault, exit 1.
    /// </summary>
    NoImplementation = 7,
}
