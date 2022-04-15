namespace TaskLauncher.Api.Contracts.Requests;

public class UpdateConfigValueRequest
{
    /// <summary>
    /// Klíč proměnné
    /// </summary>
    /// <example>autofileremove</example>
    public string Key { get; set; }

    /// <summary>
    /// Hodnota proměnné
    /// </summary>
    /// <example>5</example>
    public string Value { get; set; }
}
