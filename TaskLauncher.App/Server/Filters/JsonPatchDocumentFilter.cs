using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace TaskLauncher.App.Server.Filters;

/// <summary>
/// Převzato z https://michael-mckenna.com/swagger-with-asp-net-core-3-1-json-patch/ od Michaela McKenna
/// </summary>
public class JsonPatchDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        var schemas = swaggerDoc.Components.Schemas.ToList();
        foreach (var item in schemas)
        {
            if (item.Key.StartsWith("OperationOf") || item.Key.StartsWith("JsonPatchDocumentOf"))
                swaggerDoc.Components.Schemas.Remove(item.Key);
        }

        swaggerDoc.Components.Schemas.Add("Operation", new OpenApiSchema
        {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema>
            {
                {"op", new OpenApiSchema{ Type = "string", Example = new OpenApiString("replace") } },
                {"value", new OpenApiSchema{ Type = "object", Nullable = true, Example = new OpenApiString("/name") } },
                {"path", new OpenApiSchema{ Type = "string", Example = new OpenApiString("New value") } }
            }
        });

        swaggerDoc.Components.Schemas.Add("JsonPatchDocument", new OpenApiSchema
        {
            Type = "array",
            Items = new OpenApiSchema
            {
                Reference = new OpenApiReference { Type = ReferenceType.Schema, Id = "Operation" }
            },
            Description = "Array of operations to perform"
        });

        foreach (var path in swaggerDoc.Paths.SelectMany(p => p.Value.Operations)
            .Where(p => p.Key == OperationType.Patch))
        {
            foreach (var item in path.Value.RequestBody.Content.Where(c => c.Key != "application/json-patch+json"))
                path.Value.RequestBody.Content.Remove(item.Key);
            var response = path.Value.RequestBody.Content.Single(c => c.Key == "application/json-patch+json");
            response.Value.Schema = new OpenApiSchema
            {
                Reference = new OpenApiReference { Type = ReferenceType.Schema, Id = "JsonPatchDocument" }
            };
        }
    }
}
