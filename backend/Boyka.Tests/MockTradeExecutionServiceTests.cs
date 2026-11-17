using Boyka.Domain.Entities;
using Boyka.Domain.Enums;
using Boyka.Infrastructure.Services;

namespace Boyka.Tests;

public class MockTradeExecutionServiceTests
{
    private static TradeOrder BuildOrder() => new()
    {
        Id = Guid.NewGuid(),
        TradingAccountId = Guid.NewGuid(),
        Symbol = "EURUSD",
        Direction = TradeDirection.Buy,
        Volume = 1.0m,
        EntryPrice = 1.1000m,
        StopLoss = 1.0900m,
        TakeProfit = 1.1200m,
        Status = TradeOrderStatus.Accepted,
        CreatedAt = DateTime.UtcNow
    };

    [Fact]
    public async Task ExecuteAsync_MT5TicketNumberIsNull()
    {
        var service = new MockTradeExecutionService();
        var result = await service.ExecuteAsync(BuildOrder());
        Assert.Null(result.MT5TicketNumber);
    }

    [Fact]
    public async Task ExecuteAsync_ExecutionPriceEqualsEntryPrice()
    {
        var service = new MockTradeExecutionService();
        var order = BuildOrder();
        var result = await service.ExecuteAsync(order);
        Assert.Equal(order.EntryPrice, result.ExecutionPrice);
    }

    [Fact]
    public async Task ExecuteAsync_ExecutedVolumeEqualsOrderVolume()
    {
        var service = new MockTradeExecutionService();
        var order = BuildOrder();
        var result = await service.ExecuteAsync(order);
        Assert.Equal(order.Volume, result.ExecutedVolume);
    }

    [Fact]
    public async Task ExecuteAsync_StatusIsSuccessOrFailed()
    {
        var service = new MockTradeExecutionService();
        var order = BuildOrder();
        for (int i = 0; i < 50; i++)
        {
            var result = await service.ExecuteAsync(order);
            Assert.True(
                result.ExecutionStatus == TradeExecutionStatus.Success ||
                result.ExecutionStatus == TradeExecutionStatus.Failed,
                $"Unexpected status: {result.ExecutionStatus}");
        }
    }
}
