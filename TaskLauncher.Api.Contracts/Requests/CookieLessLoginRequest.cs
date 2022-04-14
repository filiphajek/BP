namespace TaskLauncher.Api.Contracts.Requests;

public record CookieLessLoginRequest
{
    /// <summary>
    /// Jméno nebo email uživatele
    /// </summary>
    /// <example>user@email.com</example>
    public string Name { get; }

    /// <summary>
    /// Heslo
    /// </summary>
    /// <example>Heslo123*</example>
    public string Password { get; }

    public CookieLessLoginRequest(string name, string password)
    {
        Name = name;
        Password = password;
    }
}
