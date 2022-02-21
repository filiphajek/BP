using Microsoft.EntityFrameworkCore;
using TaskLauncher.Api.DAL.Entities;
using TaskLauncher.Api.DAL.Repositories.Base;

namespace TaskLauncher.Api.DAL.Repositories;

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
