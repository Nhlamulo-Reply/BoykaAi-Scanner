using System.ComponentModel.DataAnnotations;

namespace Boyka.Application.DTOs.Accounts;

public class CreateTradingAccountRequest
{
    [Required]
    [MaxLength(100)]
    public string AccountName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string BrokerName { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string AccountNumber { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string ServerName { get; set; } = string.Empty;

    [Required]
    [MaxLength(10)]
    public string AccountCurrency { get; set; } = "USD";

    public bool IsDemo { get; set; }

    [MaxLength(50)]
    public string ExecutionMode { get; set; } = "Demo";
}
