namespace TaskLauncher.Api.DAL.Entities;

public record IpEntity : BaseEntity, IUserKeyProtection
{
    public string UserId { get; set; }
    public string Ipv4 { get; set; }
}