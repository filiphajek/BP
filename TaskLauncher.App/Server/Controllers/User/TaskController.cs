using Mapster;
using MapsterMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore;
using TaskLauncher.Api.Contracts.Requests;
using TaskLauncher.Api.Contracts.Responses;
using TaskLauncher.Authorization;
using TaskLauncher.Common.Enums;
using TaskLauncher.Common.Extensions;
using TaskLauncher.Common.Services;
using TaskLauncher.App.DAL.Repositories;
using TaskLauncher.App.DAL.Entities;
using TaskLauncher.App.DAL;
using TaskLauncher.App.Server.Controllers.Base;
using TaskLauncher.App.Server.Tasks;
using TaskLauncher.Common.Models;

namespace TaskLauncher.App.Server.Controllers.User;

public class TaskController : UserODataController<TaskResponse>
{
    private readonly IMapper mapper;
    private readonly ITaskRepository taskRepository;
    private readonly IEventRepository eventRepository;
    private readonly IFileStorageService fileStorageService;
    private readonly Balancer balancer;

    public TaskController(AppDbContext context, IMapper mapper,
        ITaskRepository taskRepository,
        IEventRepository eventRepository,
        IFileStorageService fileStorageService, 
        Balancer balancer) : base(context)
    {
        this.mapper = mapper;
        this.taskRepository = taskRepository;
        this.eventRepository = eventRepository;
        this.fileStorageService = fileStorageService;
        this.balancer = balancer;
    }

    [HttpGet]
    [EnableQuery]
    public ActionResult<TaskResponse> Get()
    {
        return Ok(context.Tasks.AsQueryable().ProjectToType<TaskResponse>());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TaskDetailResponse>> GetDetail(Guid id)
    {
        var task = await context.Tasks.Include(i => i.Events).SingleOrDefaultAsync(i => i.Id == id);
        if (task is null)
            return NotFound();
        return Ok(mapper.Map<TaskDetailResponse>(task));
    }

    private readonly SemaphoreSlim semaphoreSlim = new(1, 1);

    /// <summary>
    /// Vytvoreni noveho tasku
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<TaskResponse>> CreateTaskAsync([FromForm] TaskCreateRequest request, IFormFile file)
    {
        if (!User.TryGetAuth0Id(out var userId))
            return Unauthorized();

        double price = 0;
        if (User.TryGetClaimAsBool(TaskLauncherClaimTypes.Vip, out bool vip) && vip)
        {
            var tmp = (await context.Configs.SingleAsync(i => i.Key == "normaltaskprice")).Value;
            price = double.Parse(tmp);
        }
        else
        {
            var tmp = (await context.Configs.SingleAsync(i => i.Key == "viptaskprice")).Value;
            price = double.Parse(tmp);
        }

        await semaphoreSlim.WaitAsync();
        var token = await context.TokenBalances.SingleOrDefaultAsync();
        if (token is null || token.CurrentAmount <= 0)
            return BadRequest("No balance");

        token.CurrentAmount -= price;
        context.Update(token);
        await context.SaveChangesAsync();
        semaphoreSlim.Release();

        var fileId = file.Name + DateTime.Now.Ticks.ToString();
        var path = $"{userId}/{fileId}/task";
        using (var stream = file.OpenReadStream())
        {
            await fileStorageService.UploadFileAsync(path, stream);
        }

        var eventEntity = new EventEntity { Status = TaskState.Created, Time = DateTime.Now, UserId = userId };
        TaskEntity task = new()
        {
            ActualStatus = TaskState.Created,
            UserId = userId,
            TaskFile = path,
            ResultFile = path,
            Events = new List<EventEntity> { eventEntity }
        };

        var taskEntity = mapper.Map(request, task);
        var result = await context.Tasks.AddAsync(taskEntity);
        await context.Payments.AddAsync(new() { Price = price, Task = taskEntity, Time = DateTime.Now, UserId = userId });
        await context.SaveChangesAsync();

        balancer.Enqueue(vip ? "vip" : "nonvip", new TaskModel
        {
            Id = task.Id,
            State = TaskState.Created,
            Time = DateTime.Now,
            TaskFilePath = task.TaskFile,
            UserId = task.UserId
        });

        return Ok(mapper.Map<TaskResponse>(result));
    }

    /// <summary>
    /// Aktualizace informaci o tasku
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<TaskResponse>> UpdateTaskAsync([FromRoute] Guid id, [FromBody] TaskUpdateRequest request)
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

        if (task.ActualStatus == TaskState.Running)
            return BadRequest();

        var eventEntity = new EventEntity { Status = TaskState.Closed, Time = DateTime.Now, UserId = userId, Task = task };

        await eventRepository.AddAsync(eventEntity);
        task.ActualStatus = TaskState.Closed; // uplne smazat to muze pouze admin
        await taskRepository.UpdateAsync(task);
        return Ok();
    }

    /// <summary>
    /// Zruseni tasku
    /// </summary>
    [HttpPost("cancel")]
    public async Task<IActionResult> CancelTaskAsync(Guid taskId)
    {
        var task = await context.Tasks.SingleOrDefaultAsync(i => i.Id == taskId);
        if (task is null)
            return BadRequest();

        if (task.ActualStatus == TaskState.Created || task.ActualStatus == TaskState.Running || task.ActualStatus == TaskState.Ready)
        {
            balancer.CancelTask(taskId);
            task.ActualStatus = TaskState.Cancelled;
            context.Update(task);
            await context.SaveChangesAsync();
            return Ok();
        }
        return BadRequest();
    }

    /// <summary>
    /// Stazeni vysledku tasku
    /// </summary>
    [HttpGet("file")]
    public async Task<IActionResult> DownloadFileAsync(Guid taskId)
    {
        if (!User.TryGetAuth0Id(out var userId))
            return Unauthorized();

        var task = await context.Tasks.SingleOrDefaultAsync(i => i.Id == taskId);
        if (task is null)
            return NotFound();

        if (task.ActualStatus == TaskState.Finished)
        {
            task.ActualStatus = TaskState.Downloaded;
            context.Update(task);
            await context.Events.AddAsync(new() { Task = task, Status = TaskState.Downloaded, Time = DateTime.Now, UserId = userId });
            await context.SaveChangesAsync();
            MemoryStream stream = new();
            await fileStorageService.DownloadFileAsync(task.ResultFile, stream);
            return File(stream, "application/octet-stream", task.Name);
        }

        if(task.ActualStatus == TaskState.Downloaded)
        {
            MemoryStream stream = new();
            await fileStorageService.DownloadFileAsync(task.ResultFile, stream);
            return File(stream, "application/octet-stream", task.Name);
        }
        return BadRequest();
    }

    [HttpPost("close")]
    public async Task<IActionResult> CloseTaskAsync(Guid taskId)
    {
        if (!User.TryGetAuth0Id(out var userId))
            return Unauthorized();

        var task = await context.Tasks.SingleOrDefaultAsync(i => i.Id == taskId);
        if (task is null)
            return NotFound();

        if (task.ActualStatus != TaskState.Downloaded)
            return BadRequest();

        task.ActualStatus = TaskState.Closed;
        context.Update(task);
        await context.Events.AddAsync(new() { Task = task, Status = TaskState.Closed, Time = DateTime.Now, UserId = userId });
        await context.SaveChangesAsync();
        return Ok();
    }

    [HttpPost("restart")]
    public async Task<IActionResult> RestartTaskAsync(Guid taskId)
    {
        if (!User.TryGetAuth0Id(out var userId))
            return Unauthorized();

        var task = await context.Tasks.SingleOrDefaultAsync(i => i.Id == taskId);
        if (task is null)
            return NotFound();

        if (task.ActualStatus == TaskState.Cancelled || task.ActualStatus == TaskState.Crashed)
        {
            task.ActualStatus = TaskState.Created;
            context.Update(task);
            await context.Events.AddAsync(new() { Task = task, Status = TaskState.Created, Time = DateTime.Now, UserId = userId });
            await context.SaveChangesAsync();

            User.TryGetClaimAsBool(TaskLauncherClaimTypes.Vip, out bool vip);
            balancer.Enqueue(vip ? "vip" : "nonvip", new TaskModel
            {
                Id = task.Id,
                State = TaskState.Created,
                Time = DateTime.Now,
                TaskFilePath = task.TaskFile,
                UserId = task.UserId
            });
            return Ok();
        }
        return BadRequest();
    }
}