using Boyka.Application.DTOs.MT5;
using Boyka.Application.Interfaces;
using Boyka.Domain.Entities;
using Boyka.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Boyka.Application.Services;

public class MT5CommandService : IMT5CommandService
{
    private readonly IBoykaDbContext _context;
    private readonly int _expirySeconds;

    public MT5CommandService(IBoykaDbContext context, IConfiguration configuration)
    {
        _context = context;
        _expirySeconds = configuration.GetValue<int>("MT5:CommandExpirySeconds", 60);
    }

    public async Task<MT5Command> QueueCommandAsync(TradeOrder order, string brokerSymbol)
    {
        var command = new MT5Command
        {
            Id = Guid.NewGuid(),
            TradeOrderId = order.Id,
            TradingAccountId = order.TradingAccountId,
            Symbol = brokerSymbol,
            Direction = order.Direction,
            Volume = order.Volume,
            EntryPrice = order.EntryPrice,
            StopLoss = order.StopLoss,
            TakeProfit = order.TakeProfit,
            Status = MT5CommandStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddSeconds(_expirySeconds)
        };

        _context.MT5Commands.Add(command);
        await _context.SaveChangesAsync();

        return command;
    }

    public async Task<MT5CommandResponse?> GetNextPendingCommandAsync(Guid tradingAccountId)
    {
        var command = await _context.MT5Commands
            .Where(c => c.TradingAccountId == tradingAccountId
                        && c.Status == MT5CommandStatus.Pending
                        && c.ExpiresAt > DateTime.UtcNow)
            .OrderBy(c => c.CreatedAt)
            .FirstOrDefaultAsync();

        if (command is null)
            return null;

        command.Status = MT5CommandStatus.Delivered;
        await _context.SaveChangesAsync();

        return MapToResponse(command);
    }

    public async Task AcknowledgeCommandAsync(Guid commandId, Guid tradingAccountId, MT5AcknowledgeRequest request)
    {
        var command = await _context.MT5Commands
            .Include(c => c.TradeOrder)
            .FirstOrDefaultAsync(c => c.Id == commandId);

        if (command is null)
            throw new KeyNotFoundException($"MT5 command {commandId} not found.");

        if (command.TradingAccountId != tradingAccountId)
            throw new UnauthorizedAccessException("MT5 command does not belong to this trading account.");

        var (executionStatus, orderStatus) = MapStatus(request.Status);

        var execution = new TradeExecution
        {
            Id = Guid.NewGuid(),
            TradeOrderId = command.TradeOrderId,
            MT5TicketNumber = request.BrokerTicket,
            ExecutionPrice = request.ExecutionPrice,
            ExecutedVolume = request.ExecutedVolume,
            ExecutionStatus = executionStatus,
            ErrorMessage = request.ErrorMessage,
            ExecutedAt = request.ExecutedAtUtc ?? DateTime.UtcNow
        };

        _context.TradeExecutions.Add(execution);

        if (command.TradeOrder is not null)
            command.TradeOrder.Status = orderStatus;

        command.Status = MT5CommandStatus.Acknowledged;
        command.AcknowledgedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    public async Task ExpireStaleCommandsAsync()
    {
        var stale = await _context.MT5Commands
            .Include(c => c.TradeOrder)
            .Where(c => (c.Status == MT5CommandStatus.Pending || c.Status == MT5CommandStatus.Delivered)
                        && c.ExpiresAt < DateTime.UtcNow)
            .ToListAsync();

        foreach (var command in stale)
        {
            command.Status = MT5CommandStatus.Expired;

            if (command.TradeOrder is not null)
                command.TradeOrder.Status = TradeOrderStatus.Rejected;
        }

        if (stale.Count > 0)
            await _context.SaveChangesAsync();
    }

    private static (TradeExecutionStatus executionStatus, TradeOrderStatus orderStatus) MapStatus(string status)
        => status.ToUpperInvariant() switch
        {
            "FILLED"   => (TradeExecutionStatus.Success,         TradeOrderStatus.Executed),
            "PARTIAL"  => (TradeExecutionStatus.PartiallyFilled, TradeOrderStatus.Executed),
            "REJECTED" => (TradeExecutionStatus.Rejected,        TradeOrderStatus.Rejected),
            "FAILED"   => (TradeExecutionStatus.Failed,          TradeOrderStatus.Rejected),
            _          => (TradeExecutionStatus.Unknown,         TradeOrderStatus.Rejected)
        };

    private static MT5CommandResponse MapToResponse(MT5Command command) => new()
    {
        CommandId    = command.Id,
        AccountId    = command.TradingAccountId,
        Symbol       = command.Symbol,
        Direction    = command.Direction,
        Volume       = command.Volume,
        EntryPrice   = command.EntryPrice,
        StopLoss     = command.StopLoss,
        TakeProfit   = command.TakeProfit,
        CreatedAtUtc = command.CreatedAt,
        ExpiresAtUtc = command.ExpiresAt
    };
}
