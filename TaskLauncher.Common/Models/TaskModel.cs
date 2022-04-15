using TaskLauncher.Common.Enums;

namespace TaskLauncher.Common.Models;

public class TaskModel
{
    /// <summary>
    /// Id úlohy
    /// </summary>
    /// <example>f8fe32da-71b9-4ee1-a4cd-47e395259aeb</example>
    public Guid Id { get; set; }

    /// <summary>
    /// Id uživatele
    /// </summary>
    /// <example>auth0|622033411a44b70076f27900</example>
    public string UserId { get; set; }

    /// <summary>
    /// Aktuální stav
    /// </summary>
    /// <example>1</example>
    public TaskState State { get; set; } = TaskState.Created;

    /// <summary>
    /// Čas vytvoření úlohy
    /// </summary>
    public DateTime Time { get; set; }

    /// <summary>
    /// Cesta k souboru definujicí úlohu
    /// </summary>
    /// <example>task.txt</example>
    public string TaskFilePath { get; set; }

    /// <summary>
    /// Cesta k souboru obsahující výsledek
    /// </summary>
    /// <example>task.txt</example>
    public string ResultFilePath { get; set; }

    /// <summary>
    /// Určuje prioritu
    /// </summary>
    /// <example>true</example>
    public bool IsPriority { get; set; }

    /// <summary>
    /// Jméno úlohy
    /// </summary>
    /// <example>Task1</example>
    public string Name { get; set; }
}
