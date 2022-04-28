using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace TaskLauncher.App.Server.Filters;

/// <summary>
/// Převzato z https://github.com/rgudkov-uss/aspnetcore-net6-odata-swagger-versioned
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
                Description = "Jméno některé vlastnosti daného objektu",
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
                Description = "Filtrování podle podmínky, více zde https://www.odata.org/getting-started/basic-tutorial/#filter",
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
                Description = "Maximální počet objektů v odpovědi",
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
                Description = "Přeskočí určitý počet objektů https://www.odata.org/getting-started/basic-tutorial/#select",
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
                Description = "Seřazení objektů",
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
                Description = "Pokud je nastaven na true, vrací se, kolik objektů je v kolekci",
                In = ParameterLocation.Query,
                Schema = new OpenApiSchema
                {
                    Type = "boolean",
                },
                Required = false,
            });
    }
}
