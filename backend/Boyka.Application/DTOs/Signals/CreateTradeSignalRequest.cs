using System.ComponentModel.DataAnnotations;
using Boyka.Domain.Enums;

namespace Boyka.Application.DTOs.Signals;

public class CreateTradeSignalRequest
{
    [Required]
    [MaxLength(20)]
    public string Symbol { get; set; } = string.Empty;

    [Required]
    public TradeDirection Direction { get; set; }

    [Range(0.00001, double.MaxValue, ErrorMessage = "EntryPrice must be greater than 0.")]
    public decimal EntryPrice { get; set; }

    [Range(0.00001, double.MaxValue, ErrorMessage = "StopLoss must be greater than 0.")]
    public decimal StopLoss { get; set; }

    [Range(0.00001, double.MaxValue, ErrorMessage = "TakeProfit must be greater than 0.")]
    public decimal TakeProfit { get; set; }
}
