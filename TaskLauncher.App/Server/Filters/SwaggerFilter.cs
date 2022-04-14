using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Writers;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace TaskLauncher.App.Server.Filters;

/// <summary>
/// Filter pro swagger, ktery odstanuje vsechny odata endpointy
/// </summary>
public class SwaggerFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        foreach (var route in swaggerDoc.Paths.Where(x => x.Key.ToLower().Contains("odata")))
        {
            swaggerDoc.Paths.Remove(route.Key);
        }
    }
}

/// <summary>
/// https://stackoverflow.com/questions/58973329/asp-net-core-3-0-swashbuckle-restrict-responses-content-types-globally
/// </summary>
public class ContentTypeOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (operation.RequestBody == null)
        {
            return;
        }

        /*operation.RequestBody.Content = new Dictionary<string, OpenApiMediaType>
        {
            { "application/json",  new OpenApiMediaType() }
        };*/

        foreach (var response in operation.Responses)
        {
            response.Value.Content = new Dictionary<string, OpenApiMediaType>
            {
                { "application/json",  new OpenApiMediaType() }
            };
        }
    }
}

/// <summary>
/// https://michael-mckenna.com/swagger-with-asp-net-core-3-1-json-patch/
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

/// <summary>
/// https://github.com/rgudkov-uss/aspnetcore-net6-odata-swagger-versioned
/// </summary>
public class ODataOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (operation.Parameters == null) operation.Parameters = new List<OpenApiParameter>();

        var descriptor = context.ApiDescription.ActionDescriptor as ControllerActionDescriptor;
        var queryAttr = descriptor?.FilterDescriptors
            .Where(fd => fd.Filter is EnableQueryAttribute)
            .Select(fd => fd.Filter as EnableQueryAttribute)
            .FirstOrDefault();

        if (queryAttr == null)
            return;

        if (queryAttr.AllowedQueryOptions.HasFlag(AllowedQueryOptions.Select))
            operation.Parameters.Add(new OpenApiParameter()
            {
                Name = "$select",
                In = ParameterLocation.Query,
                Schema = new OpenApiSchema
                {
                    Type = "string",
                },
                Required = false
            });

        if (queryAttr.AllowedQueryOptions.HasFlag(AllowedQueryOptions.Expand))
            operation.Parameters.Add(new OpenApiParameter()
            {
                Name = "$expand",
                In = ParameterLocation.Query,
                Schema = new OpenApiSchema
                {
                    Type = "string",
                },
                Required = false
            });


        if (queryAttr.AllowedQueryOptions.HasFlag(AllowedQueryOptions.Filter))
            operation.Parameters.Add(new OpenApiParameter()
            {
                Name = "$filter",
                In = ParameterLocation.Query,
                Schema = new OpenApiSchema
                {
                    Type = "string",
                },
                Required = false
            });


        if (queryAttr.AllowedQueryOptions.HasFlag(AllowedQueryOptions.Top))
            operation.Parameters.Add(new OpenApiParameter()
            {
                Name = "$top",
                In = ParameterLocation.Query,
                Schema = new OpenApiSchema
                {
                    Type = "string",
                },
                Required = false
            });

        if (queryAttr.AllowedQueryOptions.HasFlag(AllowedQueryOptions.Skip))
            operation.Parameters.Add(new OpenApiParameter()
            {
                Name = "$skip",
                In = ParameterLocation.Query,
                Schema = new OpenApiSchema
                {
                    Type = "string",
                },
                Required = false
            });


        if (queryAttr.AllowedQueryOptions.HasFlag(AllowedQueryOptions.OrderBy))
            operation.Parameters.Add(new OpenApiParameter()
            {
                Name = "$orderby",
                In = ParameterLocation.Query,
                Schema = new OpenApiSchema
                {
                    Type = "string",
                },
                Required = false
            });

        if (queryAttr.AllowedQueryOptions.HasFlag(AllowedQueryOptions.Count))
            operation.Parameters.Add(new OpenApiParameter()
            {
                Name = "$count",
                In = ParameterLocation.Query,
                Schema = new OpenApiSchema
                {
                    Type = "boolean",
                },
                Required = false,
            });
    }
}
