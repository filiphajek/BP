using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskLauncher.Api.Contracts.Requests;
using TaskLauncher.Api.Contracts.Responses;
using TaskLauncher.Api.DAL.Entities;
using TaskLauncher.Api.DAL.Repositories;
using TaskLauncher.Common.Enums;
using TaskLauncher.Common.Extensions;

namespace TaskLauncher.Api.Controllers;

[Authorize(Policy = "p-user-api-auth0")]
public class TaskController : BaseController
{
    private readonly IMapper mapper;
    private readonly ITaskRepository taskRepository;
    private readonly IEventRepository eventRepository;

    public TaskController(IMapper mapper, ITaskRepository taskRepository, IEventRepository eventRepository, ILogger<TaskController> logger) 
        : base(logger)
    {
        this.mapper = mapper;
        this.taskRepository = taskRepository;
        this.eventRepository = eventRepository;
    }

    /// <summary>
    /// Vraci vsechny tasky
    /// TODO PAGING
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<TaskResponse>>> GetAllTasksAsync()
    {
        var list = await taskRepository.GetAllAsync();
        return Ok(list.Select(mapper.Map<TaskResponse>));
    }

    /// <summary>
    /// Vraci detail tasku
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TaskDetailResponse>> GetTaskDetailAsync(Guid id)
    {
        var task = await taskRepository.GetAsync(new() { Id = id });
        if (task is null)
            return NotFound();
        return Ok(mapper.Map<TaskDetailResponse>(task));
    }

    /// <summary>
    /// Vytvoreni noveho tasku
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<TaskResponse>> CreateTaskAsync([FromBody] TaskCreateRequest request)
    {
        if (!User.TryGetAuth0Id(out var userId))
            return Unauthorized();

        var eventEntity = new EventEntity { Status = TaskState.Created, Time = DateTime.Now, UserId = userId };
        TaskEntity task = new()
        {
            ActualStatus = TaskState.Created,
            UserId = userId,
            Events = new List<EventEntity> { eventEntity }
        };
        var result = await taskRepository.AddAsync(mapper.Map(request, task));

        //TODO rabbitMQ message (info o souboru atd .. worker aby si to stahl tak bude mit credentials na google) + polly
        //az bude worker uploadovat soubor tak rabbitmq message a pred tim to da na google

        return Ok(mapper.Map<TaskResponse>(result));
    }
    
    /// <summary>
    /// Aktualizace informaci o tasku
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<TaskResponse>> UpdateTaskAsync([FromRoute] Guid id, [FromBody]TaskUpdateRequest request)
    {
        var result = await taskRepository.GetAsync(new() { Id = id });
        if (result is null)
            return NotFound();
        var tmp = mapper.Map(request, result);
        await taskRepository.UpdateAsync(tmp);
        return Ok(tmp);
    }

    /// <summary>
    /// Smazani tasku
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> RemoveTaskAsync(Guid id)
    {
        if (!User.TryGetAuth0Id(out var userId))
            return Unauthorized();

        var task = await taskRepository.GetAsync(new() { Id = id });
        if (task is null)
            return BadRequest();

        if (task.ActualStatus == TaskState.InQueue || task.ActualStatus == TaskState.Running)
            return BadRequest();

        var eventEntity = new EventEntity { Status = TaskState.Deleted, Time = DateTime.Now, UserId = userId, Task = task };

        await eventRepository.AddAsync(eventEntity);
        task.ActualStatus = TaskState.Deleted; // uplne smazat to muze pouze admin
        await taskRepository.UpdateAsync(task);
        return Ok();
    }

    /// <summary>
    /// Je soubor dostupny
    /// Soubor se mohl smazat nebo se jeste nedokoncil task
    /// </summary>
    [HttpGet("{id:guid}/{file}")]
    public async Task<IActionResult> FileAvailableAsync([FromRoute] Guid id, string file)
    {
        var task = await taskRepository.GetAsync(new() { Id = id });
        if (task is null)
            return BadRequest();

        if (file == "result")
        {
            if (!string.IsNullOrEmpty(task.ResultFile))
                return Ok();
            return BadRequest();
        }
        if (file == "task")
        {
            if (!string.IsNullOrEmpty(task.TaskFile))
                return Ok();
            return BadRequest();
        }
        return BadRequest();
    }
}