using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using TaskLauncher.Common.Extensions;
using TaskLauncher.WebApp.Client.Models;

namespace TaskLauncher.WebApp.Client.Pages.Tasks;

public partial class AddTask
{
    protected TaskModel Model { get; set; } = new();
    protected bool showError = false;
    protected List<string> errorMessages = new();
    private string fileName;
    private IBrowserFile file;

    [Inject]
    protected NavigationManager navigationManager { get; set; }

    [Inject]
    protected HttpClient client { get; set; }

    [Inject]
    protected HttpClient Client { get; set; }

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
                navigationManager.NavigateTo("/tasks", true);
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
