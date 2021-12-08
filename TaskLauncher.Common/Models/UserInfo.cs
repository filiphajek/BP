namespace TaskLauncher.Common.Models;

public class UserInfo
{
    public static readonly UserInfo Anonymous = new();
    public bool IsAuthenticated { get; set; }
    public string NameClaimType { get; set; }
    public List<ClaimValue> Claims { get; set; } = new();
}
