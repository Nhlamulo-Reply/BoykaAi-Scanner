using Boyka.Application.DTOs.Accounts;
using Boyka.Application.Interfaces;
using Boyka.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Boyka.Application.Services;

public class TradingAccountService : ITradingAccountService
{
    private readonly IBoykaDbContext _context;

    public TradingAccountService(IBoykaDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<TradingAccountResponse>> GetAllAsync()
    {
        var accounts = await _context.TradingAccounts.ToListAsync();
        return accounts.Select(MapToResponse);
    }

    public async Task<TradingAccountResponse?> GetByIdAsync(Guid id)
    {
        var account = await _context.TradingAccounts.FirstOrDefaultAsync(a => a.Id == id);
        return account is null ? null : MapToResponse(account);
    }

    public async Task<TradingAccountResponse> CreateAsync(CreateTradingAccountRequest request)
    {
        var account = new TradingAccount
        {
            Id = Guid.NewGuid(),
            AccountName = request.AccountName,
            BrokerName = request.BrokerName,
            AccountNumber = request.AccountNumber,
            ServerName = request.ServerName,
            AccountCurrency = request.AccountCurrency,
            IsDemo = request.IsDemo,
            ExecutionMode = request.ExecutionMode,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.TradingAccounts.Add(account);
        await _context.SaveChangesAsync();
        return MapToResponse(account);
    }

    private static TradingAccountResponse MapToResponse(TradingAccount account) => new()
    {
        Id = account.Id,
        AccountName = account.AccountName,
        BrokerName = account.BrokerName,
        AccountNumber = account.AccountNumber,
        ServerName = account.ServerName,
        AccountCurrency = account.AccountCurrency,
        IsDemo = account.IsDemo,
        ExecutionMode = account.ExecutionMode,
        IsActive = account.IsActive,
        CreatedAt = account.CreatedAt
    };
}
