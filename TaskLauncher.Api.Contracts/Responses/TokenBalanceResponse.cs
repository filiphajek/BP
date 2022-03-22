namespace TaskLauncher.Api.Contracts.Responses;

public record TokenBalanceResponse
{
    public double CurrentAmount { get; set; }
    public DateTime LastAdded { get; set; }
}
