namespace TaskLauncher.Common.Models;

public class UserRegistrationModel
{
    /// <summary>
    /// Jméno
    /// </summary>
    /// <example>Filip</example>
    public string FirstName { get; set; }

    /// <summary>
    /// Příjmení
    /// </summary>
    /// <example>Hájek</example>
    public string LastName { get; set; }

    /// <summary>
    /// Přezdívka
    /// </summary>
    /// <example>fila</example>
    public string NickName { get; set; }

    /// <summary>
    /// Telofonní číslo
    /// </summary>
    /// <example>+420123456789</example>
    public string PhoneNumber { get; set; }

    /// <summary>
    /// Url adresa obrázku
    /// </summary>
    /// <example>www.pictures.com/path.jpg</example>
    public string Picture { get; set; } = string.Empty;

    /// <summary>
    /// Email
    /// </summary>
    /// <example>test@email.com</example>
    public string Email { get; set; } = string.Empty;
}
