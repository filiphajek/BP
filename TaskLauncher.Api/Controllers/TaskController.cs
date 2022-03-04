using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskLauncher.Api.Contracts.Requests;
using TaskLauncher.Api.Contracts.Responses;
using TaskLauncher.Api.DAL.Entities;
using TaskLauncher.Api.DAL.Repositories;
using TaskLauncher.Common.Enums;
using TaskLauncher.Common.Extensions;
using TaskLauncher.Common.Messages;
using TaskLauncher.Common.Services;
using TaskLauncher.Common.RawRabbit;
using Microsoft.AspNetCore.Mvc.Filters;
using TaskLauncher.Api.DAL;
using Microsoft.EntityFrameworkCore;

namespace TaskLauncher.Api.Controllers;

/// <summary>
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

[Authorize(Policy = "user-policy")]
public class TasksController : BaseController
{
    private readonly IMapper mapper;
    private readonly ITaskRepository taskRepository;
    private readonly IEventRepository eventRepository;
    private readonly ITokenBalanceRepository tokenRepository;
    private readonly IPaymentRepository paymentRepository;
    private readonly IFileStorageService fileStorageService;
    private readonly IDefaultRabbitMQClient busClient;
    private readonly AppDbContext context;

    public TasksController(IMapper mapper, 
        ITaskRepository taskRepository, 
        IEventRepository eventRepository,
        ITokenBalanceRepository tokenRepository,
        IPaymentRepository paymentRepository,
        ILogger<TasksController> logger, 
        IFileStorageService fileStorageService,
        IDefaultRabbitMQClient busClient,
        AppDbContext context)
        : base(logger)
    {
        this.mapper = mapper;
        this.taskRepository = taskRepository;
        this.eventRepository = eventRepository;
        this.tokenRepository = tokenRepository;
        this.paymentRepository = paymentRepository;
        this.fileStorageService = fileStorageService;
        this.busClient = busClient;
        this.context = context;
    }

    [Authorize(Policy = "admin-policy")]
    [HttpGet("/api/{id}/tasks")]
    public async Task<ActionResult<List<TaskResponse>>> GetAllTasksAsync([FromRoute] string id)
    {
        var list = await context.Tasks.IgnoreQueryFilters().Where(i => i.UserId == id).ToListAsync();
        return Ok(list.Select(mapper.Map<TaskResponse>));
    }

    /// <summary>
    /// Vraci vsechny tasky
    /// TODO PAGING
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<TaskResponse>>> GetAllTasksAsync()
    {
        if (User.IsInRole("admin"))
            return Ok(new List<TaskResponse>());
        var list = await taskRepository.GetAllAsync();
        return Ok(list.Where(i => i.ActualStatus != TaskState.Deleted).Select(mapper.Map<TaskResponse>));
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
    [DbContextSave]
    [HttpPost]
    public async Task<ActionResult<TaskResponse>> CreateTaskAsync([FromForm] TaskCreateRequest request, IFormFile file)
    {
        if (!User.TryGetAuth0Id(out var userId))
            return Unauthorized();

        var fileId = file.Name + DateTime.Now.Ticks.ToString();
        using (var stream = file.OpenReadStream())
        {
            await fileStorageService.UploadFileAsync($"{userId}/{fileId}/task", stream); // todo udelat pro to builder, nebo cestu ulozit do db
        }

        var eventEntity = new EventEntity { Status = TaskState.InQueue, Time = DateTime.Now, UserId = userId };
        TaskEntity task = new()
        {
            ActualStatus = TaskState.Created,
            UserId = userId,
            TaskFile = fileId,
            Events = new List<EventEntity> { eventEntity }
        };

        var taskEntity = mapper.Map(request, task);

        var token = (await tokenRepository.GetAllAsync()).FirstOrDefault();
        if (token is null)
            return BadRequest("No balance");

        if(token.CurrentAmount <= 0)
            return BadRequest("No balance");

        token.CurrentAmount--;
        await tokenRepository.UpdateAsync(token);
        var result = await taskRepository.AddAsync(taskEntity);
        await paymentRepository.AddAsync(new() { Price = 1, Task = taskEntity, Time = DateTime.Now, UserId = userId }); // TODO price bude adminem konfigurovatelna

        await busClient.PublishAsync(new TaskCreated { });

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