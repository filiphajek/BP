using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Attributes;
using Microsoft.EntityFrameworkCore;
using TaskLauncher.App.DAL.Entities;
using TaskLauncher.App.DAL;

namespace TaskLauncher.App.Server.Controllers.Base;

/// <summary>
/// Abstraktni bazova trida pro admin kontrolery s odata
/// </summary>
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
}
