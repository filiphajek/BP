using TaskLauncher.Common.Enums;

namespace TaskLauncher.Api.Contracts.Requests;

public record CookieLessLoginRequest(string Name, string Password);

public record TaskCreateRequest
{
    public string Name { get; set; }
    public string Description { get; set; }
}

public record TaskStatusUpdateRequest
{
    public Guid Id { get; set; }
    public DateTime Time { get; set; }
    public TaskState State { get; set; }
}
