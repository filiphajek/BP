using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace TaskLauncher.Authorization.Auth0;

public interface ISynchronized
{
    bool Value { get; }
}

public class Synchronized : ISynchronized
{
    public Synchronized(bool value)
    {
        Value = value;
    }
    public bool Value { get; }
}

//mohl bych vyuzit ValidationFilter pod next() nebo vlastni filter ale musim dat order
public class SynchronizerMiddleware
{
    private readonly RequestDelegate next;

    public SynchronizerMiddleware(RequestDelegate next)
    {
        this.next = next;
    }

    public async Task InvokeAsync(HttpContext context, ClaimsPrincipalSynchronizer principalSynchronizer)
    {
        await next(context);

        var feature = context.Features.Get<ISynchronized>();
        if (feature is null || !feature.Value)
        {
            // stahni z auth0 usera a porovnej claimy
            // pokud se nektery updatujou -> refreshni principal
        }
    }
}

public static class SynchronizerExtensions
{
    public static IApplicationBuilder UseSynchronizeMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<SynchronizerMiddleware>();
    }
}

public class ClaimsPrincipalSynchronizer
{
    private readonly ManagementApiClientFactory clientFactory;
    private readonly IHttpContextAccessor httpContextAccessor;

    public ClaimsPrincipalSynchronizer(ManagementApiClientFactory clientFactory, IHttpContextAccessor httpContextAccessor)
    {
        this.clientFactory = clientFactory;
        this.httpContextAccessor = httpContextAccessor;
    }

    public Task Compare()
    {
        return Task.CompletedTask;
    }

    public Task Refresh()
    {
        return Task.CompletedTask;
    }

    public Task Synchronize()
    {
        //updatni vsechno, reload principal
        //uloz to do featury
        httpContextAccessor.HttpContext.Features.Set<ISynchronized>(new Synchronized(true));

        return Task.CompletedTask;
    }
}