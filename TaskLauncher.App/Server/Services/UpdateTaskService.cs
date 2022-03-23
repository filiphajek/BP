using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using TaskLauncher.App.DAL;
using TaskLauncher.App.DAL.Entities;
using TaskLauncher.Common.Models;

namespace TaskLauncher.App.Server.Services;

public interface IUpdateTaskService
{
    Task<EventModel> UpdateTaskAsync(TaskModel model);
    Task EndTaskAsync(TaskModel model);
}

public class UpdateTaskService : IUpdateTaskService
{
    private readonly AppDbContext dbContext;
    private readonly IMapper mapper;

    public UpdateTaskService(AppDbContext dbContext, IMapper mapper)
    {
        this.dbContext = dbContext;
        this.mapper = mapper;
    }

    public async Task<EventModel> UpdateTaskAsync(TaskModel model)
    {
        var task = await dbContext.Tasks.IgnoreQueryFilters().SingleAsync(i => i.Id == model.Id);
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
        var stat1 = await dbContext.Stats.ToListAsync();
        var stat2 = await dbContext.Stats.IgnoreQueryFilters().ToListAsync();

        var stat = await dbContext.Stats.IgnoreQueryFilters().SingleAsync(i => i.UserId == model.UserId && i.IsVip == model.IsPriority);
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
}
