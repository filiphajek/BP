namespace TaskLauncher.WebApp.Client.Pages.Admin;

public partial class Config
{
    int TaskTimeout { get; set; } = 1;
    int DeleteFilesAfter { get; set; } = 7;

    async Task SaveAsync()
    {
        await Task.Delay(11);
        TaskTimeout = 9;
        DeleteFilesAfter = 91;
        StateHasChanged();
    }
}
