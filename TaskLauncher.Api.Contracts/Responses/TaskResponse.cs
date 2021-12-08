using TaskLauncher.Common.Enums;

namespace TaskLauncher.Api.Contracts.Responses;

public record TaskResponse
{
    public Guid Id { get; set; }
    public string UserId { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public DateTime? Start { get; set; }
    public DateTime? End { get; set; }
    public TaskState Status { get; set; } = TaskState.Created;
}

public record TaskDetailResponse : TaskResponse
{
    public List<FileResponse> Files { get; set; }
}

public record FileResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; }
}
