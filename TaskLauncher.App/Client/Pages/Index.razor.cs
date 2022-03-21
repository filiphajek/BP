using System.Net.Http.Json;
using TaskLauncher.Api.Contracts.Responses;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using TaskLauncher.Authorization;
using TaskLauncher.Common.Extensions;

namespace TaskLauncher.App.Client.Pages;

public partial class Index
{
    [CascadingParameter]
    public Task<AuthenticationState> authenticationStateTask { get; set; } = null!;

    UserStatResponse model = new();
    DataItem[] data = Array.Empty<DataItem>();
    List<DayTaskCountDataItem> data2 = new();

    bool loading = false;
    bool switchVip1 = false;
    bool switchVip2 = false;
    bool hasVipTasks = false;

    protected override async Task OnInitializedAsync()
    {
        var state = await authenticationStateTask;
        if (!state.User.Identity!.IsAuthenticated)
            return;

        if (!state.User.TryGetClaimAsBool(TaskLauncherClaimTypes.Registered, out var registered) || !registered)
            return;
        if (!state.User.TryGetClaimAsBool(TaskLauncherClaimTypes.EmailVerified, out var verified) || !verified)
            return;

        state.User.TryGetClaimAsBool(TaskLauncherClaimTypes.Vip, out switchVip1);
        switchVip2 = switchVip1;

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

    void SwitchDonutGraph()
    {
        switchVip1 = !switchVip1;
        model = overallStats.Single(i => i.IsVip = switchVip1);
    }

    void SwitchColumnGraph()
    {
        switchVip2 = !switchVip2;
        model = overallStats.Single(i => i.IsVip = switchVip2);
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
        var firstDay = data2.FirstOrDefault(i => i.Count != 0);
        if(firstDay is not null)
        {
            var firstDayIndex = data2.IndexOf(firstDay);
            data2.RemoveRange(0, firstDayIndex);
        }
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

    List<UserStatResponse> overallStats = new();

    async Task LoadOverallStats(string path)
    {
        overallStats = (await client.GetFromJsonAsync<List<UserStatResponse>>(path))!;
        model = overallStats.Single(i => !i.IsVip);
        hasVipTasks = overallStats.Single(i => i.IsVip).AllTaskCount != 0; 
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
