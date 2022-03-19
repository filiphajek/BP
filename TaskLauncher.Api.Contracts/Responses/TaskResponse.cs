using System.ComponentModel.DataAnnotations;
using TaskLauncher.Common.Enums;

namespace TaskLauncher.Api.Contracts.Responses;

public record TaskResponse
{
    [Key]
    public Guid Id { get; set; }
    public string TaskFile { get; set; }
    public string ResultFile { get; set; }
    public string UserId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public DateTime CreationDate { get; set; }
    public TaskState ActualStatus { get; set; } = TaskState.Created;
}
