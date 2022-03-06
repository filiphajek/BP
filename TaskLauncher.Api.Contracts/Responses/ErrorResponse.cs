using Auth0.ManagementApi.Models;
using TaskLauncher.Common.Models;

namespace TaskLauncher.Api.Contracts.Responses;

public record ErrorResponse
{
    public List<ErrorModel> Errors { get; set; } = new();
}

public record BanResponse
{
    public DateTime Started { get; set; }
    public DateTime? Ended { get; set; }
    public string Description { get; set; }
}

public record PaymentResponse
{
    public TaskResponse Task { get; set; }
    public DateTime Time { get; set; }
    public double Price { get; set; }
}
