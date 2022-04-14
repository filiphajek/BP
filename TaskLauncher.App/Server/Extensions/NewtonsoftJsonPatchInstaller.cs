using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Options;

namespace TaskLauncher.App.Server.Extensions;

/// <summary>
/// Inspirovano z https://docs.microsoft.com/en-us/aspnet/core/web-api/jsonpatch?view=aspnetcore-6.0
/// Pridava format podporu pro json patch
/// </summary>
public static class NewtonsoftJsonPatchInstaller
{
    public static void AddJsonPatchInputFormatter(this MvcOptions options)
    {
        options.InputFormatters.Insert(0, GetJsonPatchInputFormatter());
    }

    private static NewtonsoftJsonPatchInputFormatter GetJsonPatchInputFormatter()
    {
        var builder = new ServiceCollection()
            .AddLogging()
            .AddMvc()
            .AddNewtonsoftJson()
            .Services.BuildServiceProvider();

        return builder
            .GetRequiredService<IOptions<MvcOptions>>()
            .Value
            .InputFormatters
            .OfType<NewtonsoftJsonPatchInputFormatter>()
            .First();
    }
}
