using Boyka.Application.DTOs.Signals;

namespace Boyka.Application.Interfaces;

public interface ITradeSignalService
{
    Task<IEnumerable<TradeSignalResponse>> GetAllAsync();
    Task<TradeSignalResponse?> GetByIdAsync(Guid id);
    Task<TradeSignalResponse> CreateAsync(CreateTradeSignalRequest request);
}
