namespace TaskLauncher.Api.Contracts.Responses;

public record BanResponse
{
    public Guid Id { get; set; }
    public DateTime Started { get; set; }
    public DateTime? Ended { get; set; }
    public string Description { get; set; }
    public string Email { get; set; }
    public string UserId { get; set; }
}
