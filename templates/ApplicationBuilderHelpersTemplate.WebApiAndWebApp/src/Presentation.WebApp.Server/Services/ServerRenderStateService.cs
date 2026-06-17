using Microsoft.AspNetCore.Http;
using Presentation.WebApp.Enums;
using Presentation.WebApp.Services;

namespace Presentation.WebApp.Server.Services;

internal class ServerRenderStateService(IHttpContextAccessor httpContextAccessor) : IRenderStateService
{
    public Guid ServiceUid { get; } = Guid.NewGuid();

    public RenderState RenderState
    {
        get
        {
            var httpContext = httpContextAccessor.HttpContext;
            
            // If HttpContext exists and response hasn't started, we're pre-rendering
            // Once the response starts streaming, we're in SSR mode
            if (httpContext is not null && !httpContext.Response.HasStarted)
            {
                return RenderState.PreRender;
            }
            
            return RenderState.SSR;
        }
    }
}
