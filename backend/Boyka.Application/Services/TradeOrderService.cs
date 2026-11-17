using Boyka.Application.DTOs.Trades;
using Boyka.Application.Interfaces;
using Boyka.Domain.Entities;
using Boyka.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Boyka.Application.Services;

public class TradeOrderService : ITradeOrderService
{
    private readonly IBoykaDbContext _context;
    private readonly IMockTradeExecutionService _mockExecutionService;
    private readonly IMT5CommandService _mt5CommandService;

    public TradeOrderService(
        IBoykaDbContext context,
        IMockTradeExecutionService mockExecutionService,
        IMT5CommandService mt5CommandService)
    {
        _context = context;
        _mockExecutionService = mockExecutionService;
        _mt5CommandService = mt5CommandService;
    }

    public async Task<IEnumerable<TradeOrderResponse>> GetAllAsync()
    {
        var orders = await _context.TradeOrders.ToListAsync();
        return orders.Select(MapToResponse);
    }

    public async Task<TradeOrderResponse?> GetByIdAsync(Guid id)
    {
        var order = await _context.TradeOrders.FirstOrDefaultAsync(o => o.Id == id);
        return order is null ? null : MapToResponse(order);
    }

    public async Task<IEnumerable<TradeExecutionResponse>> GetExecutionsAsync(Guid orderId)
    {
        var executions = await _context.TradeExecutions
            .Where(e => e.TradeOrderId == orderId)
            .ToListAsync();
        return executions.Select(MapExecutionToResponse);
    }

    public async Task<TradeOrderResponse> ExecuteTradeAsync(ExecuteTradeRequest request)
    {
        // 1. Load account
        var account = await _context.TradingAccounts
            .FirstOrDefaultAsync(a => a.Id == request.TradingAccountId);

        if (account is null)
            throw new KeyNotFoundException($"Trading account {request.TradingAccountId} not found.");

        // 2. Account must be active
        if (!account.IsActive)
            throw new InvalidOperationException($"Trading account {account.Id} is not active.");

        // 3. Direction must be Buy or Sell
        if (request.Direction != TradeDirection.Buy && request.Direction != TradeDirection.Sell)
            throw new InvalidOperationException("Invalid trade direction.");

        // 4. Volume must be > 0
        if (request.Volume <= 0)
            throw new InvalidOperationException("Volume must be greater than zero.");

        // 5. Volume must not exceed 100
        if (request.Volume > 100)
            throw new InvalidOperationException("Volume exceeds the maximum allowed per order.");

        // 6. Prices must be positive
        if (request.EntryPrice <= 0 || request.StopLoss <= 0 || request.TakeProfit <= 0)
            throw new InvalidOperationException("Entry price, stop-loss, and take-profit must be greater than zero.");

        // 7. Directional SL/TP validation
        if (request.Direction == TradeDirection.Buy)
        {
            if (request.StopLoss >= request.EntryPrice || request.TakeProfit <= request.EntryPrice)
                throw new InvalidOperationException("Invalid stop-loss or take-profit for the given direction.");
        }
        else
        {
            if (request.StopLoss <= request.EntryPrice || request.TakeProfit >= request.EntryPrice)
                throw new InvalidOperationException("Invalid stop-loss or take-profit for the given direction.");
        }

        // 8. Risk profile checks
        var riskProfile = await _context.RiskProfiles
            .FirstOrDefaultAsync(r => r.TradingAccountId == request.TradingAccountId && r.IsEnabled);

        if (riskProfile is not null)
        {
            if (request.Volume > riskProfile.MaximumLotSize)
                throw new InvalidOperationException("Order volume exceeds the account's maximum lot size.");

            var openCount = await _context.TradeOrders.CountAsync(o =>
                o.TradingAccountId == account.Id &&
                (o.Status == TradeOrderStatus.Pending || o.Status == TradeOrderStatus.Accepted));

            if (openCount >= riskProfile.MaximumOpenPositions)
                throw new InvalidOperationException("Maximum open positions reached for this account.");
        }

        // 9. Create order with Accepted status
        var order = new TradeOrder
        {
            Id = Guid.NewGuid(),
            TradingAccountId = request.TradingAccountId,
            TradeSignalId = request.TradeSignalId,
            Symbol = request.Symbol,
            Direction = request.Direction,
            Volume = request.Volume,
            EntryPrice = request.EntryPrice,
            StopLoss = request.StopLoss,
            TakeProfit = request.TakeProfit,
            Status = TradeOrderStatus.Accepted,
            CreatedAt = DateTime.UtcNow
        };

        // 10. Persist order
        _context.TradeOrders.Add(order);
        await _context.SaveChangesAsync();

        // 11. Execute via mock service
        var execution = await _mockExecutionService.ExecuteAsync(order);
        execution.TradeOrderId = order.Id;

        // 12. Persist execution
        _context.TradeExecutions.Add(execution);
        await _context.SaveChangesAsync();

        // 13. Update order status based on execution outcome
        order.Status = execution.ExecutionStatus == TradeExecutionStatus.Success
            ? TradeOrderStatus.Executed
            : TradeOrderStatus.Rejected;

        // 14. Save final status
        await _context.SaveChangesAsync();

        // 15. Return response
        return MapToResponse(order);
    }

    public async Task<TradeOrderResponse> QueueForMT5Async(ExecuteTradeRequest request)
    {
        // 1. Load account
        var account = await _context.TradingAccounts
            .FirstOrDefaultAsync(a => a.Id == request.TradingAccountId);

        if (account is null)
            throw new KeyNotFoundException($"Trading account {request.TradingAccountId} not found.");

        // 2. Account must be active
        if (!account.IsActive)
            throw new InvalidOperationException($"Trading account {account.Id} is not active.");

        // 3. Direction must be Buy or Sell
        if (request.Direction != TradeDirection.Buy && request.Direction != TradeDirection.Sell)
            throw new InvalidOperationException("Invalid trade direction.");

        // 4. Volume must be > 0
        if (request.Volume <= 0)
            throw new InvalidOperationException("Volume must be greater than zero.");

        // 5. Volume must not exceed 100
        if (request.Volume > 100)
            throw new InvalidOperationException("Volume exceeds the maximum allowed per order.");

        // 6. Prices must be positive
        if (request.EntryPrice <= 0 || request.StopLoss <= 0 || request.TakeProfit <= 0)
            throw new InvalidOperationException("Entry price, stop-loss, and take-profit must be greater than zero.");

        // 7. Directional SL/TP validation
        if (request.Direction == TradeDirection.Buy)
        {
            if (request.StopLoss >= request.EntryPrice || request.TakeProfit <= request.EntryPrice)
                throw new InvalidOperationException("Invalid stop-loss or take-profit for the given direction.");
        }
        else
        {
            if (request.StopLoss <= request.EntryPrice || request.TakeProfit >= request.EntryPrice)
                throw new InvalidOperationException("Invalid stop-loss or take-profit for the given direction.");
        }

        // 8. Risk profile checks
        var riskProfile = await _context.RiskProfiles
            .FirstOrDefaultAsync(r => r.TradingAccountId == request.TradingAccountId && r.IsEnabled);

        if (riskProfile is not null)
        {
            if (request.Volume > riskProfile.MaximumLotSize)
                throw new InvalidOperationException("Order volume exceeds the account's maximum lot size.");

            var openCount = await _context.TradeOrders.CountAsync(o =>
                o.TradingAccountId == account.Id &&
                (o.Status == TradeOrderStatus.Pending || o.Status == TradeOrderStatus.Accepted));

            if (openCount >= riskProfile.MaximumOpenPositions)
                throw new InvalidOperationException("Maximum open positions reached for this account.");
        }

        // 9. Create order with Queued status
        var order = new TradeOrder
        {
            Id = Guid.NewGuid(),
            TradingAccountId = request.TradingAccountId,
            TradeSignalId = request.TradeSignalId,
            Symbol = request.Symbol,
            Direction = request.Direction,
            Volume = request.Volume,
            EntryPrice = request.EntryPrice,
            StopLoss = request.StopLoss,
            TakeProfit = request.TakeProfit,
            Status = TradeOrderStatus.Queued,
            CreatedAt = DateTime.UtcNow
        };

        _context.TradeOrders.Add(order);
        await _context.SaveChangesAsync();

        // 10. Resolve broker symbol via SymbolMapping
        var symbolMapping = await _context.SymbolMappings
            .FirstOrDefaultAsync(m => m.TradingAccountId == request.TradingAccountId
                                      && m.CanonicalSymbol == request.Symbol);

        var brokerSymbol = symbolMapping?.BrokerSymbol ?? request.Symbol;

        // 11. Queue the MT5 command
        var command = await _mt5CommandService.QueueCommandAsync(order, brokerSymbol);

        // 12. Store correlation ID on the order
        order.MT5CommandId = command.Id;
        await _context.SaveChangesAsync();

        return MapToResponse(order);
    }

    private static TradeOrderResponse MapToResponse(TradeOrder order) => new()
    {
        Id = order.Id,
        TradingAccountId = order.TradingAccountId,
        TradeSignalId = order.TradeSignalId,
        Symbol = order.Symbol,
        Direction = order.Direction,
        Volume = order.Volume,
        EntryPrice = order.EntryPrice,
        StopLoss = order.StopLoss,
        TakeProfit = order.TakeProfit,
        Status = order.Status,
        StatusName = order.Status.ToString(),
        CreatedAt = order.CreatedAt
    };

    private static TradeExecutionResponse MapExecutionToResponse(TradeExecution execution) => new()
    {
        Id = execution.Id,
        TradeOrderId = execution.TradeOrderId,
        MT5TicketNumber = execution.MT5TicketNumber,
        ExecutionPrice = execution.ExecutionPrice,
        ExecutedVolume = execution.ExecutedVolume,
        ExecutionStatus = execution.ExecutionStatus,
        ErrorMessage = execution.ErrorMessage,
        ExecutedAt = execution.ExecutedAt
    };
}
