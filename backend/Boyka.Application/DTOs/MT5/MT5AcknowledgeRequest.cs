namespace Boyka.Application.DTOs.MT5;

public class MT5AcknowledgeRequest
{
    public Guid AccountId { get; set; }

    /// <summary>Accepted values: "FILLED", "PARTIAL", "REJECTED", "FAILED", "UNKNOWN"</summary>
    public string Status { get; set; } = string.Empty;

    public long? BrokerTicket { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string Direction { get; set; } = string.Empty;
    public decimal RequestedVolume { get; set; }
    public decimal ExecutedVolume { get; set; }
    public decimal ExecutionPrice { get; set; }
    public int? BrokerRetcode { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime? ExecutedAtUtc { get; set; }
}
