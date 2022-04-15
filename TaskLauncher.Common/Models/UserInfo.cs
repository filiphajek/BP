namespace TaskLauncher.Common.Models;

public class UserInfo
{
    public static readonly UserInfo Anonymous = new();

    /// <summary>
    /// Určuje, zda je uživatel přihlášen
    /// </summary>
    /// <example>true</example>
    public bool IsAuthenticated { get; set; }

    /// <summary>
    /// Typ claimu se jménem
    /// </summary>
    /// <example>http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name</example>
    public string NameClaimType { get; set; }

    /// <summary>
    /// Claims (nároky)
    /// </summary>
    public List<ClaimValue> Claims { get; set; } = new();
}
