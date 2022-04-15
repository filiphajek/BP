using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using TaskLauncher.App.DAL;
using TaskLauncher.App.DAL.Entities;
using TaskLauncher.Common.Models;

namespace TaskLauncher.App.Server.Services;

/// <summary>
/// Interface definujici nektere operace spojene s taskem, ktere se pouzivaji na vice mistech aplikace
/// </summary>
public interface ITaskService
{
    /// <summary>
    /// Aktualizace tasku, vraci novou udalost, ktera informuje o zmene stavu tasku
    /// </summary>
    Task<EventModel?> UpdateTaskAsync(TaskModel model);

    /// <summary>
    /// Ukoncuje dany task, aktualizace statistiky
    /// </summary>
    Task EndTaskAsync(TaskModel model);

    /// <summary>
    /// Vraci true pokud task existuje v databazi, jinak false
    /// </summary>
    Task<bool> TaskExists(TaskModel model);
}

/// <summary>
/// Pomocna trida implementujici ITaskService pro aktualizaci informaci o tasku
/// </summary>
public class TaskService : ITaskService
{
    private readonly AppDbContext dbContext;
    private readonly IMapper mapper;

    public TaskService(AppDbContext dbContext, IMapper mapper)
    {
        this.dbContext = dbContext;
        this.mapper = mapper;
    }

    public async Task<EventModel?> UpdateTaskAsync(TaskModel model)
    {
        var task = await dbContext.Tasks.IgnoreQueryFilters().SingleOrDefaultAsync(i => i.Id == model.Id);
        if(task is null)
            return null;

        task.ActualStatus = model.State;
        dbContext.Update(task);
        var ev = new EventEntity() { Status = model.State, Task = task, Time = DateTime.Now, UserId = model.UserId };
        await dbContext.Events.AddAsync(ev);
        await dbContext.SaveChangesAsync();

        var tmp = mapper.Map<EventModel>(ev);
        tmp.TaskId = model.Id;
        return tmp;
    }

    public async Task EndTaskAsync(TaskModel model)
    {
        var stat = await dbContext.Stats.IgnoreQueryFilters().SingleOrDefaultAsync(i => i.UserId == model.UserId && i.IsVip == model.IsPriority);
        if (stat is null)
            return;

        switch (model.State)
        {
            case Common.Enums.TaskState.Crashed:
                stat.CrashedTasks++;
                break;
            case Common.Enums.TaskState.Timeouted:
                stat.TimeoutedTasks++;
                break;
            case Common.Enums.TaskState.FinishedSuccess:
                stat.SuccessTasks++;
                stat.FinishedTaskCount++;
                break;
            case Common.Enums.TaskState.FinishedFailure:
                stat.FailedTasks++;
                stat.FinishedTaskCount++;
                break;
        }
        dbContext.Update(stat);
        await dbContext.SaveChangesAsync();
    }

    public async Task<bool> TaskExists(TaskModel model)
    {
        var task = await dbContext.Tasks.IgnoreQueryFilters().SingleOrDefaultAsync(i => i.Id == model.Id);
        return task is not null;
    }
}
