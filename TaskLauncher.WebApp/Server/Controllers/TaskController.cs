using MapsterMapper;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskLauncher.Api.Contracts.Requests;
using TaskLauncher.Api.Contracts.Responses;
using TaskLauncher.Api.DAL.Entities;
using TaskLauncher.Api.DAL.Repositories;
using TaskLauncher.Common.Enums;
using TaskLauncher.Common.Extensions;

namespace TaskLauncher.WebApp.Server.Controllers;

/*
[Authorize(AuthenticationSchemes = "Cookies, Bearer, Auth")]
[ApiController]
public class TaskController : ControllerBase
{
    private readonly IMapper mapper;
    private readonly ITaskRepository taskRepository;
    private readonly IEventRepository eventRepository;

    public TaskController(IMapper mapper, ITaskRepository taskRepository, IEventRepository eventRepository, ILogger<TaskController> logger)
    {
        this.mapper = mapper;
        this.taskRepository = taskRepository;
        this.eventRepository = eventRepository;
    }

    [HttpGet("/api/taskos")]
    public async Task<ActionResult<List<TaskResponse>>> GetAllTasksAsync()
    {
        var list = await taskRepository.GetAllAsync();
        return Ok(list.Select(mapper.Map<TaskResponse>));
    }
}*/