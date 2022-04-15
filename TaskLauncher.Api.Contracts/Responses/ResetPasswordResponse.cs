namespace TaskLauncher.Api.Contracts.Responses;

public record ResetPasswordResponse
{
    public ResetPasswordResponse(string message)
    {
        Message = message;
    }

    /// <summary>
    /// Zpráva od auth0 o změnění hesla
    /// </summary>
    /// <example>Adresa pro změnění hesla je ...</example>
    public string Message { get; }
}