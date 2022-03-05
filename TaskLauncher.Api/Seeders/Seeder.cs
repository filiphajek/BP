using Microsoft.EntityFrameworkCore;
using TaskLauncher.Api.DAL;
using TaskLauncher.Common.Services;

namespace TaskLauncher.Api.Seeders;

public class Seeder
{
    private readonly AppDbContext dbContext;
    private readonly IFileStorageService fileStorageService;

    public Seeder(AppDbContext dbContext, IFileStorageService fileStorageService)
    {
        this.dbContext = dbContext;
        this.fileStorageService = fileStorageService;
    }

    public async Task SeedAsync()
    {
        await dbContext.Database.EnsureCreatedAsync();
        
        if (await dbContext.Payments.IgnoreQueryFilters().AnyAsync())
            return;

        //user 1
        var task = await dbContext.Tasks.AddAsync(new() { Name = "Task 1", Description = "some text", TaskFile = "xd", UserId = "61b0e161678a0c00689644e0" });
        await dbContext.Payments.AddAsync(new() { Price = 1, Time = DateTime.Now, UserId = "61b0e161678a0c00689644e0", Task = task.Entity });
        await dbContext.TokenBalances.AddAsync(new() { CurrentAmount = 99, LastAdded = DateTime.Now, UserId = "61b0e161678a0c00689644e0" });

        await dbContext.SaveChangesAsync();
    }
}
