using Microsoft.EntityFrameworkCore;
using TaskLauncher.App.DAL.Entities;
using TaskLauncher.App.DAL.Repositories.Base;

namespace TaskLauncher.App.DAL.Repositories;

public interface ITaskRepository : IBaseRepository<TaskEntity>
{
}

public class TaskRepository : AppRepository<TaskEntity>, ITaskRepository
{
    public TaskRepository(AppDbContext context) : base(context)
    {
    }

    public override async Task<TaskEntity?> GetAsync(TaskEntity entity)
        => await Context.Tasks.Include(i => i.Events).AsNoTracking().SingleOrDefaultAsync(i => i.Id == entity.Id);
}