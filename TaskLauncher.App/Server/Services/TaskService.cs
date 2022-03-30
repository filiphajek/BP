using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using TaskLauncher.App.DAL;
using TaskLauncher.App.DAL.Entities;
using TaskLauncher.Common.Models;

namespace TaskLauncher.App.Server.Services;

public interface ITaskService
{
    Task<EventModel?> UpdateTaskAsync(TaskModel model);
    Task EndTaskAsync(TaskModel model);
    Task<bool> TaskExists(TaskModel model);
}

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
