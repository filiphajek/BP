namespace TaskLauncher.Api.Contracts.Requests;

public record UpdateBalanceRequest
{
    /// <summary>
    /// Id uživatele
    /// </summary>
    /// <example>auth0|622033411a44b70076f27900</example>
    public string UserId { get; set; }

    /// <summary>
    /// Částka
    /// </summary>
    /// <example>50</example>
    public double Amount { get; set; }
}