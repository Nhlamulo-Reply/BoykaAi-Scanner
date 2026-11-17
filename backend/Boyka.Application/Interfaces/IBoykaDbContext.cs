using Boyka.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Boyka.Application.Interfaces;

public interface IBoykaDbContext
{
    DbSet<TradingAccount> TradingAccounts { get; }
    DbSet<TradeSignal> TradeSignals { get; }
    DbSet<TradeOrder> TradeOrders { get; }
    DbSet<TradeExecution> TradeExecutions { get; }
    DbSet<RiskProfile> RiskProfiles { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
