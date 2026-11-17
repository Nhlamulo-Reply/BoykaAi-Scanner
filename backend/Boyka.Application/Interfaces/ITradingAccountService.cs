using Boyka.Application.DTOs.Accounts;

namespace Boyka.Application.Interfaces;

public interface ITradingAccountService
{
    Task<IEnumerable<TradingAccountResponse>> GetAllAsync();
    Task<TradingAccountResponse?> GetByIdAsync(Guid id);
    Task<TradingAccountResponse> CreateAsync(CreateTradingAccountRequest request);
}
