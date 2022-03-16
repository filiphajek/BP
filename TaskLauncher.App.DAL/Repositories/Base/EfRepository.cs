using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using TaskLauncher.App.DAL.Entities;

namespace TaskLauncher.App.DAL.Repositories.Base;

/// <summary>
/// Bazova trida repozitare pro pristup do DbContextu, implementace zakladnich metod
/// </summary>
public class EfRepository<TEntity, TDbContext> : IBaseRepository<TEntity>
    where TEntity : BaseEntity
    where TDbContext : DbContext
{
    protected TDbContext Context { get; }

    public EfRepository(TDbContext context) => Context = context;

    public virtual async Task<TEntity?> GetAsync(TEntity entity)
        => await Context.Set<TEntity>().AsNoTracking().SingleOrDefaultAsync(i => i.Id == entity.Id);

    public virtual async Task<IEnumerable<TEntity>> GetAllAsync()
        => await Context.Set<TEntity>().AsNoTracking().ToListAsync();

    public virtual async Task<IEnumerable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate)
        => await Context.Set<TEntity>().AsNoTracking().Where(predicate).AsNoTracking().ToListAsync();

    public virtual async Task<TEntity> AddAsync(TEntity entity)
    {
        await Context.AddAsync(entity);
        await Context.SaveChangesAsync();
        return entity;
    }

    public virtual async Task AddRangeAsync(IEnumerable<TEntity> entities)
    {
        await Context.Set<TEntity>().AddRangeAsync(entities);
        await Context.SaveChangesAsync();
    }

    public virtual async Task RemoveAsync(TEntity entity)
    {
        var item = await GetAsync(entity);
        if (item is null)
            return;
        Context.Set<TEntity>().Remove(item);
        await Context.SaveChangesAsync();
        ClearTrackedEntries();
    }

    public virtual async Task RemoveRangeAsync(IEnumerable<TEntity> entities)
    {
        Context.Set<TEntity>().RemoveRange(entities);
        await Context.SaveChangesAsync();
    }

    public virtual async Task UpdateAsync(TEntity entity)
    {
        Context.Set<TEntity>().Update(entity);
        await Context.SaveChangesAsync();
    }

    public void ClearTrackedEntries()
    {
        foreach (var entry in Context.ChangeTracker.Entries())
        {
            entry.State = EntityState.Detached;
        }
    }
}