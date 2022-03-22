namespace TaskLauncher.Api.Contracts.Requests;

public record UpdateBalanceRequest
{
    public string UserId { get; set; }
    public double Amount { get; set; }
}