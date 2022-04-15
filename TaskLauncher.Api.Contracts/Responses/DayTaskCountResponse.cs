namespace TaskLauncher.Api.Contracts.Responses;

public record DayTaskCountResponse
{
    public DayTaskCountResponse(int count, DateTime date)
    {
        Count = count;
        Date = date;
    }

    /// <summary>
    /// Počet úloh
    /// </summary>
    /// <example>25</example>
    public int Count { get; set; }

    /// <summary>
    /// Den
    /// </summary>
    public DateTime Date { get; set; }
}
