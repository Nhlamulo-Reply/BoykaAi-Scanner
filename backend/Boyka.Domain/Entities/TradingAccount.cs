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

    public ICollection<TradeOrder> TradeOrders { get; set; } = new HashSet<TradeOrder>();
    public ICollection<RiskProfile> RiskProfiles { get; set; } = new HashSet<RiskProfile>();
}
