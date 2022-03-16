namespace TaskLauncher.Api.Contracts.Requests;

public class AddOrUpdateConfigValueRequest
{
    public string Key { get; set; }
    public string Value { get; set; }
    public string Description { get; set; }
}

public record UpdateBalanceRequest
{
    public string UserId { get; set; }
    public double Amount { get; set; }
}