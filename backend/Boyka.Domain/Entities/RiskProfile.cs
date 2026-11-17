namespace Boyka.Domain.Entities;

public class RiskProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TradingAccountId { get; set; }
    public decimal RiskPercentage { get; set; }
    public decimal MaximumLotSize { get; set; }
    public decimal MaximumDailyLoss { get; set; }
    public int MaximumOpenPositions { get; set; }
    public bool IsEnabled { get; set; } = true;

    public TradingAccount? TradingAccount { get; set; }
}
