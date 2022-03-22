namespace TaskLauncher.Api.Contracts.Responses;

public record TaskDetailResponse : TaskResponse
{
    public List<EventResponse> Events { get; set; } = new();
    public SimplePaymentResponse Payment { get; set; }
}
