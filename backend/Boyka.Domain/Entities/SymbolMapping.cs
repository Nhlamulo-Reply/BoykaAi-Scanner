namespace Boyka.Domain.Entities;

public class SymbolMapping
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TradingAccountId { get; set; }
    public string CanonicalSymbol { get; set; } = string.Empty;
    public string BrokerSymbol { get; set; } = string.Empty;

    public TradingAccount? TradingAccount { get; set; }
}
