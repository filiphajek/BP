using Microsoft.EntityFrameworkCore;
using TaskLauncher.App.DAL.Entities;

namespace TaskLauncher.App.DAL;

/// <summary>
/// Pristup do databaze
/// </summary>
public class AppDbContext : DbContext
{
    private readonly IUserIdProvider service;

    public AppDbContext(IUserIdProvider service, DbContextOptions<AppDbContext> dbContext) : base(dbContext)
    {
        this.service = service;
    }

    public DbSet<EventEntity> Events { get; set; }
    public DbSet<TaskEntity> Tasks { get; set; }
    public DbSet<PaymentEntity> Payments { get; set; }
    public DbSet<TokenBalanceEntity> TokenBalances { get; set; }
    public DbSet<BanEntity> Bans { get; set; }
    public DbSet<ConfigEntity> Configs { get; set; }
    public DbSet<StatEntity> Stats { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ConfigEntity>().HasKey(i => new { i.Key });

        //nastaveni globalnich filteru
        modelBuilder.Entity<EventEntity>().HasQueryFilter(i => i.UserId == service.GetUserId());
        modelBuilder.Entity<TaskEntity>().HasQueryFilter(i => i.UserId == service.GetUserId());
        modelBuilder.Entity<PaymentEntity>().HasQueryFilter(i => i.UserId == service.GetUserId());
        modelBuilder.Entity<TokenBalanceEntity>().HasQueryFilter(i => i.UserId == service.GetUserId());
        modelBuilder.Entity<BanEntity>().HasQueryFilter(i => i.UserId == service.GetUserId());
        modelBuilder.Entity<StatEntity>().HasQueryFilter(i => i.UserId == service.GetUserId());

        base.OnModelCreating(modelBuilder);
    }
}