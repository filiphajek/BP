using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using System.Net.Http.Json;
using TaskLauncher.Api.Contracts.Responses;
using TaskLauncher.App.Client.Extensions;
using TaskLauncher.App.Client.Models;
using TaskLauncher.App.Client.Store;
using TaskLauncher.Authorization;
using TaskLauncher.Common.Extensions;

namespace TaskLauncher.App.Client.Pages.Tasks;

public partial class AddTask
{
    protected TaskModel Model { get; set; } = new();
    protected bool showError = false;
    protected List<string> errorMessages = new();
    private string fileName;
    private IBrowserFile file;

    [Inject]
    public TokenStore TokenStore { get; set; }

    [Inject]
    protected NavigationManager navigationManager { get; set; }

    private HttpClient client;

    protected override void OnInitialized()
    {
        client = HttpClientFactory.CreateApiClient();
        base.OnInitialized();
    }

    //vytvoreni tasku, poslani http dotazu se zadanym souborem
    private async Task Create()
    {
        showError = false;
        if (string.IsNullOrEmpty(fileName))
            errorMessages.Add("Select file");

        if (errorMessages.Count > 0)
        {
            showError = true;
            return;
        }

        using (var stream = file.OpenReadStream())
        {
            var response = await client.SendMultipartFormDataAsync("api/task", stream, Model, file.Name);
            if (response.IsSuccessStatusCode)
            {
                navigationManager.NavigateTo("/tasks");
                var balance = await client.GetFromJsonAsync<TokenBalanceResponse>("api/token");
                await TokenStore.UpdateBalanceAsync(balance.CurrentAmount.ToString());
            }
            else
            {
                showError = true;
            }
        }
    }

    private void HandleSelected(InputFileChangeEventArgs e)
    {
        showError = false;
        errorMessages.Clear();
        file = e.File;
        fileName = e.File.Name;
    }
}
