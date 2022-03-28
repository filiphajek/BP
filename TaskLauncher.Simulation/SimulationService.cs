using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TaskLauncher.Api.Contracts.Requests;
using TaskLauncher.Api.Contracts.Responses;
using TaskLauncher.Common.Extensions;

namespace TaskLauncher.Simulation;

public class ApiClient : HttpClient
{
    public HttpClient Client { get; }

    public ApiClient(HttpClient client) : base()
    {
        Client = client;
        BaseAddress = client.BaseAddress;
    }
}

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
        var response = await httpClient.GetAsync("health", cancellationToken);
        while (!response.IsSuccessStatusCode)
        {
            response = await httpClient.GetAsync("health", cancellationToken);
            logger.LogInformation("Waiting for server");
            await Task.Delay(5, cancellationToken);
        }
        await userFactory.Initialize();
    }

    /// <summary>
    /// Hlavni funkce simulace
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var tasks = new List<Task>();

        for (int i = 0; i < simulationOptions.NormalUsers; i++)
        {
            var tmp = UserSimulation(false, simulationOptions.TaskCount, stoppingToken);
            tasks.Add(tmp);
        }

        for (int i = 0; i < simulationOptions.VipUsers; i++)
        {
            var tmp = UserSimulation(true, simulationOptions.TaskCount, stoppingToken);
            tasks.Add(tmp);
        }

        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Simulace uzivatela
    /// Vytvori a zaregistruje se uzivatel
    /// Postupne vytvari nekolik tasku
    /// </summary>
    private async Task UserSimulation(bool vip, int taskCount, CancellationToken cancellationToken)
    {
        //vytvoreni uzivatele
        var user = await userFactory.CreateUser(vip);
        HttpClient client = httpClientFactory.CreateClient("default");

        //prihlaseni
        var auth = await client.PostAsJsonAsync("loginbypasswordflow", new CookieLessLoginRequest(user.Email, "Password123*"), cancellationToken);
        if (!auth.IsSuccessStatusCode)
            throw new ApplicationException();

        //nastaveni access tokenu
        var authResponse = await auth.Content.ReadFromJsonAsync<AuthResponse>(cancellationToken: cancellationToken);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResponse!.access_token);

        //spousteni tasku
        for (int i = 0; i < taskCount; i++)
        {
            using var stream = new MemoryStream();
            using var streamWriter = new StreamWriter(stream);
            streamWriter.WriteLine("Simulation");
            var response = await client.SendMultipartFormDataAsync("api/task", stream, new SimTaskModel
            {
                Description = "sim",
                Name = ""
            }, "simulation");
            await Task.Delay(TimeSpan.FromSeconds(random.Next(simulationOptions.DelayMin, simulationOptions.DelayMax)), cancellationToken);
        }

        //odstraneni uzivatele
        await client.DeleteAsync("api/user", cancellationToken);
    }
}