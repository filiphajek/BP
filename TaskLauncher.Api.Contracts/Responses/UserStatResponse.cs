namespace TaskLauncher.Api.Contracts.Responses;

public record UserStatResponse
{
    /// <summary>
    /// Počet všech úloh (i smazaných), které uživatel vytvořil
    /// </summary>
    /// <example>125</example>
    public int AllTaskCount { get; set; }

    /// <summary>
    /// Celkový počet dokončených úloh (úspěšně a neúspěšně)
    /// </summary>
    /// <example>100</example>
    public int FinishedTaskCount { get; set; }

    /// <summary>
    /// Celkový počet dokončených neúspěšných úloh
    /// </summary>
    /// <example>30</example>
    public int FailedTasks { get; set; }

    /// <summary>
    /// Celkový počet dokončených úspěšných úloh
    /// </summary>
    /// <example>70</example>
    public int SuccessTasks { get; set; }

    /// <summary>
    /// Celkový počet úloh, které se nestihly dokončit
    /// </summary>
    /// <example>5</example>
    public int TimeoutedTasks { get; set; }

    /// <summary>
    /// Celkový počet zhavarovaných úloh
    /// </summary>
    /// <example>10</example>
    public int CrashedTasks { get; set; }

    /// <summary>
    /// Určuje, zda jsou statistiky určeny z prioritních úloh
    /// </summary>
    /// <example>true</example>
    public bool IsVip { get; set; }
}
