namespace TaskLauncher.Api.Contracts.Responses;

public record SimplePaymentResponse
{
    public DateTime Time { get; set; }
    public double Price { get; set; }
}