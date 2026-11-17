namespace Boyka.Domain.Entities;

public class EaRegistration
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TradingAccountId { get; set; }
    public string ApiKeyHash { get; set; } = string.Empty;
    public string? EaVersion { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastHeartbeatAt { get; set; }
    public DateTime? RevokedAt { get; set; }

    public TradingAccount? TradingAccount { get; set; }
}
