using Microsoft.EntityFrameworkCore;
using TaskLauncher.Api.DAL;

namespace TaskLauncher.Api.Seeders;

public class Seeder
{
    private readonly AppDbContext dbContext;

    public Seeder(AppDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task SeedAsync()
    {
        await dbContext.Database.EnsureCreatedAsync();
        
        if (await dbContext.Payments.IgnoreQueryFilters().AnyAsync())
            return;

        var task = await dbContext.Tasks.IgnoreQueryFilters().SingleAsync();
        await dbContext.Payments.AddAsync(new() { Price = 1, Time = DateTime.Now, UserId = "61b0e161678a0c00689644e0", Task = task });
        await dbContext.TokenBalances.AddAsync(new() { CurrentAmount = 99, LastAdded = DateTime.Now, UserId = "61b0e161678a0c00689644e0" });

        await dbContext.SaveChangesAsync();
    }
}
