using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ForFitChicken.API.Commerce;

// MVC validates positional-record constructor parameters, not their generated properties.
// Publish the same constraints for generated React clients.
public sealed class RecordValidationSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        var constructor = context.Type.GetConstructors().OrderByDescending(x => x.GetParameters().Length).FirstOrDefault();
        foreach (var parameter in constructor?.GetParameters() ?? [])
        {
            var constraints = parameter.GetCustomAttributes<ValidationAttribute>().ToArray();
            if (constraints.Length == 0 || string.IsNullOrWhiteSpace(parameter.Name)) continue;
            var name = JsonNamingPolicy.CamelCase.ConvertName(parameter.Name!);
            if (!schema.Properties.TryGetValue(name, out var property)) continue;
            foreach (var attribute in constraints)
            {
                switch (attribute)
                {
                    case RequiredAttribute: schema.Required.Add(name); break;
                    case StringLengthAttribute length: property.MaxLength = length.MaximumLength; property.MinLength = length.MinimumLength; break;
                    case MinLengthAttribute minimum:
                        if (property.Type == "array") property.MinItems = minimum.Length; else property.MinLength = minimum.Length;
                        break;
                    case MaxLengthAttribute maximum:
                        if (property.Type == "array") property.MaxItems = maximum.Length; else property.MaxLength = maximum.Length;
                        break;
                    case RangeAttribute range:
                        property.Minimum = Convert.ToDecimal(range.Minimum, CultureInfo.InvariantCulture);
                        property.Maximum = Convert.ToDecimal(range.Maximum, CultureInfo.InvariantCulture); break;
                    case RegularExpressionAttribute regex: property.Pattern = regex.Pattern; break;
                }
            }
        }
    }
}

public sealed class CsrfOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.ApiDescription.HttpMethod is not ("POST" or "PUT" or "PATCH" or "DELETE")) return;
        if (context.MethodInfo.GetCustomAttribute<IgnoreAntiforgeryTokenAttribute>() is not null ||
            context.MethodInfo.DeclaringType?.GetCustomAttribute<IgnoreAntiforgeryTokenAttribute>() is not null) return;
        operation.Parameters ??= new List<OpenApiParameter>();
        operation.Parameters.Add(new OpenApiParameter { Name = "X-CSRF-TOKEN", In = ParameterLocation.Header, Required = true,
            Description = "Get /api/csrf with the same cookie jar. Fetch a fresh token after login.", Schema = new OpenApiSchema { Type = "string" } });
    }
}
