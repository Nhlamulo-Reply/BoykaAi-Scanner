using Boyka.Domain.Enums;

namespace Boyka.Application.DTOs.MT5;

public class MT5CommandResponse
{
    public Guid CommandId { get; set; }
    public Guid AccountId { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public TradeDirection Direction { get; set; }
    public decimal Volume { get; set; }
    public decimal EntryPrice { get; set; }
    public decimal StopLoss { get; set; }
    public decimal TakeProfit { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
}
