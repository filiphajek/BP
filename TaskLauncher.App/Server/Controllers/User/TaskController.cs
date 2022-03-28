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
using TaskLauncher.App.DAL.Entities;
using TaskLauncher.App.DAL;
using TaskLauncher.App.Server.Controllers.Base;
using TaskLauncher.App.Server.Tasks;
using TaskLauncher.Common.Models;
using System.Text.RegularExpressions;

namespace TaskLauncher.App.Server.Controllers.User;

public class TaskController : UserODataController<TaskResponse>
{
    private readonly IMapper mapper;
    private readonly IFileStorageService fileStorageService;
    private readonly Balancer balancer;
    private readonly SemaphoreSlim semaphoreSlim = new(1, 1); // semafor pro odecitani ze zustatku

    public TaskController(AppDbContext context, IMapper mapper, IFileStorageService fileStorageService, Balancer balancer) : base(context)
    {
        this.mapper = mapper;
        this.fileStorageService = fileStorageService;
        this.balancer = balancer;
    }

    /// <summary>
    /// Zpristupnuje dotazovani nad celou kolekci tasku daneho uzivatele
    /// </summary>
    [HttpGet]
    [EnableQuery]
    public ActionResult<TaskResponse> Get()
    {
        return Ok(context.Tasks.Where(i => i.ActualStatus != TaskState.Closed).AsQueryable().ProjectToType<TaskResponse>());
    }

    /// <summary>
    /// Detail tasku, s taskem se vraci i platba a vsechny udalosti
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TaskDetailResponse>> GetDetail(Guid id)
    {
        var payment = await context.Payments.Include(i => i.Task).ThenInclude(i => i.Events).SingleOrDefaultAsync(i => i.Task.Id == id);
        if (payment is not null)
        {
            var taskResponse = mapper.Map<TaskDetailResponse>(payment.Task);
            taskResponse.Payment = mapper.Map<SimplePaymentResponse>(payment);
            return Ok(taskResponse);
        }

        var task = await context.Tasks.Include(i => i.Events).SingleOrDefaultAsync(i => i.Id == id);
        if (task is null)
            return NotFound();
        return Ok(mapper.Map<TaskDetailResponse>(task));
    }

    //regex pro jmeno tasku
    private static readonly Regex taskNameRegex = new(@"(?<=\()(\d+)(?=\))", RegexOptions.Compiled, TimeSpan.FromSeconds(2));
    
    /// <summary>
    /// Pomocna metoda pro zaruceni originalniho jmena taska (pridava (x) ve jmene pokud dane jmeno existuje)
    /// </summary>
    private async Task<string> GetUniqueName(string name)
    {
        bool exists = await context.Tasks.AnyAsync(i => i.Name == name);
        if (!exists)
            return name;

        int index = 0;
        string tmpExistingName = "";
        await foreach (var existingNameTask in context.Tasks.AsNoTracking().Where(i => i.Name.StartsWith(name)).AsAsyncEnumerable())
        {
            var match = taskNameRegex.Match(existingNameTask.Name);
            if (match.Success && int.TryParse(match.Value, out var number) && number >= index)
            {
                index = number;
                tmpExistingName = existingNameTask.Name;
            }
        }

        if (index == 0)
            return name += " (1)";
     
        index++;
        return taskNameRegex.Replace(tmpExistingName, index.ToString());
    }

    /// <summary>
    /// Vytvoreni noveho tasku
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<TaskResponse>> CreateTaskAsync([FromForm] TaskCreateRequest request, IFormFile file)
    {
        if (!User.TryGetAuth0Id(out var userId))
            return Unauthorized();

        if (request.Name.Contains('(') || request.Name.Contains(')'))
            return BadRequest();

        request.Name = await GetUniqueName(request.Name);

        double price = 0;
        if (User.TryGetClaimAsBool(TaskLauncherClaimTypes.Vip, out bool vip) && vip)
        {
            var tmp = (await context.Configs.SingleAsync(i => i.Key == "viptaskprice")).Value;
            price = double.Parse(tmp);
        }
        else
        {
            var tmp = (await context.Configs.SingleAsync(i => i.Key == "normaltaskprice")).Value;
            price = double.Parse(tmp);
        }

        await semaphoreSlim.WaitAsync();
        var token = await context.TokenBalances.SingleOrDefaultAsync();
        if (token is null || token.CurrentAmount <= 0)
            return BadRequest(new ErrorMessageResponse("No balance"));

        token.CurrentAmount -= price;

        if(token.CurrentAmount < 0)
            return BadRequest(new ErrorMessageResponse("Not enough token balance"));

        context.Update(token);
        await context.SaveChangesAsync();
        semaphoreSlim.Release();

        var creationDate = DateTime.Now;
        var fileId = file.Name + creationDate.Ticks.ToString();
        var path = $"{userId}/{fileId}/task";
        using (var stream = file.OpenReadStream())
        {
            await fileStorageService.UploadFileAsync(path, stream);
        }

        var eventEntity = new EventEntity { Status = TaskState.Created, Time = creationDate, UserId = userId };
        TaskEntity task = new()
        {
            IsPriority = vip,
            ActualStatus = TaskState.Created,
            UserId = userId,
            TaskFile = path,
            CreationDate = creationDate,
            ResultFile = path,
            Events = new List<EventEntity> { eventEntity }
        };

        var taskEntity = mapper.Map(request, task);
        var result = await context.Tasks.AddAsync(taskEntity);
        await context.Payments.AddAsync(new() { Price = price, Task = taskEntity, Time = DateTime.Now, UserId = userId });

        var stat = await context.Stats.SingleOrDefaultAsync(i => i.IsVip == vip);
        if(stat is not null)
        {
            stat.AllTaskCount++;
            context.Update(stat);
        }

        await context.SaveChangesAsync();

        balancer.Enqueue(vip ? "vip" : "nonvip", new TaskModel
        {
            Id = task.Id,
            State = TaskState.Created,
            Time = creationDate,
            Name = task.Name,
            TaskFilePath = task.TaskFile,
            ResultFilePath = task.ResultFile,
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
        var task = await context.Tasks.SingleOrDefaultAsync(i => i.Id == id);
        if (task is null)
            return NotFound();
        var tmp = mapper.Map(request, task);
        context.Update(tmp);
        await context.SaveChangesAsync();
        return Ok(tmp);
    }

    /// <summary>
    /// Uzavreni tasku
    /// </summary>
    [HttpPost("close")]
    public async Task<IActionResult> CloseTaskAsync(Guid taskId)
    {
        if (!User.TryGetAuth0Id(out var userId))
            return Unauthorized();

        var task = await context.Tasks.SingleOrDefaultAsync(i => i.Id == taskId);
        if (task is null)
            return BadRequest();

        if (task.ActualStatus != TaskState.Downloaded)
            return BadRequest();

        task.ActualStatus = TaskState.Closed;
        context.Update(task);
        await context.Events.AddAsync(new() { Task = task, Status = TaskState.Closed, Time = DateTime.Now, UserId = userId });
        await context.SaveChangesAsync();
        return Ok();
    }

    /// <summary>
    /// Zruseni tasku, vraceni tokenu
    /// </summary>
    [HttpPost("cancel")]
    public async Task<IActionResult> CancelTaskAsync(Guid taskId)
    {
        if (!User.TryGetAuth0Id(out var userId))
            return Unauthorized();

        var balance = await context.TokenBalances.SingleAsync();
        var task = await context.Tasks.SingleOrDefaultAsync(i => i.Id == taskId);
        if (task is null)
            return BadRequest();

        if (task.ActualStatus == TaskState.Created)
        {
            if (!balancer.CancelTask(taskId))
                return new BadRequestObjectResult(new { error = "Try again" });
            task.ActualStatus = TaskState.Cancelled;
            balance.CurrentAmount = task.IsPriority ? balance.CurrentAmount + 1 : balance.CurrentAmount + 2;
            balance.LastAdded = DateTime.Now;
            context.Update(balance);
            context.Update(task);
            var ev = await context.Events.AddAsync(new() { Task = task, Status = TaskState.Cancelled, Time = DateTime.Now, UserId = userId });
            await context.SaveChangesAsync();
            return Ok(mapper.Map<EventResponse>(ev.Entity));
        }
        return BadRequest();
    }

    /// <summary>
    /// Smazani tasku
    /// </summary>
    [HttpDelete]
    public async Task<IActionResult> DeleteTaskAsync(Guid id)
    {
        var task = await context.Tasks.SingleOrDefaultAsync(i => i.Id == id);
        if (task is null)
            return BadRequest();

        context.Remove(task);
        await context.SaveChangesAsync();
        return Ok();
    }

    /// <summary>
    /// Restartuje task pokud je ve stavu timeouted
    /// Task ve stavu crashed se restartuje automaticky
    /// Task ktery byl zrusen, nemuze byt restartovan, musi byt zalozen novy
    /// </summary>
    [HttpPost("restart")]
    public async Task<IActionResult> RestartTaskAsync(Guid taskId)
    {
        if (!User.TryGetAuth0Id(out var userId))
            return Unauthorized();

        var task = await context.Tasks.SingleOrDefaultAsync(i => i.Id == taskId);
        if (task is null)
            return NotFound();

        if (task.ActualStatus == TaskState.Timeouted)
        {
            task.ActualStatus = TaskState.Created;
            context.Update(task);
            var ev = await context.Events.AddAsync(new() { Task = task, Status = TaskState.Created, Time = DateTime.Now, UserId = userId });
            await context.SaveChangesAsync();

            User.TryGetClaimAsBool(TaskLauncherClaimTypes.Vip, out bool vip);
            balancer.Enqueue(vip ? "vip" : "nonvip", new TaskModel
            {
                Id = task.Id,
                State = TaskState.Created,
                Name = task.Name,
                Time = DateTime.Now,
                TaskFilePath = task.TaskFile,
                UserId = task.UserId
            });
            return Ok(mapper.Map<EventResponse>(ev.Entity));
        }
        return BadRequest();
    }
}