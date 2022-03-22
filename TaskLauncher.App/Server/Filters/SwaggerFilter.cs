using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace TaskLauncher.App.Server.Filters;

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
