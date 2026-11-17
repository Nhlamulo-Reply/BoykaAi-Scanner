using System.ComponentModel.DataAnnotations;

namespace Boyka.Application.DTOs.Risk;

public class UpsertRiskProfileRequest
{
    [Range(0.01, 100.0, ErrorMessage = "RiskPercentage must be between 0.01 and 100.")]
    public decimal RiskPercentage { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "MaximumLotSize must be greater than 0.")]
    public decimal MaximumLotSize { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "MaximumDailyLoss must be greater than 0.")]
    public decimal MaximumDailyLoss { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "MaximumOpenPositions must be at least 1.")]
    public int MaximumOpenPositions { get; set; }

    public bool IsEnabled { get; set; }
}
