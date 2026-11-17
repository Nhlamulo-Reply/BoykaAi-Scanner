using System.ComponentModel.DataAnnotations;

namespace Boyka.Application.DTOs.MT5;

public class ProvisionEaRequest
{
    [Required]
    public Guid TradingAccountId { get; set; }

    public string? EaVersion { get; set; }
}
