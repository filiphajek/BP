using Microsoft.AspNetCore.Components;
using System.Net.Http.Json;
using TaskLauncher.Api.Contracts.Requests;
using TaskLauncher.Api.Contracts.Responses;

namespace TaskLauncher.WebApp.Client.Pages.Admin;

public partial class Config
{
    public class ConfigItem
    {
        public bool Changed { get; set; } = false;
        public ConfigResponse Config { get; }

        public ConfigItem(ConfigResponse config)
        {
            Config = config;
        }
    }

    [Inject]
    public HttpClient Client { get; set; }

    protected List<ConfigItem>? configs = new();

    bool addConfig = false;

    AddOrUpdateConfigValueRequest model = new();

    async Task SaveChangesAsync()
    {
        foreach(var config in configs!)
        {
            if(config.Changed)
            {
                await Client.PostAsJsonAsync("api/config", new AddOrUpdateConfigValueRequest
                {
                    Value = config.Config.Value,
                    Key = config.Config.Key,
                    Description = config.Config.Description
                });
            }
        }
    }

    async Task RemoveConfigAsync(string key)
    {
        await Client.DeleteAsync($"api/config?key={key}");
        configs!.Remove(configs.Single(i => i.Config.Key == key));
    }

    protected override async Task OnInitializedAsync()
    {
        var tmp = await Client.GetFromJsonAsync<List<ConfigResponse>>("api/config");
        configs = tmp!.Select(i => new ConfigItem(i)).ToList();
    }

    void ValueChanged(string key)
    {
        var tmp = configs!.Single(i => i.Config.Key == key);
        tmp.Changed = true;
    }

    async Task OnSubmit()
    {
        if(configs!.SingleOrDefault(i => i.Config.Key == model.Key) is not null)
        {
            //error message
            return;
        }
        var result = await Client.PostAsJsonAsync("api/config", model);
        var newConfig = await result.Content.ReadFromJsonAsync<ConfigResponse>();
        configs!.Add(new(newConfig!));
    }
}