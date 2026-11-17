using Boyka.Application.DTOs.MT5;
using Boyka.Domain.Entities;

namespace Boyka.Application.Interfaces;

public interface IMT5CommandService
{
    Task<MT5Command> QueueCommandAsync(TradeOrder order, string brokerSymbol);
    Task<MT5CommandResponse?> GetNextPendingCommandAsync(Guid tradingAccountId);
    Task AcknowledgeCommandAsync(Guid commandId, Guid tradingAccountId, MT5AcknowledgeRequest request);
    Task ExpireStaleCommandsAsync();
}
