using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Attributes;
using TaskLauncher.Api.DAL;
using TaskLauncher.Api.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace TaskLauncher.Api.Controllers.Base;

[ODataRouteComponent("odata/admin")]
[Route("api/admin/[controller]")]
[Authorize(Policy = "admin-policy")]
public abstract class AdminODataController<TResponse> : ControllerBase
    where TResponse : class
{
    protected readonly AppDbContext context;

    public AdminODataController(AppDbContext context)
    {
        this.context = context;
    }

    [HttpGet]
    [EnableQuery]
    public abstract ActionResult<IQueryable<TResponse>> Get(string userId = "");
}

public abstract class AdminODataController<TEntity,TResponse> : ControllerBase
    where TEntity : class, IUserKeyProtection
    where TResponse : class
{
    protected readonly AppDbContext context;

    public AdminODataController(AppDbContext context)
    {
        this.context = context;
    }

    public ActionResult<IQueryable<TResponse>> Get(string userId = "")
    {
        if (string.IsNullOrEmpty(userId))
        {
            return Ok(context.Set<TEntity>().IgnoreQueryFilters().ProjectToType<TResponse>());
        }
        return Ok(context.Set<TEntity>().IgnoreQueryFilters().Where(i => i.UserId == userId).ProjectToType<TResponse>());
    }
}
