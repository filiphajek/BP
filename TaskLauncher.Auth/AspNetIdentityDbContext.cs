using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace TaskLauncher.Auth;

public class ApplicationUser : IdentityUser
{
	public bool Blocked { get; set; }
}

public class AspNetIdentityDbContext : IdentityDbContext
{
	public AspNetIdentityDbContext(DbContextOptions<AspNetIdentityDbContext> options) : base(options)
	{
	}
}