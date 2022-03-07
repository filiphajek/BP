using Mapster;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskLauncher.Api.Contracts.Responses;
using TaskLauncher.Api.Controllers.Base;
using TaskLauncher.Api.DAL;

namespace TaskLauncher.Api.Controllers.Admin;

public class TaskController : AdminODataController<TaskResponse>
{
    public TaskController(AppDbContext context) : base(context) { }

    public override ActionResult<TaskResponse> Get(string userId = "")
    {
        if (string.IsNullOrEmpty(userId))
        {
            return Ok(context.Tasks.IgnoreQueryFilters().ProjectToType<TaskResponse>());
        }
        return Ok(context.Tasks.IgnoreQueryFilters().Where(i => i.UserId == userId).ProjectToType<TaskResponse>());
    }
}