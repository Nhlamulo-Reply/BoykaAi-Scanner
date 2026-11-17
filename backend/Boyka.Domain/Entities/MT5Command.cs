using Boyka.Domain.Enums;

namespace Boyka.Domain.Entities;

public class MT5Command
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TradeOrderId { get; set; }
    public Guid TradingAccountId { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public TradeDirection Direction { get; set; }
    public decimal Volume { get; set; }
    public decimal EntryPrice { get; set; }
    public decimal StopLoss { get; set; }
    public decimal TakeProfit { get; set; }
    public MT5CommandStatus Status { get; set; } = MT5CommandStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
    public DateTime? AttemptedAt { get; set; }
    public int RetryCount { get; set; } = 0;
    public string? LastError { get; set; }

    public TradeOrder? TradeOrder { get; set; }
    public TradingAccount? TradingAccount { get; set; }
}
