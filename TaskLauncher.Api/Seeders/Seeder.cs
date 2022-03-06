using Microsoft.EntityFrameworkCore;
using TaskLauncher.Api.DAL;
using TaskLauncher.Common.Enums;
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

        //user1 6225224ff0bca300691d9bd8
        //seededvip 6225226df7aa7a006a913c3b
        //testuser 61b0e161678a0c00689644e0
        //testadmin 622076411a44b70076f27000
        foreach (var id in new[] { "6225224ff0bca300691d9bd8", "6225226df7aa7a006a913c3b", "61b0e161678a0c00689644e0", "622076411a44b70076f27000" })
        {
            await SeedUser(id);
        }

        await dbContext.SaveChangesAsync();
    }

    private async Task UploadFile(string userId, string fileName)
    {
        using var stream = new MemoryStream();
        using var writer = new StreamWriter(stream);
        writer.WriteLine("seeded");
        await fileStorageService.UploadFileAsync($"{userId}/{fileName}", stream);
    }

    private async Task SeedUser(string userId)
    {
        int i = 0;

        var fileName = $"seed{DateTime.Now.Ticks}";
        await UploadFile(userId, fileName);

        //task1
        var task1 = await dbContext.Tasks.AddAsync(new() { Name = $"Task {i++}", Description = "text", TaskFile = fileName, UserId = userId, ResultFile = "", ActualStatus = TaskState.InQueue });
        await dbContext.Payments.AddAsync(new() { Price = 1, Time = DateTime.Now, UserId = userId, Task = task1.Entity });
        await dbContext.Events.AddAsync(new() { Task = task1.Entity, Status = TaskState.Created, UserId = userId, Time = DateTime.Now });
        await dbContext.Events.AddAsync(new() { Task = task1.Entity, Status = TaskState.InQueue, UserId = userId, Time = DateTime.Now.AddMinutes(10) });

        //task2
        var task2 = await dbContext.Tasks.AddAsync(new() { Name = $"Task {i++}", Description = "text", TaskFile = fileName, UserId = userId, ResultFile = "" });
        await dbContext.Events.AddAsync(new() { Task = task2.Entity, Status = TaskState.Created, UserId = userId, Time = DateTime.Now });
        await dbContext.Payments.AddAsync(new() { Price = 1, Time = DateTime.Now, UserId = userId, Task = task2.Entity });

        await dbContext.TokenBalances.AddAsync(new() { CurrentAmount = 98, LastAdded = DateTime.Now, UserId = userId });
    }
}
