using TaskLauncher.Common.Enums;

namespace TaskLauncher.Api.Contracts.Requests;

public record TaskStatusUpdateRequest
{
    public Guid Id { get; set; }
    public DateTime Time { get; set; }
    public TaskState State { get; set; }
}
