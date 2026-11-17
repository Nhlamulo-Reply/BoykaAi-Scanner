namespace Boyka.Domain.Entities;

public class TradingAccount
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string AccountName { get; set; } = string.Empty;
    public string BrokerName { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string ServerName { get; set; } = string.Empty;
    public string AccountCurrency { get; set; } = string.Empty;
    public bool IsDemo { get; set; }
    public string ExecutionMode { get; set; } = "Demo";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool AutoTradingEnabled { get; set; } = false;

    public ICollection<TradeOrder> TradeOrders { get; set; } = new HashSet<TradeOrder>();
    public ICollection<RiskProfile> RiskProfiles { get; set; } = new HashSet<RiskProfile>();
    public ICollection<EaRegistration> EaRegistrations { get; set; } = new HashSet<EaRegistration>();
    public ICollection<SymbolMapping> SymbolMappings { get; set; } = new HashSet<SymbolMapping>();
    public ICollection<MT5Command> MT5Commands { get; set; } = new HashSet<MT5Command>();
}
