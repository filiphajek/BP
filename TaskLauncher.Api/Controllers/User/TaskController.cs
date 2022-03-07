using Mapster;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using TaskLauncher.Api.Contracts.Responses;
using TaskLauncher.Api.Controllers.Base;
using TaskLauncher.Api.DAL;
using TaskLauncher.Api.DAL.Entities;

namespace TaskLauncher.Api.Controllers.User;

public class TaskController : UserODataController<TaskEntity>
{
    public TaskController(AppDbContext context) : base(context) { }

    [HttpGet]
    [EnableQuery]
    public ActionResult<TaskResponse> Get()
    {
        return Ok(context.Tasks.AsQueryable().ProjectToType<TaskResponse>());
    }
}