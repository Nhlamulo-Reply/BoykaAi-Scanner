using Boyka.Domain.Enums;

namespace Boyka.Domain.Entities;

public class TradeExecution
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TradeOrderId { get; set; }
    public long? MT5TicketNumber { get; set; }
    public decimal ExecutionPrice { get; set; }
    public decimal ExecutedVolume { get; set; }
    public TradeExecutionStatus ExecutionStatus { get; set; } = TradeExecutionStatus.Pending;
    public string? ErrorMessage { get; set; }
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;

    public TradeOrder? TradeOrder { get; set; }
}
