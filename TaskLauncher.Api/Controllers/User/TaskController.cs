using Mapster;
using MapsterMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore;
using TaskLauncher.Api.Contracts.Requests;
using TaskLauncher.Api.Contracts.Responses;
using TaskLauncher.Api.Controllers.Base;
using TaskLauncher.Api.DAL;
using TaskLauncher.Api.DAL.Entities;
using TaskLauncher.Api.DAL.Repositories;
using TaskLauncher.Common.Enums;
using TaskLauncher.Common.Extensions;
using TaskLauncher.Common.Services;

namespace TaskLauncher.Api.Controllers.User;

public class TaskController : UserODataController<TaskResponse>
{
    private readonly IMapper mapper;
    private readonly ITaskRepository taskRepository;
    private readonly IEventRepository eventRepository;
    private readonly ITokenBalanceRepository tokenRepository;
    private readonly IPaymentRepository paymentRepository;
    private readonly IFileStorageService fileStorageService;

    public TaskController(AppDbContext context, IMapper mapper, 
        ITaskRepository taskRepository, 
        IEventRepository eventRepository, 
        ITokenBalanceRepository tokenRepository, 
        IPaymentRepository paymentRepository, 
        IFileStorageService fileStorageService) : base(context)
    {
        this.mapper = mapper;
        this.taskRepository = taskRepository;
        this.eventRepository = eventRepository;
        this.tokenRepository = tokenRepository;
        this.paymentRepository = paymentRepository;
        this.fileStorageService = fileStorageService;
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

    /// <summary>
    /// Vytvoreni noveho tasku
    /// </summary>
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

        if (token.CurrentAmount <= 0)
            return BadRequest("No balance");

        token.CurrentAmount--;
        await tokenRepository.UpdateAsync(token);
        var result = await taskRepository.AddAsync(taskEntity);
        await paymentRepository.AddAsync(new() { Price = 1, Task = taskEntity, Time = DateTime.Now, UserId = userId }); // TODO price bude adminem konfigurovatelna

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

        if (task.ActualStatus == TaskState.InQueue || task.ActualStatus == TaskState.Running)
            return BadRequest();

        var eventEntity = new EventEntity { Status = TaskState.Deleted, Time = DateTime.Now, UserId = userId, Task = task };

        await eventRepository.AddAsync(eventEntity);
        task.ActualStatus = TaskState.Deleted; // uplne smazat to muze pouze admin
        await taskRepository.UpdateAsync(task);
        return Ok();
    }
}