using Presentation.WebApp.Enums;

namespace Presentation.WebApp.Services;

/// <summary>
/// Service for detecting the current render state of Blazor components.
/// Provides information about whether the component is pre-rendering, SSR, or CSR.
/// </summary>
public interface IRenderStateService
{
    /// <summary>
    /// Unique identifier for this service instance.
    /// Useful for debugging to track which service instance is being used.
    /// </summary>
    Guid ServiceUid { get; }

    /// <summary>
    /// Current render state of the component.
    /// </summary>
    RenderState RenderState { get; }

    /// <summary>
    /// Returns true if the component is currently being pre-rendered on the server.
    /// Use this to skip async operations during pre-render to improve performance.
    /// </summary>
    bool IsPreRender => RenderState == RenderState.PreRender;
}
