using Auth0.ManagementApi;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using TaskLauncher.App.DAL;
using TaskLauncher.App.DAL.Entities;
using TaskLauncher.Authorization.Auth0;
using TaskLauncher.Common;
using TaskLauncher.Common.Enums;
using TaskLauncher.Common.Services;

namespace TaskLauncher.App.Server.Seeders;

/// <summary>
/// Trida ktera inicializuje databazi a vygeneruje testovaci uzivatele s testovacimi daty
/// </summary>
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
    private readonly Auth0Roles roles;
    private readonly IClientFactory<ManagementApiClient> clientFactory;

    private readonly List<string> seededEmails = new() { "tomashavel@test.com", "filipnovak@test.com", "stepannemec@test.com", "jakubstefacek@test.com", "vojtechbrychta@test.com" };
    private readonly Dictionary<UserType, string> seededUsers = new();

    public Seeder(AppDbContext dbContext, IFileStorageService fileStorageService, IOptions<Auth0Roles> options, IClientFactory<ManagementApiClient> clientFactory)
    {
        roles = options.Value;
        this.dbContext = dbContext;
        this.fileStorageService = fileStorageService;
        this.clientFactory = clientFactory;
    }

    /// <summary>
    /// Hlavni funkce, tvori uzivatele, tasky apod.
    /// </summary>
    public async Task SeedAsync()
    {
        //vytvor databazi
        await dbContext.Database.EnsureCreatedAsync();

        //pokud db neni prazdna, neseeduj
        if (await dbContext.Payments.IgnoreQueryFilters().AnyAsync())
            return;

        // vytvoreni uzivatelu
        await EnsureCreatedUsersAsync();

        // seed uzivatelskych uloh, plateb, statistik apod.
        foreach (var user in seededUsers.Where(i => i.Key != UserType.NotRegisteredUser).Where(i => i.Key != UserType.NotVerifiedUser))
        {
            await SeedUser(user.Value, user.Key);
        }
        await dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Vytvari uzivatele, pred tim nez vytvori nove, smaze stare
    /// </summary>
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
            Connection = Constants.Auth0.DefaultConnection,
            FirstName = "Tomas",
            LastName = "Havel",
            Email = "tomashavel@test.com",
            EmailVerified = true,
            NickName = "tom",
            Password = "Password123*",
        });
        seededUsers.Add(UserType.VipUser, vipUser.UserId);
        await auth0client.Users.UpdateAsync(vipUser!.UserId, new()
        {
            AppMetadata = JsonConvert.DeserializeObject("{ 'vip': true, 'registered': true, 'isadmin': false }")
        });

        //normal user
        var normalUser = await auth0client.Users.CreateAsync(new()
        {
            Connection = Constants.Auth0.DefaultConnection,
            FirstName = "Filip",
            LastName = "Novak",
            Email = "filipnovak@test.com",
            EmailVerified = true,
            NickName = "fila",
            Password = "Password123*",
        });
        seededUsers.Add(UserType.NormalUser, normalUser.UserId);
        await auth0client.Users.UpdateAsync(normalUser!.UserId, new()
        {
            AppMetadata = JsonConvert.DeserializeObject("{ 'vip': false, 'registered': true, 'isadmin': false }")
        });

        //user that received vip
        var assignedVipUser = await auth0client.Users.CreateAsync(new()
        {
            Connection = Constants.Auth0.DefaultConnection,
            FirstName = "Stepan",
            LastName = "Nemec",
            Email = "stepannemec@test.com",
            EmailVerified = true,
            NickName = "stepi",
            Password = "Password123*",
        });
        seededUsers.Add(UserType.NormalVipUser, assignedVipUser.UserId);
        await auth0client.Users.UpdateAsync(assignedVipUser!.UserId, new()
        {
            AppMetadata = JsonConvert.DeserializeObject("{ 'vip': true, 'registered': true, 'isadmin': false }")
        });

        //not registered user
        var notRegisteredUser = await auth0client.Users.CreateAsync(new()
        {
            Connection = Constants.Auth0.DefaultConnection,
            Email = "jakubstefacek@test.com",
            EmailVerified = false,
            Password = "Password123*",
        });
        seededUsers.Add(UserType.NotRegisteredUser, notRegisteredUser.UserId);
        await auth0client.Users.UpdateAsync(notRegisteredUser!.UserId, new()
        {
            AppMetadata = JsonConvert.DeserializeObject("{ 'vip': false, 'registered': false, 'isadmin': false }")
        });

        //not verified user
        var notVerifiedUser = await auth0client.Users.CreateAsync(new()
        {
            Connection = Constants.Auth0.DefaultConnection,
            FirstName = "Vojtech",
            LastName = "Brychta",
            Email = "vojtechbrychta@test.com",
            EmailVerified = false,
            NickName = "vojta",
            Password = "Password123*",
        });
        seededUsers.Add(UserType.NotVerifiedUser, notVerifiedUser.UserId);
        await auth0client.Users.UpdateAsync(notVerifiedUser!.UserId, new()
        {
            AppMetadata = JsonConvert.DeserializeObject("{ 'vip': false, 'registered': true, 'isadmin': false }")
        });

        await auth0client.Roles.AssignUsersAsync(roles.User, 
            new() { Users = new[] { vipUser.UserId, normalUser.UserId, assignedVipUser.UserId, notRegisteredUser.UserId, notVerifiedUser.UserId } });
    }

    /// <summary>
    /// Funkce defaultne naseeduje 35 tasku k danemu uzivatelu vcetne plateb, statistik a eventu
    /// </summary>
    private async Task SeedUser(string userId, UserType userType, int taskCount = 35)
    {
        Random random = new(Guid.NewGuid().GetHashCode());

        DateTime time = DateTime.Now.AddDays(-20);
        int priceSum = 0;
        StatEntity normalStatEntity = new() { UserId = userId, IsVip = false };
        StatEntity vipStatEntity = new() { UserId = userId, IsVip = true };

        Console.WriteLine($"Start seeding user (id = '{userId}')");

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

            if (isPriority)
                vipStatEntity.AllTaskCount++;
            else
                normalStatEntity.AllTaskCount++;

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
        await dbContext.TokenBalances.AddAsync(new() { CurrentAmount = 200 - priceSum, LastAdded = DateTime.Now.AddDays(-30.5), UserId = userId });
        await dbContext.Stats.AddAsync(normalStatEntity);
        await dbContext.Stats.AddAsync(vipStatEntity);
    }

    /// <summary>
    /// Pomocna funkce pro nahrani souboru
    /// </summary>
    private async Task UploadFile(string fileName)
    {
        using var stream = new MemoryStream();
        using var writer = new StreamWriter(stream);
        writer.WriteLine("seeded");
        await fileStorageService.UploadFileAsync(fileName, stream);
    }

    /// <summary>
    /// Pomocna funkce pro aktualizaci statistik
    /// </summary>
    private void UpdateStats(StatEntity normalStatEntity, StatEntity vipStatEntity, TaskState actualStatus, bool isPriority)
    {
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
            case TaskState.Downloaded:
            case TaskState.FinishedSuccess:
                if (isPriority)
                {
                    vipStatEntity.SuccessTasks++;
                    vipStatEntity.FinishedTaskCount++;
                }
                else
                {
                    normalStatEntity.SuccessTasks++;
                    normalStatEntity.FinishedTaskCount++;
                }
                break;
            case TaskState.FinishedFailure:
                if (isPriority)
                {
                    vipStatEntity.FailedTasks++;
                    vipStatEntity.FinishedTaskCount++;
                }
                else
                {
                    normalStatEntity.FailedTasks++;
                    normalStatEntity.FinishedTaskCount++;
                }
                break;
        }
    }

    /// <summary>
    /// Pomocna funkce pro ziskani ceny tasku na zaklade typu uzivatele
    /// </summary>
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

    /// <summary>
    /// Nahodne (s ruznyma pravd.) se urci jak task skoncil - s nejvyssi pravdepodobnosti je to uspech pak neuspecj, timeout a crash
    /// </summary>
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
