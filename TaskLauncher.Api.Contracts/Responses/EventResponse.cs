using TaskLauncher.Common.Enums;

namespace TaskLauncher.Api.Contracts.Responses;

public record EventResponse
{
    public TaskState Status { get; set; }
    public DateTime Time { get; set; }
}
