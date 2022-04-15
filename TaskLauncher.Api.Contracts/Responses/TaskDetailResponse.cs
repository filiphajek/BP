namespace TaskLauncher.Api.Contracts.Responses;

public record TaskDetailResponse : TaskResponse
{
    /// <summary>
    /// Udalosti
    /// </summary>
    public List<EventResponse> Events { get; set; } = new();
    
    /// <summary>
    /// Platby
    /// </summary>
    public SimplePaymentResponse Payment { get; set; }
}
