namespace Boyka.Application.DTOs.MT5;

public class ProvisionEaResponse
{
    public Guid RegistrationId { get; set; }
    public Guid TradingAccountId { get; set; }

    /// <summary>
    /// The only time the plaintext API key is returned.
    /// Store it securely; it cannot be retrieved again.
    /// </summary>
    public string RawApiKey { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}
