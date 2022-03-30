namespace TaskLauncher.Api.Contracts.Responses;

public record DayTaskCountResponse
{
    public DayTaskCountResponse(int count, DateTime date)
    {
        Count = count;
        Date = date;
    }

    public int Count { get; set; }
    public DateTime Date { get; set; }
}
