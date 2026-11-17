using Boyka.Application.DTOs.Trades;

namespace Boyka.Application.Interfaces;

public interface ITradeOrderService
{
    Task<IEnumerable<TradeOrderResponse>> GetAllAsync();
    Task<TradeOrderResponse?> GetByIdAsync(Guid id);
    Task<IEnumerable<TradeExecutionResponse>> GetExecutionsAsync(Guid orderId);
    Task<TradeOrderResponse> ExecuteTradeAsync(ExecuteTradeRequest request);
}
