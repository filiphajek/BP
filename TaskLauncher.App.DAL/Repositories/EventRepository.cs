using Microsoft.EntityFrameworkCore;
using TaskLauncher.App.DAL.Entities;
using TaskLauncher.App.DAL.Repositories.Base;

namespace TaskLauncher.App.DAL.Repositories;

public interface IEventRepository : IBaseRepository<EventEntity>
{
}

public class EventRepository : AppRepository<EventEntity>, IEventRepository
{
    public EventRepository(AppDbContext context) : base(context)
    {
    }

    public override async Task<EventEntity?> GetAsync(EventEntity entity)
        => await Context.Events.Include(i => i.Task).AsNoTracking().SingleOrDefaultAsync(i => i.Id == entity.Id);
}
