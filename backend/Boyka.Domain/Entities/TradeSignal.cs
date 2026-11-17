using Boyka.Domain.Enums;

namespace Boyka.Domain.Entities;

public class TradeSignal
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Symbol { get; set; } = string.Empty;
    public TradeDirection Direction { get; set; }
    public decimal EntryPrice { get; set; }
    public decimal StopLoss { get; set; }
    public decimal TakeProfit { get; set; }
    public TradeSignalStatus Status { get; set; } = TradeSignalStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<TradeOrder> TradeOrders { get; set; } = new HashSet<TradeOrder>();
}
