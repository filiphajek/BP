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
    private readonly IServiceProvider provider;
    private readonly IMapper mapper;

    public UpdateTaskService(IServiceProvider provider, IMapper mapper)
    {
        this.provider = provider;
        this.mapper = mapper;
    }

    public async Task<EventModel> UpdateTaskAsync(TaskModel model)
    {
        using var scope = provider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

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
        using var scope = provider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var stat = await dbContext.Stats.SingleAsync(i => i.IsVip == model.IsPriority); // bude tady uplatnen filter?
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
