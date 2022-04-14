namespace TaskLauncher.Api.Contracts.Requests;

public record TaskCreateRequest
{
    /// <summary>
    /// Jméno nové úlohy
    /// </summary>
    /// <example>Úloha 1</example>
    public string Name { get; set; }

    /// <summary>
    /// Popis úlohy
    /// </summary>
    /// <example>Tato úloha počítá ...</example>
    public string Description { get; set; }
}
