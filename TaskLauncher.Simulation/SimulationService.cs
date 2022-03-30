using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using TaskLauncher.Api.Contracts.Requests;
using TaskLauncher.Api.Contracts.Responses;
using TaskLauncher.Common.Extensions;
using TaskLauncher.Common.Models;

namespace TaskLauncher.Simulation;

/// <summary>
/// Sluzbu implementujici simulaci uzivatelu pristupujici ke sluzbe
/// </summary>
public class SimulationService : BackgroundService
{
    private readonly UserFactory userFactory;
    private readonly ILogger<SimulationService> logger;
    private readonly IHttpClientFactory httpClientFactory;
    private readonly HttpClient httpClient;
    private readonly SimulationConfig simulationOptions;
    private readonly Random random = new(Guid.NewGuid().GetHashCode());

    private readonly List<HttpClient> userHttpClients = new();

    public SimulationService(UserFactory userFactory, 
        ILogger<SimulationService> logger, 
        IHttpClientFactory httpClientFactory,
        HttpClient httpClient,
        IOptions<SimulationConfig> simulationOptions)
    {
        this.userFactory = userFactory;
        this.logger = logger;
        this.httpClientFactory = httpClientFactory;
        this.httpClient = httpClient;
        this.simulationOptions = simulationOptions.Value;
    }

    /// <summary>
    /// Cekani na server, inicializace
    /// </summary>
    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            try
            {
                var response = await httpClient.GetAsync("health", cancellationToken);
                if (response is not null && response.IsSuccessStatusCode)
                    break;
            }
            catch { }
            logger.LogInformation("Waiting for server");
            await Task.Delay(5000, cancellationToken);
        }
        await userFactory.Initialize();
        await base.StartAsync(cancellationToken);
    }

    /// <summary>
    /// Hlavni funkce simulace
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var tasks = new List<Task>();
            var users = await CreateUsers();

            Console.WriteLine($"{users.Count} was created");
            foreach (var user in users)
            {
                var isvip = user.Vip ? "vip" : "normal";
                Console.WriteLine($"{isvip}: {user.Email}");
            }

            foreach (var user in users)
            {
                var tmp = SimulateUser(user, simulationOptions.TaskCount, stoppingToken);
                tasks.Add(tmp);
            }

            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            logger.LogError(ex.ToString());
        }
    }

    /// <summary>
    /// Smazani uzivatelu
    /// </summary>
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Deleting users");
        foreach (var client in userHttpClients)
        {
            await client.DeleteAsync("api/user", cancellationToken);
            client.Dispose();
        }
        
        await base.StopAsync(cancellationToken);
    }

    /// <summary>
    /// Vytvoreni uzivatelu
    /// </summary>
    private async Task<List<UserModel>> CreateUsers()
    {
        var tmp = new List<UserModel>();
        for (int i = 0; i < simulationOptions.NormalUsers; i++)
        {
            var user = await userFactory.CreateUser(false);
            tmp.Add(user);
        }
        for (int i = 0; i < simulationOptions.VipUsers; i++)
        {
            var user = await userFactory.CreateUser(true);
            tmp.Add(user);
        }
        return tmp;
    }

    /// <summary>
    /// Simulace uzivatela
    /// Po prihlaseni postupne vytvari nekolik tasku
    /// </summary>
    private async Task SimulateUser(UserModel user, int taskCount, CancellationToken cancellationToken)
    {
        HttpClient client = httpClientFactory.CreateClient("default");

        //prihlaseni
        var auth = await client.PostAsJsonAsync("loginbypasswordflow", new CookieLessLoginRequest(user.Email, "Password123*"), cancellationToken);
        if (!auth.IsSuccessStatusCode)
            throw new ApplicationException();

        //nastaveni access tokenu
        var authResponse = await auth.Content.ReadFromJsonAsync<AuthResponse>(cancellationToken: cancellationToken);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResponse!.access_token);

        userHttpClients.Add(client);

        //spousteni tasku
        for (int i = 0; i < taskCount; i++)
        {
            using var stream = new MemoryStream();
            await stream.WriteAsync(Encoding.UTF8.GetBytes("Simulation"), cancellationToken);
            stream.Position = 0;

            var response = await client.SendMultipartFormDataAsync("api/task", stream, new SimTaskModel
            {
                Description = "sim",
                Name = "sim"
            }, "simulation");

            var tmp = await response.Content.ReadFromJsonAsync<TaskResponse>(cancellationToken: cancellationToken);
            Console.WriteLine($"User '{user.Email}' created new task with id '{tmp!.Id}'");
            await Task.Delay(TimeSpan.FromSeconds(random.Next(simulationOptions.DelayMin, simulationOptions.DelayMax)), cancellationToken);
        }
    }
}