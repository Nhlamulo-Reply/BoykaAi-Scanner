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
    DbSet<MT5Command> MT5Commands { get; }
    DbSet<EaRegistration> EaRegistrations { get; }
    DbSet<SymbolMapping> SymbolMappings { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
