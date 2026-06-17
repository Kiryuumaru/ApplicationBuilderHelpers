using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Presentation.WebApi.Attributes;

/// <summary>
/// Binds a parameter value from a JWT claim using <see cref="System.Security.Claims.ClaimsPrincipal.FindFirst(string)"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
public sealed class FromJwtAttribute : Attribute, IBindingSourceMetadata
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FromJwtAttribute"/> class.
    /// </summary>
    /// <param name="claimType">The claim type to extract from the JWT (e.g., <see cref="System.Security.Claims.ClaimTypes.NameIdentifier"/>).</param>
    public FromJwtAttribute(string claimType)
    {
        if (string.IsNullOrWhiteSpace(claimType))
        {
            throw new ArgumentException("Claim type cannot be null or whitespace.", nameof(claimType));
        }

        ClaimType = claimType;
    }

    /// <summary>
    /// Gets the claim type to extract from the JWT.
    /// </summary>
    public string ClaimType { get; }

    /// <summary>
    /// Gets the binding source for this attribute.
    /// </summary>
    public BindingSource BindingSource => BindingSource.Custom;
}
