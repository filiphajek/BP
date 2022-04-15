using TaskLauncher.Common.Enums;

namespace TaskLauncher.Api.Contracts.Responses;

public record EventResponse
{
    /// <summary>
    /// Aktuální stav úlohy
    /// </summary>
    /// <example>1</example>
    public TaskState Status { get; set; }

    /// <summary>
    /// Čas vytvoření události
    /// </summary>
    public DateTime Time { get; set; }

    /// <summary>
    /// Popis události
    /// </summary>
    /// <example>Stalo se ...</example>
    public string Description { get; set; } = "";
}
