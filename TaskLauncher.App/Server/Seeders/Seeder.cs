using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using TaskLauncher.App.DAL;
using TaskLauncher.App.DAL.Entities;
using TaskLauncher.Authorization.Auth0;
using TaskLauncher.Common.Enums;
using TaskLauncher.Common.Services;

namespace TaskLauncher.App.Server.Seeders;

public class Seeder
{
    enum UserType
    {
        VipUser,
        NormalUser,
        NormalVipUser,
        NotRegisteredUser,
        NotVerifiedUser
    }

    private readonly AppDbContext dbContext;
    private readonly IFileStorageService fileStorageService;
    private readonly ManagementApiClientFactory clientFactory;

    private readonly List<string> seededEmails = new() { "tomashavel@test.com", "filipnovak@test.com", "stepannemec@test.com", "jakubstefacek@test.com", "vojtechbrychta@test.com" };
    private readonly Dictionary<UserType, string> seededUsers = new()
    {
        { UserType.VipUser, "auth0|6238ef1ee644f4006ff21121" },
        { UserType.NormalUser, "auth0|6238ef1e9e63f500683a8116" },
        { UserType.NormalVipUser, "auth0|6238ef1e647a36006ba40f9e" },
        { UserType.NotRegisteredUser, "auth0|6238ef1f647a36006ba40f9f" },
        { UserType.NotVerifiedUser, "auth0|6238ef1f647a36006ba40fa0" },

    };

    public Seeder(AppDbContext dbContext, IFileStorageService fileStorageService, ManagementApiClientFactory clientFactory)
    {
        this.dbContext = dbContext;
        this.fileStorageService = fileStorageService;
        this.clientFactory = clientFactory;
    }

    public async Task SeedAsync()
    {
        //vytvor databazi
        await dbContext.Database.EnsureCreatedAsync();

        //pokud neni prazdna, neseeduj
        if (await dbContext.Payments.IgnoreQueryFilters().AnyAsync())
            return;

        // vytvoreni uzivatelu
        //await EnsureCreatedUsersAsync();

        // seed uzivatelskych uloh, plateb, statistik apod.
        foreach (var user in seededUsers.Where(i => i.Key != UserType.NotRegisteredUser).Where(i => i.Key != UserType.NotVerifiedUser))
        {
            await SeedUser(user.Value, user.Key);
        }
        await dbContext.SaveChangesAsync();
    }

    private async Task EnsureCreatedUsersAsync()
    {
        var auth0client = await clientFactory.GetClient();

        foreach (var user in (await auth0client.Users.GetAllAsync(new())).Where(i => seededEmails.Contains(i.Email)))
        {
            await auth0client.Users.DeleteAsync(user.UserId);
        }

        //vip user
        var vipUser = await auth0client.Users.CreateAsync(new()
        {
            Connection = "Username-Password-Authentication",
            FirstName = "Tomas",
            LastName = "Havel",
            Email = "tomashavel@test.com",
            EmailVerified = true,
            NickName = "tom",
            Password = "Password123*",
            AppMetadata = JsonConvert.DeserializeObject("{ 'vip': true, 'registered': true, 'isadmin': false }")
        });
        seededUsers.Add(UserType.VipUser, vipUser.UserId);

        //normal user
        var normalUser = await auth0client.Users.CreateAsync(new()
        {
            Connection = "Username-Password-Authentication",
            FirstName = "Filip",
            LastName = "Novak",
            Email = "filipnovak@test.com",
            EmailVerified = true,
            NickName = "fila",
            Password = "Password123*",
            AppMetadata = JsonConvert.DeserializeObject("{ 'vip': false, 'registered': true, 'isadmin': false }")
        });
        seededUsers.Add(UserType.NormalUser, normalUser.UserId);

        //user that received vip
        var assignedVipUser = await auth0client.Users.CreateAsync(new()
        {
            Connection = "Username-Password-Authentication",
            FirstName = "Stepan",
            LastName = "Nemec",
            Email = "stepannemec@test.com",
            EmailVerified = true,
            NickName = "stepi",
            Password = "Password123*",
            AppMetadata = JsonConvert.DeserializeObject("{ 'vip': true, 'registered': true, 'isadmin': false }")
        });
        seededUsers.Add(UserType.NormalVipUser, assignedVipUser.UserId);

        //not registered user
        var notRegisteredUser = await auth0client.Users.CreateAsync(new()
        {
            Connection = "Username-Password-Authentication",
            Email = "jakubstefacek@test.com",
            EmailVerified = false,
            Password = "Password123*",
            AppMetadata = JsonConvert.DeserializeObject("{ 'vip': false, 'registered': false, 'isadmin': false }")
        });
        seededUsers.Add(UserType.NotRegisteredUser, notRegisteredUser.UserId);

        //not verified user
        var notVerifiedUser = await auth0client.Users.CreateAsync(new()
        {
            Connection = "Username-Password-Authentication",
            FirstName = "Vojtech",
            LastName = "Brychta",
            Email = "vojtechbrychta@test.com",
            EmailVerified = false,
            NickName = "vojta",
            Password = "Password123*",
            AppMetadata = JsonConvert.DeserializeObject("{ 'vip': false, 'registered': true, 'isadmin': false }")
        });
        seededUsers.Add(UserType.NotVerifiedUser, notVerifiedUser.UserId);

        await auth0client.Roles.AssignUsersAsync("rol_6Vh7zpX3Z61sN307", 
            new() { Users = new[] { vipUser.UserId, normalUser.UserId, assignedVipUser.UserId, notRegisteredUser.UserId, notVerifiedUser.UserId } });
    }

    private async Task UploadFile(string fileName)
    {
        using var stream = new MemoryStream();
        using var writer = new StreamWriter(stream);
        writer.WriteLine("seeded");
        await fileStorageService.UploadFileAsync(fileName, stream);
    }

    private async Task SeedUser(string userId, UserType userType, int taskCount = 35)
    {
        Random random = new(Guid.NewGuid().GetHashCode());

        DateTime time = DateTime.Now.AddDays(-20);
        int priceSum = 0;
        StatEntity normalStatEntity = new() { UserId = userId, AllTaskCount = taskCount + 5, IsVip = false }; // +5 smazanych napr.
        StatEntity vipStatEntity = new() { UserId = userId, AllTaskCount = taskCount + 5, IsVip = true };

        Console.WriteLine(userId);

        for (int i = 0; i < taskCount; i++)
        {
            if(i % random.Next(2, 4) == 0)
            {
                time = time.AddDays(1);
            }
            else
            {
                time = time.AddHours(1);
            }
            var exitQueueTime = random.Next(2, random.Next(10, 12));
            var runningTime = random.Next(3, random.Next(7, 10));

            //files
            var taskFileName = $"{userId}/taskfile{i}";
            var resultFileName = $"{userId}/resultfile{i}";
            await UploadFile(taskFileName);
            await UploadFile(resultFileName);

            //get task price, priority
            int price = GetTaskPrice(taskCount, i, userType);
            bool isPriority = price != 1;
            priceSum += price;

            //task
            var task = await dbContext.Tasks.AddAsync(new() { CreationDate = time, Name = $"Task {i}", Description = "text", IsPriority = isPriority, TaskFile = taskFileName, UserId = userId, ResultFile = resultFileName });
            
            //payment
            await dbContext.Payments.AddAsync(new() { Price = price, Time = time, UserId = userId, Task = task.Entity });
            
            //events
            await dbContext.Events.AddAsync(new() { Task = task.Entity, Status = TaskState.Created, UserId = userId, Time = time });
            await dbContext.Events.AddAsync(new() { Task = task.Entity, Status = TaskState.Ready, UserId = userId, Time = time.AddMinutes(exitQueueTime) });
            await dbContext.Events.AddAsync(new() { Task = task.Entity, Status = TaskState.Running, UserId = userId, Time = time.AddMinutes(exitQueueTime + 0.1) });
            await AddTaskFinishedEvent(task.Entity, userId, exitQueueTime, runningTime, time);
            UpdateStats(normalStatEntity, vipStatEntity, task.Entity.ActualStatus, isPriority);
        }
        await dbContext.TokenBalances.AddAsync(new() { CurrentAmount = priceSum, LastAdded = DateTime.Now.AddDays(-30.5), UserId = userId });
        await dbContext.Stats.AddAsync(normalStatEntity);
        await dbContext.Stats.AddAsync(vipStatEntity);
        //ZBIRAT STATISTIKU !! JAK CASY TASK DOKONCENI -> PAK TAM PRIDAT NEJAKOU PEVNOU HODNOTU
    }

    private void UpdateStats(StatEntity normalStatEntity, StatEntity vipStatEntity, TaskState actualStatus, bool isPriority)
    {
        if (isPriority)
            vipStatEntity.FinishedTaskCount++;
        else
            normalStatEntity.FinishedTaskCount++;

        switch (actualStatus)
        {
            case TaskState.Crashed:
                if(isPriority)
                    vipStatEntity.CrashedTasks++;
                else
                    normalStatEntity.CrashedTasks++;
                break;
            case TaskState.Timeouted:
                if (isPriority)
                    vipStatEntity.TimeoutedTasks++;
                else
                    normalStatEntity.TimeoutedTasks++;
                break;
            case TaskState.FinishedSuccess:
                if (isPriority)
                    vipStatEntity.SuccessTasks++;
                else
                    normalStatEntity.SuccessTasks++;
                break;
            case TaskState.FinishedFailure:
                if (isPriority)
                    vipStatEntity.FailedTasks++;
                else
                    normalStatEntity.FailedTasks++;
                break;
        }
    }

    private static int GetTaskPrice(int taskCount, int index, UserType userType)
    {
        if (userType == UserType.NormalUser)
            return 1;

        if (userType == UserType.VipUser)
            return 2;

        if (taskCount / 2 > index)
            return 1;
        return 2;
    }

    private async Task AddTaskFinishedEvent(TaskEntity task, string userId, int exitQueueTime, int runningTime, DateTime time)
    {
        Random random = new(Guid.NewGuid().GetHashCode());
        var rand = random.NextDouble();
        TaskState finalState = TaskState.FinishedSuccess;
        if (rand < 0.8)
        {
            await dbContext.Events.AddAsync(new() { Task = task, Status = finalState, UserId = userId, Time = time.AddMinutes(exitQueueTime + runningTime + 0.1) });
            if(random.NextDouble() < 0.1)
            {
                finalState = TaskState.Downloaded;
                await dbContext.Events.AddAsync(new() { Task = task, Status = finalState, UserId = userId, Time = time.AddMinutes(exitQueueTime + runningTime + 15) });
            }
        }
        else
        {
            var randError = random.NextDouble();
            if (randError < 0.7)
            {
                finalState = TaskState.FinishedFailure;
                await dbContext.Events.AddAsync(new() { Task = task, Status = finalState, UserId = userId, Time = time.AddMinutes(exitQueueTime + runningTime + 0.1) });
            }
            else if (randError < 0.9)
            {
                finalState = TaskState.Timeouted;
                await dbContext.Events.AddAsync(new() { Task = task, Status = finalState, UserId = userId, Time = time.AddMinutes(exitQueueTime + runningTime + 0.1) });
            }
            else
            {
                await dbContext.Events.AddAsync(new() { Task = task, Status = TaskState.Crashed, UserId = userId, Time = time.AddMinutes(exitQueueTime + runningTime + 0.1) });
                await dbContext.Events.AddAsync(new() { Task = task, Status = TaskState.Created, UserId = userId, Time = time.AddMinutes(exitQueueTime + runningTime + 10 ) });
                await dbContext.Events.AddAsync(new() { Task = task, Status = TaskState.Ready, UserId = userId, Time = time.AddMinutes(exitQueueTime + runningTime + 12) });
                await dbContext.Events.AddAsync(new() { Task = task, Status = TaskState.Running, UserId = userId, Time = time.AddMinutes(exitQueueTime + runningTime + 14) });
                await dbContext.Events.AddAsync(new() { Task = task, Status = TaskState.FinishedSuccess, UserId = userId, Time = time.AddMinutes(exitQueueTime + runningTime + 14.2) });
            }
        }
        task.ActualStatus = finalState;
    }
}
