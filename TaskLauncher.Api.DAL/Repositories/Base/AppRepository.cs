using TaskLauncher.Api.DAL.Entities;

namespace TaskLauncher.Api.DAL.Repositories.Base;

public class AppRepository<TEntity> : EfRepository<TEntity, AppDbContext>, IBaseRepository<TEntity>
    where TEntity : BaseEntity
{
    public AppRepository(AppDbContext context) : base(context)
    {
    }
}
