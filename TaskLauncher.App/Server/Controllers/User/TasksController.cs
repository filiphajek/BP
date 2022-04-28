using Mapster;
using MapsterMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore;
using TaskLauncher.Api.Contracts.Requests;
using TaskLauncher.Api.Contracts.Responses;
using TaskLauncher.Common;
using TaskLauncher.Common.Enums;
using TaskLauncher.Common.Extensions;
using TaskLauncher.Common.Services;
using TaskLauncher.App.DAL.Entities;
using TaskLauncher.App.DAL;
using TaskLauncher.App.Server.Controllers.Base;
using TaskLauncher.App.Server.Tasks;
using TaskLauncher.Common.Models;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.JsonPatch;

namespace TaskLauncher.App.Server.Controllers.User;

/// <summary>
/// Task kontroler, pristupny pouze pro prihlaseneho uzivatele
/// </summary>
public class TasksController : UserODataController<TaskResponse>
{
    private readonly IMapper mapper;
    private readonly IFileStorageService fileStorageService;
    private readonly Balancer balancer;
    private readonly SemaphoreSlim semaphoreSlim = new(1, 1); // semafor pro odecitani ze zustatku

    public TasksController(AppDbContext context, IMapper mapper, IFileStorageService fileStorageService, Balancer balancer) : base(context)
    {
        this.mapper = mapper;
        this.fileStorageService = fileStorageService;
        this.balancer = balancer;
    }

    /// <summary>
    /// Zpřístupňuje dotazovaní přes odata nad celou kolekcí úloh přihlášeného uživatele
    /// </summary>
    [EnableQuery]
    [ProducesResponseType(typeof(List<TaskResponse>), 200)]
    [Produces("application/json")]
    [HttpGet]
    public ActionResult<List<TaskResponse>> Get()
    {
        return Ok(context.Tasks.Where(i => i.ActualStatus != TaskState.Closed).AsQueryable().ProjectToType<TaskResponse>());
    }

    /// <summary>
    /// Vrací detail úlohy společně s platbou a všemi událostmi
    /// </summary>
    /// <param name="id" example="f6195afa-168d-4a30-902e-f4c93af06acd">Id úlohy</param>
    [ProducesResponseType(typeof(TaskDetailResponse), 200)]
    [ProducesResponseType(404)]
    [Produces("application/json")]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TaskDetailResponse>> GetDetail([FromRoute] Guid id)
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
    /// Vytvoření nové úlohy
    /// </summary>
    [ProducesResponseType(typeof(TaskResponse), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(typeof(ErrorMessageResponse), 400)]
    [Produces("application/json")]
    [HttpPost]
    public async Task<ActionResult<TaskResponse>> CreateTaskAsync([FromForm] TaskCreateRequest request, IFormFile file)
    {
        //validace
        if (!User.TryGetAuth0Id(out var userId))
            return Unauthorized();

        if (request.Name.Contains('(') || request.Name.Contains(')'))
            return BadRequest(new ErrorMessageResponse("Name should not contain '(' and ')'"));

        //vytvoreni originalniho jmena
        request.Name = await GetUniqueName(request.Name);

        //urceni ceny tasku podle vip
        double price = 0;
        if (User.TryGetClaimAsBool(Constants.ClaimTypes.Vip, out bool vip) && vip)
        {
            var tmp = (await context.Configs.SingleAsync(i => i.Key == Constants.Configuration.VipTaskPrice)).Value;
            price = double.Parse(tmp);
        }
        else
        {
            var tmp = (await context.Configs.SingleAsync(i => i.Key == Constants.Configuration.NormalTaskPrice)).Value;
            price = double.Parse(tmp);
        }

        //odecteni tokenu z celkove castky uzivatele
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

        //ulozeni souboru
        var creationDate = DateTime.Now;
        var fileId = file.Name + creationDate.Ticks.ToString();
        var path = $"{userId}/{fileId}/task";
        using (var stream = file.OpenReadStream())
        {
            await fileStorageService.UploadFileAsync(path, stream);
        }

        //aktualizace databaze
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

        //zarazeni tasku do fronty
        balancer.Enqueue(vip ? "vip" : "nonvip", new TaskModel
        {
            IsPriority = vip,
            Id = task.Id,
            State = TaskState.Created,
            Time = creationDate,
            Name = task.Name,
            TaskFilePath = task.TaskFile,
            ResultFilePath = task.ResultFile,
            UserId = task.UserId
        });

        return Ok(mapper.Map<TaskResponse>(taskEntity));
    }

    /// <summary>
    /// Aktualizace informací úlohy
    /// </summary>
    /// <param name="id" example="f6195afa-168d-4a30-902e-f4c93af06acd">Id úlohy</param>
    [ProducesResponseType(typeof(TaskResponse), 200)]
    [ProducesResponseType(404)]
    [Produces("application/json")]
    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<TaskResponse>> UpdateTaskAsync([FromRoute] Guid id, [FromBody] JsonPatchDocument<TaskUpdateRequest> patchDoc)
    {
        var task = await context.Tasks.SingleOrDefaultAsync(i => i.Id == id);
        if (task is null)
            return NotFound();

        var request = mapper.Map<TaskUpdateRequest>(task);
        patchDoc.ApplyTo(request);

        var tmp = mapper.Map(request, task);
        context.Update(tmp);
        await context.SaveChangesAsync();
        return Ok(mapper.Map<TaskResponse>(tmp));
    }

    /// <summary>
    /// Příkaz o uzavření úlohy, neposílá se žádná informace v těle dotazu
    /// </summary>
    /// <param name="id" example="f6195afa-168d-4a30-902e-f4c93af06acd">Id úlohy</param>
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    [ProducesResponseType(typeof(ErrorMessageResponse), 400)]
    [ProducesResponseType(typeof(EventResponse), 200)]
    [Produces("application/json")]
    [HttpPost("{id:guid}/close")]
    public async Task<IActionResult> CloseTaskAsync(Guid id)
    {
        if (!User.TryGetAuth0Id(out var userId))
            return Unauthorized();

        var task = await context.Tasks.SingleOrDefaultAsync(i => i.Id == id);
        if (task is null)
            return NotFound();

        if (task.ActualStatus != TaskState.Downloaded)
            return BadRequest(new ErrorMessageResponse("Task is not in Downloaded state"));

        task.ActualStatus = TaskState.Closed;
        context.Update(task);
        var ev = await context.Events.AddAsync(new() { Task = task, Status = TaskState.Closed, Time = DateTime.Now, UserId = userId });
        await context.SaveChangesAsync();
        return Ok(mapper.Map<EventResponse>(ev.Entity));
    }

    /// <summary>
    /// Příkaz o zrušení úlohy a vrácení tokenu, neposílá se žádná informace v těle dotazu
    /// </summary>
    /// <param name="id" example="f6195afa-168d-4a30-902e-f4c93af06acd">Id úlohy</param>
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    [ProducesResponseType(typeof(ErrorMessageResponse), 400)]
    [ProducesResponseType(typeof(EventResponse), 200)]
    [Produces("application/json")]
    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> CancelTaskAsync(Guid id)
    {
        if (!User.TryGetAuth0Id(out var userId))
            return Unauthorized();

        var balance = await context.TokenBalances.SingleAsync();
        var task = await context.Tasks.SingleOrDefaultAsync(i => i.Id == id);
        if (task is null)
            return NotFound();

        if (task.ActualStatus == TaskState.Created)
        {
            if (!balancer.CancelTask(id))
                return new BadRequestObjectResult(new { error = "Try again" });
            task.ActualStatus = TaskState.Cancelled;
            balance.CurrentAmount = task.IsPriority ? balance.CurrentAmount + 2 : balance.CurrentAmount + 1;
            balance.LastAdded = DateTime.Now;
            context.Update(balance);
            context.Update(task);
            var ev = await context.Events.AddAsync(new() { Task = task, Status = TaskState.Cancelled, Time = DateTime.Now, UserId = userId });
            await context.SaveChangesAsync();
            return Ok(mapper.Map<EventResponse>(ev.Entity));
        }
        return BadRequest(new ErrorMessageResponse("Task is not in Create State"));
    }

    /// <summary>
    /// Smazání úlohy, neposílá se žádná informace v těle dotazu
    /// </summary>
    /// <param name="id" example="f6195afa-168d-4a30-902e-f4c93af06acd">Id úlohy</param>
    [ProducesResponseType(404)]
    [ProducesResponseType(200)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteTaskAsync(Guid id)
    {
        var task = await context.Tasks.SingleOrDefaultAsync(i => i.Id == id);
        if (task is null)
            return NotFound();

        context.Remove(task);
        await context.SaveChangesAsync();
        return Ok();
    }

    /// <summary>
    /// Restartuje úlohu, pokud je ve stavu Nedokončeno, úloha ve stavu Zhavarováno se restartuje automaticky, zrušená úloha nemůže být restartována
    /// </summary>
    /// <param name="id" example="f6195afa-168d-4a30-902e-f4c93af06acd">Id úlohy</param>
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    [ProducesResponseType(typeof(EventResponse), 200)]
    [ProducesResponseType(typeof(ErrorMessageResponse), 400)]
    [Produces("application/json")]
    [HttpPost("{id:guid}/restart")]
    public async Task<IActionResult> RestartTaskAsync(Guid id)
    {
        if (!User.TryGetAuth0Id(out var userId))
            return Unauthorized();

        var task = await context.Tasks.SingleOrDefaultAsync(i => i.Id == id);
        if (task is null)
            return NotFound();

        if (task.ActualStatus == TaskState.Timeouted)
        {
            task.ActualStatus = TaskState.Created;
            context.Update(task);
            var ev = await context.Events.AddAsync(new() { Task = task, Status = TaskState.Created, Time = DateTime.Now, UserId = userId });
            await context.SaveChangesAsync();

            User.TryGetClaimAsBool(Constants.ClaimTypes.Vip, out bool vip);
            balancer.Enqueue(vip ? "vip" : "nonvip", new TaskModel
            {
                IsPriority = vip,
                Id = task.Id,
                State = TaskState.Created,
                Time = task.CreationDate,
                Name = task.Name,
                TaskFilePath = task.TaskFile,
                ResultFilePath = task.ResultFile,
                UserId = task.UserId
            });
            return Ok(mapper.Map<EventResponse>(ev.Entity));
        }
        return BadRequest(new ErrorMessageResponse("Task is not in Timeouted state"));
    }
}