using Auth0.ManagementApi.Models;
using TaskLauncher.Common.Models;

namespace TaskLauncher.Api.Contracts.Responses;

public record ErrorResponse
{
    public List<ErrorModel> Errors { get; set; } = new();
}

public record BanResponse
{
    public Guid Id { get; set; }
    public DateTime Started { get; set; }
    public DateTime? Ended { get; set; }
    public string Description { get; set; }
    public string Email { get; set; }
    public string UserId { get; set; }
}

public record PaymentResponse
{
    public Guid Id { get; set; }
    public string UserId { get; set; }
    public TaskResponse Task { get; set; }
    public DateTime Time { get; set; }
    public double Price { get; set; }
}
