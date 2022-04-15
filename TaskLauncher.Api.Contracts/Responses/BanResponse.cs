namespace TaskLauncher.Api.Contracts.Responses;

public record BanResponse
{
    /// <summary>
    /// Id banu
    /// </summary>
    /// <example>f8fe32da-71b9-4ee1-a4cd-47e395259aeb</example>
    public Guid Id { get; set; }

    /// <summary>
    /// Kdy začal ban platit
    /// </summary>
    public DateTime Started { get; set; }

    /// <summary>
    /// Kdy se ban zrušil
    /// </summary>
    public DateTime? Ended { get; set; }

    /// <summary>
    /// Popis banu
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Emailový, který je spojený s tímto banem
    /// <example>test@email.com</example>
    public string Email { get; set; }

    /// <summary>
    /// Id zabanovaného uživatele
    /// </summary>
    /// <example>auth0|622033411a44b70076f27900</example>
    public string UserId { get; set; }
}
