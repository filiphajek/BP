using TaskLauncher.Common.Enums;

namespace TaskLauncher.Common.Models;

public class EventModel
{
    public Guid Id { get; set; }
    public TaskState Status { get; set; }
    public DateTime Time { get; set; }
    public Guid TaskId { get; set; }
}