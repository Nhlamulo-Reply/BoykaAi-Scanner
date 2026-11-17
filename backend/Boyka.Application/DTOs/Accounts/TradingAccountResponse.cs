namespace Boyka.Application.DTOs.Accounts;

public class TradingAccountResponse
{
    public Guid Id { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public string BrokerName { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string ServerName { get; set; } = string.Empty;
    public string AccountCurrency { get; set; } = string.Empty;
    public bool IsDemo { get; set; }
    public string ExecutionMode { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
