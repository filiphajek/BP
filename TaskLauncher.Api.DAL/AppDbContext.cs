using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using TaskLauncher.Api.DAL.Entities;
using TaskLauncher.Common.Extensions;

namespace TaskLauncher.Api.DAL;

public class AppDbContext : DbContext
{
    private readonly string userId = string.Empty;

    public AppDbContext(IHttpContextAccessor accessor, DbContextOptions<AppDbContext> dbContext) : base(dbContext)
    {
        //v budoucnu se toto predela, aby byla jednodussi konfigurace
        var request = accessor.HttpContext?.Request;
        if (request is not null)
        {
            //pokud se request posila na launcher endpointy, global filtry se nebudou pouzivat
            if (request.Path.Value.Contains("launcher", StringComparison.InvariantCultureIgnoreCase))
                return;
        }
        //ziskej user id a vyfiltruj podle nej databazi, uzivatel pak muze pristoupit pouze ke svym datum

        if (accessor.HttpContext is not null && accessor.HttpContext.User.IsInRole("admin"))
            return;
        accessor.HttpContext?.User.TryGetAuth0Id(out userId);
    }

    public DbSet<EventEntity> Events { get; set; }
    public DbSet<TaskEntity> Tasks { get; set; }
    public DbSet<PaymentEntity> Payments { get; set; }
    public DbSet<TokenBalanceEntity> TokenBalances { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        //konfigurace global query filteru
        modelBuilder.Entity<EventEntity>().HasQueryFilter(i => string.IsNullOrEmpty(userId) || i.UserId == userId);
        modelBuilder.Entity<TaskEntity>().HasQueryFilter(i => string.IsNullOrEmpty(userId) || i.UserId == userId);
        modelBuilder.Entity<PaymentEntity>().HasQueryFilter(i => string.IsNullOrEmpty(userId) || i.UserId == userId);
        modelBuilder.Entity<TokenBalanceEntity>().HasQueryFilter(i => string.IsNullOrEmpty(userId) || i.UserId == userId);

        base.OnModelCreating(modelBuilder);
    }
}