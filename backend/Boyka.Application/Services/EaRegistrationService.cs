using System.Security.Cryptography;
using System.Text;
using Boyka.Application.DTOs.MT5;
using Boyka.Application.Interfaces;
using Boyka.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Boyka.Application.Services;

public class EaRegistrationService : IEaRegistrationService
{
    private readonly IBoykaDbContext _context;

    public EaRegistrationService(IBoykaDbContext context)
    {
        _context = context;
    }

    public async Task<ProvisionEaResponse> ProvisionAsync(ProvisionEaRequest request)
    {
        // Generate 32 random bytes → hex string as the raw key
        var rawKeyBytes = RandomNumberGenerator.GetBytes(32);
        var rawApiKey = Convert.ToHexString(rawKeyBytes).ToLowerInvariant();

        // Compute SHA-256 hex hash — never log or store the raw key
        var hash = ComputeSha256Hex(rawApiKey);

        var registration = new EaRegistration
        {
            Id = Guid.NewGuid(),
            TradingAccountId = request.TradingAccountId,
            ApiKeyHash = hash,
            EaVersion = request.EaVersion,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.EaRegistrations.Add(registration);
        await _context.SaveChangesAsync();

        return new ProvisionEaResponse
        {
            RegistrationId = registration.Id,
            TradingAccountId = registration.TradingAccountId,
            RawApiKey = rawApiKey,
            CreatedAt = registration.CreatedAt
        };
    }

    public async Task RevokeAsync(Guid registrationId)
    {
        var registration = await _context.EaRegistrations
            .FirstOrDefaultAsync(r => r.Id == registrationId);

        if (registration is null)
            throw new KeyNotFoundException($"EA registration {registrationId} not found.");

        registration.IsActive = false;
        registration.RevokedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    public async Task<EaRegistration?> ValidateApiKeyAsync(string rawApiKey)
    {
        var hash = ComputeSha256Hex(rawApiKey);

        return await _context.EaRegistrations
            .FirstOrDefaultAsync(r => r.ApiKeyHash == hash && r.IsActive);
    }

    private static string ComputeSha256Hex(string input)
    {
        var inputBytes = Encoding.UTF8.GetBytes(input);
        var hashBytes = SHA256.HashData(inputBytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
