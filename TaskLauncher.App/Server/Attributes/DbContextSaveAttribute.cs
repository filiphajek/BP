using Microsoft.AspNetCore.Mvc.Filters;
using TaskLauncher.App.DAL;

namespace TaskLauncher.App.Server.Attributes;

/// <summary>
/// Uklada db context
/// Trva radove nanosekund pokud nedochazi k sql operacim
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class DbContextSaveAttribute : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        //https://www.infoworld.com/article/3544006/how-to-use-dependency-injection-in-action-filters-in-aspnet-core-31.html
        var dbContext = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
        await next();
        await dbContext.SaveChangesAsync();
    }
}