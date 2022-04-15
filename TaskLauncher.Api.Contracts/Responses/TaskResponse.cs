using TaskLauncher.Common.Enums;

namespace TaskLauncher.Api.Contracts.Responses;

public record TaskResponse
{
    /// <summary>
    /// Id úlohy
    /// </summary>
    /// <example>f8fe32da-71b9-4ee1-a4cd-47e395259aeb</example>
    public Guid Id { get; set; }

    /// <summary>
    /// Cesta k souboru definujicí úlohu
    /// </summary>
    /// <example>task.txt</example>
    public string ResultFile { get; set; }

    /// <summary>
    /// Id uživatele
    /// </summary>
    /// <example>auth0|622033411a44b70076f27900</example>
    public string UserId { get; set; }

    /// <summary>
    /// Jméno úlohy
    /// </summary>
    /// <example>Task1</example>
    public string Name { get; set; }

    /// <summary>
    /// Popis úlohy
    /// </summary>
    /// <example>Popis</example>
    public string Description { get; set; }

    /// <summary>
    /// Čas vytvoření úlohy
    /// </summary>
    public DateTime CreationDate { get; set; }

    /// <summary>
    /// Aktuální stav
    /// </summary>
    /// <example>0</example>
    public TaskState ActualStatus { get; set; } = TaskState.Created;

    /// <summary>
    /// Určuje prioritu
    /// </summary>
    /// <example>true</example>
    public bool IsPriority { get; set; }
}
