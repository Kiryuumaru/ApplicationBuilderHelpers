namespace Presentation.WebApp.Enums;

/// <summary>
/// Represents the different render states a Blazor component can be in.
/// </summary>
public enum RenderState
{
    /// <summary>
    /// Unknown or uninitialized state.
    /// </summary>
    None,

    /// <summary>
    /// Component is being pre-rendered on the server before being sent to the client.
    /// HttpContext is available but Response has not started.
    /// </summary>
    PreRender,

    /// <summary>
    /// Server-side rendering (Interactive Server mode).
    /// Component is rendered and interactive on the server via SignalR.
    /// </summary>
    SSR,

    /// <summary>
    /// Client-side rendering (WebAssembly mode).
    /// Component is rendered and interactive in the browser via WASM.
    /// </summary>
    CSR
}
