namespace Boyka.Application.DTOs.MT5;

public class MT5PositionSnapshotRequest
{
    public List<MT5PositionItem> Positions { get; set; } = new();
}

public class MT5PositionItem
{
    public long BrokerTicket { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string Direction { get; set; } = string.Empty;
    public decimal Volume { get; set; }
    public decimal OpenPrice { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal StopLoss { get; set; }
    public decimal TakeProfit { get; set; }
    public decimal Profit { get; set; }
    public DateTime? OpenedAtUtc { get; set; }
}
