using Microsoft.EntityFrameworkCore;
using TaskLauncher.App.DAL;
using TaskLauncher.Common.Enums;
using TaskLauncher.Common.Services;

namespace TaskLauncher.App.Server.Seeders;

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

        //todo management api
        //testuser auth0|61b0e161678a0c00689644e0
        //testadmin auth0|622076411a44b70076f27000
        foreach (var id in new[] { "auth0|61b0e161678a0c00689644e0", "auth0|622076411a44b70076f27000" })
        {
            await SeedUser(id);
            await SeedStats(id);
        }
        await dbContext.SaveChangesAsync();
    }

    private async Task UploadFile(string fileName)
    {
        using var stream = new MemoryStream();
        using var writer = new StreamWriter(stream);
        writer.WriteLine("seeded");
        await fileStorageService.UploadFileAsync(fileName, stream);
    }

    private async Task SeedStats(string id)
    {
        await dbContext.Stats.AddAsync(new()
        {
            UserId = id,
            AllTaskCount = 20,
            FinishedTaskCount = 16,
            FailedTasks = 2,
            CrashedTasks = 1,
            TimeoutedTasks = 1,
            SuccessTasks = 12,
            IsVip = false
        });
        await dbContext.Stats.AddAsync(new()
        {
            UserId = id,
            IsVip = true
        });
    }

    private async Task SeedUser(string userId)
    {
        int i = 0;

        //soubory
        var taskFileName = $"{userId}/taskfile{i}";
        var resultFileName = $"{userId}/resultfile{i}";
        await UploadFile(taskFileName);
        await UploadFile(resultFileName);
        
        //task1
        var task1 = await dbContext.Tasks.AddAsync(new() { CreationDate = DateTime.Now, Name = $"Task {i++}", Description = "text", TaskFile = taskFileName, UserId = userId, ResultFile = resultFileName, ActualStatus = TaskState.FinishedSuccess });
        await dbContext.Events.AddAsync(new() { Task = task1.Entity, Status = TaskState.Created, UserId = userId, Time = DateTime.Now });
        await dbContext.Events.AddAsync(new() { Task = task1.Entity, Status = TaskState.Ready, UserId = userId, Time = DateTime.Now.AddMinutes(15) });
        await dbContext.Events.AddAsync(new() { Task = task1.Entity, Status = TaskState.Running, UserId = userId, Time = DateTime.Now.AddMinutes(16) });
        await dbContext.Events.AddAsync(new() { Task = task1.Entity, Status = TaskState.FinishedSuccess, UserId = userId, Time = DateTime.Now.AddMinutes(20) });
        
        await dbContext.Payments.AddAsync(new() { Price = 1, Time = DateTime.Now, UserId = userId, Task = task1.Entity });

        //task2
        var task2 = await dbContext.Tasks.AddAsync(new() { CreationDate = DateTime.Now, Name = $"Task {i++}", ActualStatus = TaskState.FinishedSuccess, Description = "text", TaskFile = taskFileName, UserId = userId, ResultFile = resultFileName });
        await dbContext.Events.AddAsync(new() { Task = task2.Entity, Status = TaskState.Created, UserId = userId, Time = DateTime.Now });
        await dbContext.Events.AddAsync(new() { Task = task2.Entity, Status = TaskState.Ready, UserId = userId, Time = DateTime.Now.AddMinutes(13) });
        await dbContext.Events.AddAsync(new() { Task = task2.Entity, Status = TaskState.Running, UserId = userId, Time = DateTime.Now.AddMinutes(14) });
        await dbContext.Events.AddAsync(new() { Task = task2.Entity, Status = TaskState.FinishedSuccess, UserId = userId, Time = DateTime.Now.AddMinutes(21) });
        
        await dbContext.Payments.AddAsync(new() { Price = 1, Time = DateTime.Now, UserId = userId, Task = task2.Entity });

        await dbContext.TokenBalances.AddAsync(new() { CurrentAmount = 98, LastAdded = DateTime.Now, UserId = userId });
    }
}
