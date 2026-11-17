using Boyka.Application.Interfaces;
using Boyka.Domain.Entities;
using Boyka.Domain.Enums;

namespace Boyka.Infrastructure.Services;

/// <summary>
/// Mock trade execution service. Does not connect to any real broker or MT5 instance.
/// </summary>
public class MockTradeExecutionService : IMockTradeExecutionService
{
    private static readonly Random _rng = new();

    public Task<TradeExecution> ExecuteAsync(TradeOrder order)
    {
        var execution = new TradeExecution
        {
            Id = Guid.NewGuid(),
            TradeOrderId = order.Id,
            MT5TicketNumber = null,
            ExecutionPrice = order.EntryPrice,
            ExecutedVolume = order.Volume,
            ExecutedAt = DateTime.UtcNow
        };

        if (_rng.NextDouble() < 0.9)
        {
            execution.ExecutionStatus = TradeExecutionStatus.Success;
        }
        else
        {
            execution.ExecutionStatus = TradeExecutionStatus.Failed;
            execution.ErrorMessage = "[MOCK] Simulated execution failure.";
        }

        return Task.FromResult(execution);
    }
}
