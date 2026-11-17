using Boyka.Application.DTOs.Signals;
using Boyka.Application.Interfaces;
using Boyka.Domain.Entities;
using Boyka.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Boyka.Application.Services;

public class TradeSignalService : ITradeSignalService
{
    private readonly IBoykaDbContext _context;

    public TradeSignalService(IBoykaDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<TradeSignalResponse>> GetAllAsync()
    {
        var signals = await _context.TradeSignals.ToListAsync();
        return signals.Select(MapToResponse);
    }

    public async Task<TradeSignalResponse?> GetByIdAsync(Guid id)
    {
        var signal = await _context.TradeSignals.FirstOrDefaultAsync(s => s.Id == id);
        return signal is null ? null : MapToResponse(signal);
    }

    public async Task<TradeSignalResponse> CreateAsync(CreateTradeSignalRequest request)
    {
        var signal = new TradeSignal
        {
            Id = Guid.NewGuid(),
            Symbol = request.Symbol,
            Direction = request.Direction,
            EntryPrice = request.EntryPrice,
            StopLoss = request.StopLoss,
            TakeProfit = request.TakeProfit,
            Status = TradeSignalStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _context.TradeSignals.Add(signal);
        await _context.SaveChangesAsync();
        return MapToResponse(signal);
    }

    private static TradeSignalResponse MapToResponse(TradeSignal signal) => new()
    {
        Id = signal.Id,
        Symbol = signal.Symbol,
        Direction = signal.Direction,
        EntryPrice = signal.EntryPrice,
        StopLoss = signal.StopLoss,
        TakeProfit = signal.TakeProfit,
        Status = signal.Status,
        CreatedAt = signal.CreatedAt
    };
}
