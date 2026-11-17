namespace Boyka.Application.DTOs.Risk;

public class RiskProfileResponse
{
    public Guid Id { get; set; }
    public Guid TradingAccountId { get; set; }
    public decimal RiskPercentage { get; set; }
    public decimal MaximumLotSize { get; set; }
    public decimal MaximumDailyLoss { get; set; }
    public int MaximumOpenPositions { get; set; }
    public bool IsEnabled { get; set; }
}
