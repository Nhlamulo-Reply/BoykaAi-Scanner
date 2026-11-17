using Boyka.Domain.Enums;

namespace Boyka.Application.DTOs.Trades;

public class TradeOrderResponse
{
    public Guid Id { get; set; }
    public Guid TradingAccountId { get; set; }
    public Guid? TradeSignalId { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public TradeDirection Direction { get; set; }
    public decimal Volume { get; set; }
    public decimal EntryPrice { get; set; }
    public decimal StopLoss { get; set; }
    public decimal TakeProfit { get; set; }
    public TradeOrderStatus Status { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
