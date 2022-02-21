using TaskLauncher.Common.Enums;

namespace TaskLauncher.Api.Contracts.Responses;

public record TokenBalanceResponse
{
    public double CurrentAmount { get; set; }
    public DateTime LastAdded { get; set; }
}

public record TaskResponse
{
    public string TaskFile { get; set; }
    public string? ResultFile { get; set; }
    public Guid Id { get; set; }
    public string UserId { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public TaskState ActualStatus { get; set; } = TaskState.Created;
}

public record TaskDetailResponse : TaskResponse
{
    public List<EventResponse> Events { get; set; }
}

public record EventResponse
{
    public string UserId { get; set; }
    public TaskState Status { get; set; }
    public DateTime Time { get; set; }
    public Guid TaskId { get; set; }
}
