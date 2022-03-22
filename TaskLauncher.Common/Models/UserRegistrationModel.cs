using System.ComponentModel.DataAnnotations;

namespace TaskLauncher.Common.Models;

public class UserRegistrationModel
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string NickName { get; set; }
    public string PhoneNumber { get; set; }
    public string Picture { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}
