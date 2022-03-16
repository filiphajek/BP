using System.Linq.Expressions;
using TaskLauncher.App.DAL.Entities;

namespace TaskLauncher.App.DAL.Repositories.Base;

public interface IBaseRepository<TEntity> : IRepository
    where TEntity : BaseEntity
{
    Task<TEntity?> GetAsync(TEntity entity);
    Task<IEnumerable<TEntity>> GetAllAsync();
    Task<IEnumerable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate);
    Task<TEntity> AddAsync(TEntity entity);
    Task AddRangeAsync(IEnumerable<TEntity> entities);
    Task RemoveAsync(TEntity entity);
    Task RemoveRangeAsync(IEnumerable<TEntity> entities);
    Task UpdateAsync(TEntity entity);
    void ClearTrackedEntries();
}
