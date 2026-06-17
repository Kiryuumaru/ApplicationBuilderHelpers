using Presentation.WebApp.Enums;
using Presentation.WebApp.Services;

namespace Presentation.WebApp.Client.Services;

internal class ClientRenderStateService : IRenderStateService
{
    public Guid ServiceUid { get; } = Guid.NewGuid();

    public RenderState RenderState => RenderState.CSR;
}
