using System.ComponentModel.DataAnnotations;

namespace TaskLauncher.Common.Models;

public class UserRegistrationModel
{
    [Required]
    public string FirstName { get; set; }
    [Required]
    public string LastName { get; set; }
    [Required]
    public string NickName { get; set; }
    
    [Required]
    [Phone]
    public string PhoneNumber { get; set; }
    public string Picture { get; set; }
}
