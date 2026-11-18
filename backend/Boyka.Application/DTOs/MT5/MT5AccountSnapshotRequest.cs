namespace Boyka.Application.DTOs.MT5;

public class MT5AccountSnapshotRequest
{
    public long AccountLogin { get; set; }
    public string BrokerCompany { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public decimal Equity { get; set; }
    public decimal Margin { get; set; }
    public decimal FreeMargin { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string ServerName { get; set; } = string.Empty;
    public int? Leverage { get; set; }
}
