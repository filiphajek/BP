namespace TaskLauncher.Api.Contracts.Responses;

public record SimplePaymentResponse
{
    /// <summary>
    /// Čas potvrzení platby
    /// </summary>
    /// <example>2022-04-14T14:22:03.151Z</example>
    public DateTime Time { get; set; }

    /// <summary>
    /// Uhrazená částka
    /// </summary>
    /// <example>2</example>
    public double Price { get; set; }
}