using Boyka.Domain.Enums;

namespace Boyka.Application.DTOs.Trades;

public class TradeExecutionResponse
{
    public Guid Id { get; set; }
    public Guid TradeOrderId { get; set; }
    public long? MT5TicketNumber { get; set; }
    public decimal ExecutionPrice { get; set; }
    public decimal ExecutedVolume { get; set; }
    public TradeExecutionStatus ExecutionStatus { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime ExecutedAt { get; set; }
}
