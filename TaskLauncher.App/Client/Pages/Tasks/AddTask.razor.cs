using Blazored.Toast.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using System.Net.Http.Json;
using TaskLauncher.Api.Contracts.Responses;
using TaskLauncher.App.Client.Models;
using TaskLauncher.App.Client.Store;
using TaskLauncher.Common.Extensions;

namespace TaskLauncher.App.Client.Pages.Tasks;

public partial class AddTask
{
    protected TaskModel Model { get; set; } = new();
    protected List<string> errorMessages = new();
    private string fileName;
    private IBrowserFile file;

    [Inject]
    public IToastService ToastService { get; set; }

    [Inject]
    public TokenStore TokenStore { get; set; }

    [Inject]
    protected NavigationManager navigationManager { get; set; }

    [Inject]
    protected ApiClient Client { get; set; }

    //vytvoreni tasku, poslani http dotazu se zadanym souborem
    private async Task Create()
    {
        if (string.IsNullOrEmpty(fileName))
            errorMessages.Add("Select file");

        if (errorMessages.Count > 0)
        {
            return;
        }

        using (var stream = file.OpenReadStream())
        {
            var response = await Client.SendMultipartFormDataAsync("api/task", stream, Model, file.Name);
            if (response.IsSuccessStatusCode)
            {
                navigationManager.NavigateTo("/tasks");
                var balance = await Client.GetFromJsonAsync<TokenBalanceResponse>("api/token");
                await TokenStore.UpdateBalanceAsync(balance!.CurrentAmount.ToString());
            }
            else
            {
                var tmp = await response.Content.ReadFromJsonAsync<ErrorMessageResponse>();
                if(tmp is not null)
                {
                    ToastService.ShowError(tmp.Error);
                }
                else
                {
                    ToastService.ShowError("Internal error");
                }
            }
        }
    }

    private void HandleSelected(InputFileChangeEventArgs e)
    {
        errorMessages.Clear();
        file = e.File;
        fileName = e.File.Name;
    }
}
