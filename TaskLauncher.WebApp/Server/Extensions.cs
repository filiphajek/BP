using Ocelot.Configuration;
using Ocelot.Configuration.Repository;
using Ocelot.DownstreamRouteFinder;
using Ocelot.DownstreamRouteFinder.Finder;
using Ocelot.Middleware;
using Ocelot.Responses;

namespace TaskLauncher.WebApp.Server;

public static class Extensions
{
    public static IApplicationBuilder UseOcelotWhenRouteMatch(this IApplicationBuilder app)
    {
        app.MapWhen(delegate (HttpContext context)
        {
            Response<IInternalConfiguration> response = context.RequestServices.GetRequiredService<IInternalConfigurationRepository>().Get();
            if (response.IsError || response.Data.Routes.Count == 0)
            {
                return false;
            }

            IInternalConfiguration data = response.Data;
            IDownstreamRouteProvider downstreamRouteProvider = context.RequestServices.GetRequiredService<IDownstreamRouteProviderFactory>().Get(data);
            Response<DownstreamRouteHolder> response2 = downstreamRouteProvider.Get((string)context.Request.Path, context.Request.QueryString.ToString(), context.Request.Method, data, context.Request.Host.ToString());

            var tmp = !response2.IsError && !string.IsNullOrEmpty(response2.Data?.Route?.DownstreamRoute?.FirstOrDefault()?.DownstreamScheme);
            return tmp;
        }, delegate (IApplicationBuilder appBuilder)
        {
            appBuilder.UseOcelot().Wait();
        });
        return app;
    }
}
