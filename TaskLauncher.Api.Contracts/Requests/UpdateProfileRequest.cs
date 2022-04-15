namespace TaskLauncher.Api.Contracts.Requests;

public record UpdateProfileRequest
{
    /// <summary>
    /// Nový obrázek
    /// </summary>
    /// <example>www.domena.cz/path.jpg</example>
    public string Picture { get; set; }

    /// <summary>
    /// Nová přezdívka
    /// </summary>
    /// <example>Filip</example>
    public string Nickname { get; set; }
}