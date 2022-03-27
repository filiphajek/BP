﻿using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TaskLauncher.Api.Contracts.Requests;
using TaskLauncher.Api.Contracts.Responses;
using TaskLauncher.Common.Configuration;
using TaskLauncher.Common.Extensions;

namespace TaskLauncher.Simulation;

public class SimulationService : BackgroundService
{
    private readonly UserFactory userFactory;
    private readonly ILogger<SimulationService> logger;
    private readonly ServiceAddresses options;
    private readonly SimulationConfig simulationOptions;
    private readonly Random random = new(Guid.NewGuid().GetHashCode());

    public SimulationService(UserFactory userFactory, 
        ILogger<SimulationService> logger, 
        IOptions<ServiceAddresses> options, 
        IOptions<SimulationConfig> simulationOptions)
    {
        this.userFactory = userFactory;
        this.logger = logger;
        this.options = options.Value;
        this.simulationOptions = simulationOptions.Value;
    }

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

    private async Task UserSimulation(bool vip, int taskCount, CancellationToken cancellationToken)
    {
        var user = await userFactory.CreateUser(vip);
        HttpClient client = new() { BaseAddress = options.WebApiAddressUri };

        var auth = await client.PostAsJsonAsync("loginbypasswordflow", new CookieLessLoginRequest(user.Email, "Password123*"), cancellationToken);
        if (!auth.IsSuccessStatusCode)
            throw new ApplicationException();

        var authResponse = await auth.Content.ReadFromJsonAsync<AuthResponse>(cancellationToken: cancellationToken);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResponse!.access_token);

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

        await userFactory.RemoveUser(user.UserId);
    }
}