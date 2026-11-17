using Boyka.Application.Interfaces;
using Boyka.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Boyka.Infrastructure.Data;

public class BoykaDbContext : DbContext, IBoykaDbContext
{
    public BoykaDbContext(DbContextOptions<BoykaDbContext> options) : base(options)
    {
    }

    public DbSet<TradingAccount> TradingAccounts { get; set; } = null!;
    public DbSet<TradeSignal> TradeSignals { get; set; } = null!;
    public DbSet<TradeOrder> TradeOrders { get; set; } = null!;
    public DbSet<TradeExecution> TradeExecutions { get; set; } = null!;
    public DbSet<RiskProfile> RiskProfiles { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // TradingAccount
        modelBuilder.Entity<TradingAccount>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AccountName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.BrokerName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.AccountNumber).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ServerName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.AccountCurrency).IsRequired().HasMaxLength(10);
            entity.Property(e => e.ExecutionMode).IsRequired().HasMaxLength(50);
        });

        // TradeSignal
        modelBuilder.Entity<TradeSignal>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Symbol).IsRequired().HasMaxLength(20);
            entity.Property(e => e.EntryPrice).HasPrecision(18, 8);
            entity.Property(e => e.StopLoss).HasPrecision(18, 8);
            entity.Property(e => e.TakeProfit).HasPrecision(18, 8);
        });

        // TradeOrder
        modelBuilder.Entity<TradeOrder>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Symbol).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Volume).HasPrecision(18, 8);
            entity.Property(e => e.EntryPrice).HasPrecision(18, 8);
            entity.Property(e => e.StopLoss).HasPrecision(18, 8);
            entity.Property(e => e.TakeProfit).HasPrecision(18, 8);
            entity.HasOne(e => e.TradingAccount)
                  .WithMany(a => a.TradeOrders)
                  .HasForeignKey(e => e.TradingAccountId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.TradeSignal)
                  .WithMany(s => s.TradeOrders)
                  .HasForeignKey(e => e.TradeSignalId)
                  .OnDelete(DeleteBehavior.SetNull)
                  .IsRequired(false);
        });

        // TradeExecution
        modelBuilder.Entity<TradeExecution>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ExecutionPrice).HasPrecision(18, 8);
            entity.Property(e => e.ExecutedVolume).HasPrecision(18, 8);
            entity.Property(e => e.ErrorMessage).HasMaxLength(500);
            entity.HasOne(e => e.TradeOrder)
                  .WithMany(o => o.Executions)
                  .HasForeignKey(e => e.TradeOrderId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // RiskProfile
        modelBuilder.Entity<RiskProfile>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RiskPercentage).HasPrecision(18, 8);
            entity.Property(e => e.MaximumLotSize).HasPrecision(18, 8);
            entity.Property(e => e.MaximumDailyLoss).HasPrecision(18, 8);
            entity.HasOne(e => e.TradingAccount)
                  .WithMany(a => a.RiskProfiles)
                  .HasForeignKey(e => e.TradingAccountId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Seed data
        modelBuilder.Entity<TradingAccount>().HasData(new TradingAccount
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
            AccountName = "Demo Account",
            BrokerName = "Mock Broker",
            AccountNumber = "0001",
            ServerName = "mock-server",
            AccountCurrency = "USD",
            IsDemo = true,
            ExecutionMode = "Demo",
            IsActive = true,
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });
    }
}
