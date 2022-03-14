using Auth0.AuthenticationApi.Models;

namespace TaskLauncher.Common.Models;

public class UserClaimsModel
{
    public string TokenBalance { get; set; }
    public bool Vip { get; set; }
    public bool Blocked { get; set; }
    public bool EmailVerified { get; set; }
}

public class Auth0UserModel
{
    public string UserId { get; set;}
    public string FullName { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string MiddleName { get; set; }
    public string NickName { get; set; }
    public string PreferredUsername { get; set; }
    public string Profile { get; set; }
    public string Picture { get; set; }
    public string Website { get; set; }
    public string Email { get; set; }
    public bool? EmailVerified { get; set; }
    public string Gender { get; set; }
    public string Birthdate { get; set; }
    public string ZoneInformation { get; set; }
    public string Locale { get; set; }
    public string PhoneNumber { get; set; }
    public bool? PhoneNumberVerified { get; set; }
    public UserInfoAddress Address { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Dictionary<string, string> AdditionalClaims { get; set; }
}