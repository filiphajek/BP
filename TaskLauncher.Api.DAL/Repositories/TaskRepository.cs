using Microsoft.EntityFrameworkCore;
using TaskLauncher.Api.DAL.Entities;
using TaskLauncher.Api.DAL.Repositories.Base;

namespace TaskLauncher.Api.DAL.Repositories;

public interface ITaskRepository : IBaseRepository<TaskEntity>
{
    Task AddFile(TaskEntity task, FileEntity file);
}

public class TaskRepository : AppRepository<TaskEntity>, ITaskRepository
{
    public TaskRepository(AppDbContext context) : base(context)
    {
    }

    public override async Task<TaskEntity?> GetAsync(TaskEntity entity) 
        => await Context.Tasks.Include(i => i.Files).AsNoTracking().SingleOrDefaultAsync(i => i.Id == entity.Id);

    public async Task AddFile(TaskEntity task, FileEntity file)
    {
        Context.Tasks.Attach(task);
        await Context.AddAsync(file);
        await Context.SaveChangesAsync();
    }
}