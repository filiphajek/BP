using System.Net.Http.Json;
using TaskLauncher.Api.Contracts.Responses;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using TaskLauncher.Authorization;

namespace TaskLauncher.App.Client.Pages;

public partial class Index
{
    [CascadingParameter]
    public Task<AuthenticationState> authenticationStateTask { get; set; } = null!;

    UserStatResponse model = new();
    DataItem[] data = Array.Empty<DataItem>();

    bool loading = false;

    protected override async Task OnInitializedAsync()
    {
        var state = await authenticationStateTask;
        if (!state.User.Identity!.IsAuthenticated)
            return;

        loading = true;
        if (state.User.IsInRole(TaskLauncherRoles.User))
        {
            model = (await client.GetFromJsonAsync<UserStatResponse>("api/stat"))!;
            data = new DataItem[]{new("Success", model.SuccessTasks), new("Failed", model.FailedTasks), new("Crashed", model.CrashedTasks), new("Timeouted", model.TimeoutedTasks), };
            loading = false;
            return;
        }
        model = (await client.GetFromJsonAsync<UserStatResponse>("api/stat/all"))!;
        data = new DataItem[] { new("Success", model.SuccessTasks), new("Failed", model.FailedTasks), new("Crashed", model.CrashedTasks), new("Timeouted", model.TimeoutedTasks), };
        loading = false;
    }

    record DataItem(string Description, double Number);
}
