using Boyka.Application.DTOs.Risk;

namespace Boyka.Application.Interfaces;

public interface IRiskProfileService
{
    Task<RiskProfileResponse?> GetByAccountIdAsync(Guid accountId);
    Task<RiskProfileResponse> UpsertAsync(Guid accountId, UpsertRiskProfileRequest request);
}
