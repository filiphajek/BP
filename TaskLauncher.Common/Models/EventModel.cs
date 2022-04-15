using TaskLauncher.Common.Enums;

namespace TaskLauncher.Common.Models;

public class EventModel
{
    /// <summary>
    /// Id události
    /// </summary>
    /// <example>f8fe32da-71b9-4ee1-a4cd-47e395259aeb</example>
    public Guid Id { get; set; }

    /// <summary>
    /// Aktuální stav
    /// </summary>
    /// <example>4</example>
    public TaskState Status { get; set; }

    /// <summary>
    /// Čas vytvoření
    /// </summary>
    public DateTime Time { get; set; }

    /// <summary>
    /// Id úlohy
    /// </summary>
    /// <example>f8fe32da-71b9-4ee1-a4cd-47e395259aeb</example>
    public Guid TaskId { get; set; }
}