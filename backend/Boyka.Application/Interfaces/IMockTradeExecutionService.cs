using Boyka.Domain.Entities;

namespace Boyka.Application.Interfaces;

public interface IMockTradeExecutionService
{
    Task<TradeExecution> ExecuteAsync(TradeOrder order);
}
