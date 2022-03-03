﻿using Microsoft.EntityFrameworkCore;
using TaskLauncher.Api.DAL.Entities;

namespace TaskLauncher.Api.DAL;

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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EventEntity>().HasQueryFilter(i => i.UserId == service.GetUserId());
        modelBuilder.Entity<TaskEntity>().HasQueryFilter(i => i.UserId == service.GetUserId());
        modelBuilder.Entity<PaymentEntity>().HasQueryFilter(i => i.UserId == service.GetUserId());
        modelBuilder.Entity<TokenBalanceEntity>().HasQueryFilter(i => i.UserId == service.GetUserId());

        base.OnModelCreating(modelBuilder);
    }
}