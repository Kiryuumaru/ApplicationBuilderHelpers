using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.Json.Nodes;

namespace Presentation.WebApp.Server.Filters;

internal class AllowedValuesSchemaFilter : ISchemaFilter
{
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema is not OpenApiSchema concreteSchema || context.Type == null)
        {
            return;
        }

        ApplyAllowedValues(context.MemberInfo, concreteSchema);
        ApplyAllowedValues(context.ParameterInfo, concreteSchema);
    }

    private static void ApplyAllowedValues(ICustomAttributeProvider? attributeProvider, OpenApiSchema schema)
    {
        if (attributeProvider == null)
        {
            return;
        }

        var enumAttribute = attributeProvider.GetCustomAttributes(typeof(AllowedValuesAttribute), true)
            .OfType<AllowedValuesAttribute>()
            .FirstOrDefault();

        if (enumAttribute?.Values == null || enumAttribute.Values.Length == 0)
        {
            return;
        }

        schema.Type = JsonSchemaType.String;
        schema.Enum = enumAttribute.Values
            .Select(value => JsonValue.Create(value?.ToString()))
            .OfType<JsonNode>()
            .ToList();
    }
}
