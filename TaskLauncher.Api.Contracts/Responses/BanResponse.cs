namespace TaskLauncher.Api.Contracts.Responses;

public record BanResponse
{
    /// <summary>
    /// Id blokace
    /// </summary>
    /// <example>f8fe32da-71b9-4ee1-a4cd-47e395259aeb</example>
    public Guid Id { get; set; }

    /// <summary>
    /// Kdy začala blokace platit
    /// </summary>
    public DateTime Started { get; set; }

    /// <summary>
    /// Kdy se blokace zrušila
    /// </summary>
    public DateTime? Ended { get; set; }

    /// <summary>
    /// Popis blokace
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Email, který je spojený s touto blokací
    /// <example>test@email.com</example>
    public string Email { get; set; }

    /// <summary>
    /// Id blokovaného uživatele
    /// </summary>
    /// <example>auth0|622033411a44b70076f27900</example>
    public string UserId { get; set; }
}
