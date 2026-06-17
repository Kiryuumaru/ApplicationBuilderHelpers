namespace Application.Shared.Interfaces.Inbound;

/// <summary>
/// Defines constants for application metadata and configuration.
/// </summary>
public interface IApplicationConstants
{
    /// <summary>
    /// Gets the application name.
    /// </summary>
    string AppName { get; }

    /// <summary>
    /// Gets the application title.
    /// </summary>
    string AppTitle { get; }

    /// <summary>
    /// Gets the application description.
    /// </summary>
    string AppDescription { get; }

    /// <summary>
    /// Gets the application version.
    /// </summary>
    string Version { get; }

    /// <summary>
    /// Gets the application tag (release channel or environment label).
    /// </summary>
    string AppTag { get; }

    /// <summary>
    /// Gets the build payload serialized as a Base64 string.
    /// </summary>
    string BuildPayload { get; }
}
