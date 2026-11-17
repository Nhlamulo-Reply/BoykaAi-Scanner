using Boyka.Domain.Enums;

namespace Boyka.Application.DTOs.Signals;

public class TradeSignalResponse
{
    public Guid Id { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public TradeDirection Direction { get; set; }
    public decimal EntryPrice { get; set; }
    public decimal StopLoss { get; set; }
    public decimal TakeProfit { get; set; }
    public TradeSignalStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}
