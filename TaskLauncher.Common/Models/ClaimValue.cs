namespace TaskLauncher.Common.Models;

public class ClaimValue
{
    /// <summary>
    /// Typ nároku
    /// </summary>
    /// <example>email</example>
    public string Type { get; set; }

    /// <summary>
    /// Hodnota nároku
    /// </summary>
    /// <example>test@email.com</example>
    public string Value { get; set; }
}