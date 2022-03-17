using Microsoft.AspNetCore.Components;
using Radzen.Blazor;
using System.Net.Http.Json;
using TaskLauncher.Api.Contracts.Requests;
using TaskLauncher.Api.Contracts.Responses;

namespace TaskLauncher.App.Client.Pages.Admin;

public partial class Config
{
    public record ConfigItem : ConfigResponse
    {
        public string OldValue { get; }

        public ConfigItem(ConfigResponse config)
        {
            OldValue = config.Value;
            Value = config.Value;
            Key = config.Key;
            Description = config.Description;
        }
    }
    public class ConfigurationModel
    {
        public List<ConfigItem>? Configs { get; set; } = new();
    }

    [Inject]
    public HttpClient Client { get; set; }

    ConfigurationModel model = new();
    RadzenTemplateForm<ConfigurationModel> form;

    async Task SaveChangesAsync()
    {
        if (!form.EditContext.Validate())
        {
            return;
        }
        foreach (var config in model.Configs!)
        {
            if(config.OldValue != config.Value)
            {
                await Client.PutAsJsonAsync("api/config", new UpdateConfigValueRequest
                {
                    Value = config.Value,
                    Key = config.Key
                });
            }
        }
    }

    protected override async Task OnInitializedAsync()
    {
        var tmp = await Client.GetFromJsonAsync<List<ConfigResponse>>("api/config");
        model.Configs = tmp!.Select(i => new ConfigItem(i)).ToList();
    }
}