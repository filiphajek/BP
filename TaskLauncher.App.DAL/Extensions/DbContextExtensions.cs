using Microsoft.EntityFrameworkCore;
using TaskLauncher.App.DAL.Entities;

namespace TaskLauncher.App.DAL.Extensions;

public static class DbContextExtensions
{
    public static async Task<TEntity?> SingleAsync<TEntity>(this DbContext context, TEntity entity) where TEntity : BaseEntity, IUserKeyProtection
    {
        return await context.Set<TEntity>().SingleOrDefaultAsync(i => i.Id == entity.Id);
    }

    public static async Task<TEntity> AddAndSaveAsync<TEntity>(this DbContext context, TEntity entity) where TEntity : BaseEntity, IUserKeyProtection
    {
        await context.AddAsync(entity);
        await context.SaveChangesAsync();
        return entity;
    }

    public static async Task<TEntity> AddAsync<TEntity>(this DbContext context, TEntity entity) where TEntity : BaseEntity, IUserKeyProtection
    {
        await context.Set<TEntity>().AddAsync(entity);
        return entity;
    }
    //napsat takto extensiony kde budou i parametry kam se muze dat include a queryable
    //pak z toho udelat repozitare uplne stejne
}
