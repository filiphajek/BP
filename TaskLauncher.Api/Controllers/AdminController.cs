using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskLauncher.Api.Contracts.Responses;
using TaskLauncher.Api.DAL;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.OData.Query;
using Mapster;

namespace TaskLauncher.Api.Controllers;

[Authorize(Policy = "admin-policy")]
[Route("/api/admin")]
[ApiController]
public class AdminController : ControllerBase
{
    private readonly AppDbContext context;

    public AdminController(AppDbContext context)
    {
        this.context = context;
    }
    
    [EnableQuery]
    public IActionResult GetQueue() // https://localhost:5001/odata/Admin
    {
        return Ok(context.Tasks.IgnoreQueryFilters().ProjectToType<TaskResponse>());
    }
}
