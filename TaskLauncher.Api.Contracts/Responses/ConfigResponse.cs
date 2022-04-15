using TaskLauncher.Common.Enums;

namespace TaskLauncher.Api.Contracts.Responses;

public record ConfigResponse
{
    /// <summary>
    /// Identifikátor konfigurační proměnné
    /// </summary>
    /// <example>tasktimeout</example>
    public string Key { get; set; }

    /// <summary>
    /// Datový typ konfigurační proměnné
    /// </summary>
    /// <example>1</example>
    public ConstantTypes Type { get; set; }

    /// <summary>
    /// Hodnota proměnné
    /// </summary>
    /// <example>17</example>
    public string Value { get; set; }

    /// <summary>
    /// Popis proměnné
    /// </summary>
    /// <example>Tato proměnná nastavuje timeout.</example>
    public string Description { get; set; }
}
