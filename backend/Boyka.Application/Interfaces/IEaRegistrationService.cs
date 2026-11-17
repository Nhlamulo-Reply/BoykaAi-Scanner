using Boyka.Application.DTOs.MT5;
using Boyka.Domain.Entities;

namespace Boyka.Application.Interfaces;

public interface IEaRegistrationService
{
    Task<ProvisionEaResponse> ProvisionAsync(ProvisionEaRequest request);
    Task RevokeAsync(Guid registrationId);
    Task<EaRegistration?> ValidateApiKeyAsync(string rawApiKey);
}
