using Boyka.Application.DTOs.Risk;
using Boyka.Application.Interfaces;
using Boyka.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Boyka.Application.Services;

public class RiskProfileService : IRiskProfileService
{
    private readonly IBoykaDbContext _context;

    public RiskProfileService(IBoykaDbContext context)
    {
        _context = context;
    }

    public async Task<RiskProfileResponse?> GetByAccountIdAsync(Guid accountId)
    {
        var profile = await _context.RiskProfiles
            .FirstOrDefaultAsync(r => r.TradingAccountId == accountId);
        return profile is null ? null : MapToResponse(profile);
    }

    public async Task<RiskProfileResponse> UpsertAsync(Guid accountId, UpsertRiskProfileRequest request)
    {
        var existing = await _context.RiskProfiles
            .FirstOrDefaultAsync(r => r.TradingAccountId == accountId);

        if (existing is null)
        {
            var profile = new RiskProfile
            {
                Id = Guid.NewGuid(),
                TradingAccountId = accountId,
                RiskPercentage = request.RiskPercentage,
                MaximumLotSize = request.MaximumLotSize,
                MaximumDailyLoss = request.MaximumDailyLoss,
                MaximumOpenPositions = request.MaximumOpenPositions,
                IsEnabled = request.IsEnabled
            };
            _context.RiskProfiles.Add(profile);
            await _context.SaveChangesAsync();
            return MapToResponse(profile);
        }
        else
        {
            existing.RiskPercentage = request.RiskPercentage;
            existing.MaximumLotSize = request.MaximumLotSize;
            existing.MaximumDailyLoss = request.MaximumDailyLoss;
            existing.MaximumOpenPositions = request.MaximumOpenPositions;
            existing.IsEnabled = request.IsEnabled;
            await _context.SaveChangesAsync();
            return MapToResponse(existing);
        }
    }

    private static RiskProfileResponse MapToResponse(RiskProfile profile) => new()
    {
        Id = profile.Id,
        TradingAccountId = profile.TradingAccountId,
        RiskPercentage = profile.RiskPercentage,
        MaximumLotSize = profile.MaximumLotSize,
        MaximumDailyLoss = profile.MaximumDailyLoss,
        MaximumOpenPositions = profile.MaximumOpenPositions,
        IsEnabled = profile.IsEnabled
    };
}
