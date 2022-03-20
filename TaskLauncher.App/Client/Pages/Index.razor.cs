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
    List<DayTaskCountDataItem> data2 = new();

    bool loading = false;

    protected override async Task OnInitializedAsync()
    {
        var state = await authenticationStateTask;
        if (!state.User.Identity!.IsAuthenticated)
            return;

        loading = true;
        if (state.User.IsInRole(TaskLauncherRoles.User))
        {
            await LoadOverallStats("api/stat");
            await LoadTaskCountPerDay("api/stat/daycount");
            await LoadTimesStats("api/stat/times");
            loading = false;
            return;
        }
        await LoadOverallStats("api/stat/all");
        await LoadTaskCountPerDay("api/stat/dayallcount");
        await LoadTimesStats("api/stat/alltimes");
        loading = false;
    }

    async Task LoadTaskCountPerDay(string path)
    {
        var days = (await client.GetFromJsonAsync<List<DayTaskCountResponse>>(path))!;
        for (int i = 0; i < 31; i++)
        {
            data2.Add(new DayTaskCountDataItem(0, DateTime.Now.AddDays(30 - i).Date.Day));
            foreach (var item in days)
            {
                if (data2[i].Date == item.Date.Day)
                {
                    data2[i].Count = item.Count;
                }
            }
        }
        var firstDay = data2.IndexOf(data2.First(i => i.Count != 0));
        data2.RemoveRange(0, firstDay);
    }

    List<TaskStatDataItem> nonViptimes = new();
    List<TaskStatDataItem> vipTimes = new();

    async Task LoadTimesStats(string path)
    {
        var times = (await client.GetFromJsonAsync<List<TaskStatResponse>>(path))!;
        int j = 0, k = 0;
        vipTimes = times.Where(i => i.IsVip).Select(i => new TaskStatDataItem(i.CpuTime.TotalMinutes, i.TimeInQueue.TotalMinutes, (++j).ToString())).ToList();
        nonViptimes = times.Where(i => !i.IsVip).Select(i => new TaskStatDataItem(i.CpuTime.TotalMinutes, i.TimeInQueue.TotalMinutes, (++k).ToString())).ToList();
    }

    async Task LoadOverallStats(string path)
    {
        model = (await client.GetFromJsonAsync<List<UserStatResponse>>(path))!.Single(i => !i.IsVip);
        data = new DataItem[] { new("Success", model.SuccessTasks), new("Failed", model.FailedTasks), new("Crashed", model.CrashedTasks), new("Timeouted", model.TimeoutedTasks) };
    }

    record TaskStatDataItem(double CpuTime, double TimeInQueue, string Index);
    record DataItem(string Description, double Number);
    record DayTaskCountDataItem
    {
        public DayTaskCountDataItem(int count, int date)
        {
            Count = count;
            Date = date;
        }

        public int Count { get; set; }
        public int Date { get; set; }
    }
}
