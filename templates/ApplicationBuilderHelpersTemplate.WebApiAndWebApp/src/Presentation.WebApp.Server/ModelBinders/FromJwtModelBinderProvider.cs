using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Presentation.WebApp.Server.ModelBinders;

/// <summary>
/// Model binder provider for <see cref="Attributes.FromJwtAttribute"/>.
/// </summary>
public sealed class FromJwtModelBinderProvider : IModelBinderProvider
{
    /// <inheritdoc />
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // Check if the parameter has FromJwtAttribute
        if (context.BindingInfo.BindingSource == BindingSource.Custom)
        {
            // We need to check metadata for FromJwtAttribute
            // This provider will be used when BindingSource is Custom
            return new FromJwtModelBinder();
        }

        return null;
    }
}
