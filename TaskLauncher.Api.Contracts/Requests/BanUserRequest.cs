namespace TaskLauncher.Api.Contracts.Requests;

public record BanUserRequest
{
    /// <summary>
    /// Id uživatele
    /// </summary>
    /// <example>auth0|622033411a44b70076f27900</example>
    public string UserId { get; set; }

    /// <summary>
    /// Důvod udělení blokace
    /// </summary>
    /// <example>Podezřelé chování</example>
    public string Reason { get; set; }
}
