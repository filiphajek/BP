namespace TaskLauncher.Api.Contracts.Responses;

public record PaymentResponse
{
    /// <summary>
    /// Id platby
    /// </summary>
    /// <example>f8fe32da-71b9-4ee1-a4cd-47e395259aeb</example>
    public Guid Id { get; set; }

    /// <summary>
    /// Id platícího uživatele
    /// </summary>
    /// <example>auth0|622033411a44b70076f27900</example>
    public string UserId { get; set; }

    /// <summary>
    /// Id úlohy na kterou se platba vztahuje
    /// </summary>
    /// <example>f8fe32da-71b9-4ee1-a4cd-47e395259aeb</example>
    public Guid TaskId { get; set; }

    /// <summary>
    /// Čas provedení platby
    /// </summary>
    public DateTime Time { get; set; }

    /// <summary>
    /// Cena
    /// </summary>
    /// <example>3</example>
    public double Price { get; set; }
}
