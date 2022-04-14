namespace TaskLauncher.Api.Contracts.Requests;

public record TaskUpdateRequest
{
    /// <summary>
    /// Nové jméno úlohy
    /// </summary>
    /// <example>Úloha 2</example>
    public string Name { get; set; }

    /// <summary>
    /// Nový popis úlohy
    /// </summary>
    /// <example>Tato úloha počítá ...</example>
    public string Description { get; set; }
}
