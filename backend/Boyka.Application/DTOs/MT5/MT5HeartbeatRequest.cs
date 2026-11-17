namespace Boyka.Application.DTOs.MT5;

public class MT5HeartbeatRequest
{
    public string? EaVersion { get; set; }
    public bool TerminalConnected { get; set; }
    public decimal? AccountBalance { get; set; }
    public decimal? AccountEquity { get; set; }
    public string? AccountCurrency { get; set; }
}
