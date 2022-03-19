using Auth0.ManagementApi.Models;

namespace TaskLauncher.Common.Models;

public class UserModel : User
{
    public User Original { get; set; }
    public bool Vip { get; set; }
    public bool Registered { get; set; }
    public bool IsAdmin { get; set; }
    public string TokenBalance { get; set; }
}
