namespace TaskLauncher.WebApp.Client.Pages.Admin;

public partial class Users
{
    protected bool isLoading = false;

    protected List<Auth0.ManagementApi.Models.User>? users;

    protected async override Task OnInitializedAsync()
    {
        isLoading = true;
        SpaManagementApiClient apiClient = new("localhost:5001/auth0api");
        users = (await apiClient.Users.GetAllAsync(new())).ToList();
        isLoading = false;
    }
}
