using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Attributes;
using TaskLauncher.Api.DAL;
using TaskLauncher.Api.DAL.Entities;

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
    public abstract ActionResult<TResponse> Get(string userId = "");
}
