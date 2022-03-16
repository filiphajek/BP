using TaskLauncher.App.DAL.Entities;

namespace TaskLauncher.App.DAL.Repositories.Base;

public class AppRepository<TEntity> : EfRepository<TEntity, AppDbContext>, IBaseRepository<TEntity>
    where TEntity : BaseEntity
{
    public AppRepository(AppDbContext context) : base(context)
    {
    }
}
