namespace TaskLauncher.Api.Contracts.Responses;

public record PaymentResponse
{
    public Guid Id { get; set; }
    public string UserId { get; set; }
    public Guid TaskId { get; set; }
    public DateTime Time { get; set; }
    public double Price { get; set; }
}
