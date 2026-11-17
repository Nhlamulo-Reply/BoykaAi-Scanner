using Boyka.Domain.Enums;

namespace Boyka.Domain.Entities;

public class TradeOrder
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TradingAccountId { get; set; }
    public Guid? TradeSignalId { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public TradeDirection Direction { get; set; }
    public decimal Volume { get; set; }
    public decimal EntryPrice { get; set; }
    public decimal StopLoss { get; set; }
    public decimal TakeProfit { get; set; }
    public TradeOrderStatus Status { get; set; } = TradeOrderStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid? MT5CommandId { get; set; }

    public TradingAccount? TradingAccount { get; set; }
    public TradeSignal? TradeSignal { get; set; }
    public ICollection<TradeExecution> Executions { get; set; } = new HashSet<TradeExecution>();
}
