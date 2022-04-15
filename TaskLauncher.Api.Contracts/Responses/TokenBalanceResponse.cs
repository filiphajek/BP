namespace TaskLauncher.Api.Contracts.Responses;

public record TokenBalanceResponse
{
    /// <summary>
    /// Aktuální častka na účtě
    /// </summary>
    /// <example>125</example>
    public double CurrentAmount { get; set; }

    /// <summary>
    /// Datum posledního přidání tokenu
    /// </summary>
    public DateTime LastAdded { get; set; }
}
