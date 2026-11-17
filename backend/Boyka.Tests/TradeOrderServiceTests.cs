using Boyka.Application.Interfaces;
using Boyka.Application.Services;
using Boyka.Domain.Entities;
using Boyka.Domain.Enums;
using Boyka.Infrastructure.Data;
using Boyka.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Boyka.Tests;

/// <summary>
/// Fake execution service that always returns Success, for deterministic tests.
/// </summary>
file class FakeExecutionService : IMockTradeExecutionService
{
    public Task<TradeExecution> ExecuteAsync(TradeOrder order)
    {
        var execution = new TradeExecution
        {
            Id = Guid.NewGuid(),
            TradeOrderId = order.Id,
            MT5TicketNumber = null,
            ExecutionPrice = order.EntryPrice,
            ExecutedVolume = order.Volume,
            ExecutionStatus = TradeExecutionStatus.Success,
            ExecutedAt = DateTime.UtcNow
        };
        return Task.FromResult(execution);
    }
}

public class TradeOrderServiceTests
{
    private static BoykaDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<BoykaDbContext>()
            .UseInMemoryDatabase("TestDb_" + Guid.NewGuid())
            .Options;
        return new BoykaDbContext(options);
    }

    private static TradeOrderService BuildService(BoykaDbContext context) =>
        new(context, new FakeExecutionService());

    private static Boyka.Application.DTOs.Trades.ExecuteTradeRequest BuildValidBuyRequest(Guid accountId) => new()
    {
        TradingAccountId = accountId,
        Symbol = "EURUSD",
        Direction = TradeDirection.Buy,
        Volume = 1.0m,
        EntryPrice = 1.1000m,
        StopLoss = 1.0900m,
        TakeProfit = 1.1200m
    };

    [Fact]
    public async Task ExecuteTradeAsync_AccountNotFound_ThrowsKeyNotFoundException()
    {
        await using var ctx = CreateContext();
        var svc = BuildService(ctx);
        var request = BuildValidBuyRequest(Guid.NewGuid());

        await Assert.ThrowsAsync<KeyNotFoundException>(() => svc.ExecuteTradeAsync(request));
    }

    [Fact]
    public async Task ExecuteTradeAsync_AccountInactive_ThrowsInvalidOperationException()
    {
        await using var ctx = CreateContext();
        var accountId = Guid.NewGuid();
        ctx.TradingAccounts.Add(new TradingAccount
        {
            Id = accountId,
            AccountName = "Test",
            BrokerName = "Broker",
            AccountNumber = "12345",
            ServerName = "Server",
            AccountCurrency = "USD",
            ExecutionMode = "Live",
            IsActive = false
        });
        await ctx.SaveChangesAsync();

        var svc = BuildService(ctx);
        var request = BuildValidBuyRequest(accountId);

        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.ExecuteTradeAsync(request));
    }

    [Fact]
    public async Task ExecuteTradeAsync_VolumeZero_ThrowsInvalidOperationException()
    {
        await using var ctx = CreateContext();
        var accountId = Guid.NewGuid();
        ctx.TradingAccounts.Add(new TradingAccount
        {
            Id = accountId,
            AccountName = "Test",
            BrokerName = "Broker",
            AccountNumber = "12345",
            ServerName = "Server",
            AccountCurrency = "USD",
            ExecutionMode = "Live",
            IsActive = true
        });
        await ctx.SaveChangesAsync();

        var svc = BuildService(ctx);
        var request = BuildValidBuyRequest(accountId);
        request.Volume = 0m;

        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.ExecuteTradeAsync(request));
    }

    [Fact]
    public async Task ExecuteTradeAsync_VolumeNegative_ThrowsInvalidOperationException()
    {
        await using var ctx = CreateContext();
        var accountId = Guid.NewGuid();
        ctx.TradingAccounts.Add(new TradingAccount
        {
            Id = accountId,
            AccountName = "Test",
            BrokerName = "Broker",
            AccountNumber = "12345",
            ServerName = "Server",
            AccountCurrency = "USD",
            ExecutionMode = "Live",
            IsActive = true
        });
        await ctx.SaveChangesAsync();

        var svc = BuildService(ctx);
        var request = BuildValidBuyRequest(accountId);
        request.Volume = -1m;

        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.ExecuteTradeAsync(request));
    }

    [Fact]
    public async Task ExecuteTradeAsync_BuyInvalidStopLoss_ThrowsInvalidOperationException()
    {
        await using var ctx = CreateContext();
        var accountId = Guid.NewGuid();
        ctx.TradingAccounts.Add(new TradingAccount
        {
            Id = accountId,
            AccountName = "Test",
            BrokerName = "Broker",
            AccountNumber = "12345",
            ServerName = "Server",
            AccountCurrency = "USD",
            ExecutionMode = "Live",
            IsActive = true
        });
        await ctx.SaveChangesAsync();

        var svc = BuildService(ctx);
        // BUY: SL >= EntryPrice is invalid
        var request = new Boyka.Application.DTOs.Trades.ExecuteTradeRequest
        {
            TradingAccountId = accountId,
            Symbol = "EURUSD",
            Direction = TradeDirection.Buy,
            Volume = 1.0m,
            EntryPrice = 1.0000m,
            StopLoss = 1.1000m,   // SL > EntryPrice — invalid for BUY
            TakeProfit = 1.2000m
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.ExecuteTradeAsync(request));
    }

    [Fact]
    public async Task ExecuteTradeAsync_SellInvalidStopLoss_ThrowsInvalidOperationException()
    {
        await using var ctx = CreateContext();
        var accountId = Guid.NewGuid();
        ctx.TradingAccounts.Add(new TradingAccount
        {
            Id = accountId,
            AccountName = "Test",
            BrokerName = "Broker",
            AccountNumber = "12345",
            ServerName = "Server",
            AccountCurrency = "USD",
            ExecutionMode = "Live",
            IsActive = true
        });
        await ctx.SaveChangesAsync();

        var svc = BuildService(ctx);
        // SELL: SL <= EntryPrice is invalid
        var request = new Boyka.Application.DTOs.Trades.ExecuteTradeRequest
        {
            TradingAccountId = accountId,
            Symbol = "EURUSD",
            Direction = TradeDirection.Sell,
            Volume = 1.0m,
            EntryPrice = 1.0000m,
            StopLoss = 0.9000m,   // SL < EntryPrice — invalid for SELL
            TakeProfit = 0.8000m
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.ExecuteTradeAsync(request));
    }

    [Fact]
    public async Task ExecuteTradeAsync_ValidOrder_ReturnsExecutedStatus()
    {
        await using var ctx = CreateContext();
        var accountId = Guid.NewGuid();
        ctx.TradingAccounts.Add(new TradingAccount
        {
            Id = accountId,
            AccountName = "Test",
            BrokerName = "Broker",
            AccountNumber = "12345",
            ServerName = "Server",
            AccountCurrency = "USD",
            ExecutionMode = "Live",
            IsActive = true
        });
        await ctx.SaveChangesAsync();

        var svc = BuildService(ctx);
        var request = new Boyka.Application.DTOs.Trades.ExecuteTradeRequest
        {
            TradingAccountId = accountId,
            Symbol = "EURUSD",
            Direction = TradeDirection.Buy,
            Volume = 0.1m,
            EntryPrice = 1.1000m,
            StopLoss = 1.0900m,   // strictly below entry for BUY
            TakeProfit = 1.1200m  // strictly above entry for BUY
        };

        var response = await svc.ExecuteTradeAsync(request);

        Assert.Equal(TradeOrderStatus.Executed, response.Status);
    }

    [Fact]
    public async Task ExecuteTradeAsync_ExceedsMaxLotSize_ThrowsInvalidOperationException()
    {
        await using var ctx = CreateContext();
        var accountId = Guid.NewGuid();
        ctx.TradingAccounts.Add(new TradingAccount
        {
            Id = accountId,
            AccountName = "Test",
            BrokerName = "Broker",
            AccountNumber = "12345",
            ServerName = "Server",
            AccountCurrency = "USD",
            ExecutionMode = "Live",
            IsActive = true
        });
        ctx.RiskProfiles.Add(new RiskProfile
        {
            Id = Guid.NewGuid(),
            TradingAccountId = accountId,
            IsEnabled = true,
            MaximumLotSize = 0.5m,
            MaximumOpenPositions = 10,
            RiskPercentage = 1.0m,
            MaximumDailyLoss = 100.0m
        });
        await ctx.SaveChangesAsync();

        var svc = BuildService(ctx);
        var request = new Boyka.Application.DTOs.Trades.ExecuteTradeRequest
        {
            TradingAccountId = accountId,
            Symbol = "EURUSD",
            Direction = TradeDirection.Buy,
            Volume = 1.0m,  // exceeds MaximumLotSize of 0.5
            EntryPrice = 1.1000m,
            StopLoss = 1.0900m,
            TakeProfit = 1.1200m
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.ExecuteTradeAsync(request));
    }

    [Fact]
    public async Task ExecuteTradeAsync_ExceedsMaxOpenPositions_ThrowsInvalidOperationException()
    {
        await using var ctx = CreateContext();
        var accountId = Guid.NewGuid();
        ctx.TradingAccounts.Add(new TradingAccount
        {
            Id = accountId,
            AccountName = "Test",
            BrokerName = "Broker",
            AccountNumber = "12345",
            ServerName = "Server",
            AccountCurrency = "USD",
            ExecutionMode = "Live",
            IsActive = true
        });
        ctx.RiskProfiles.Add(new RiskProfile
        {
            Id = Guid.NewGuid(),
            TradingAccountId = accountId,
            IsEnabled = true,
            MaximumLotSize = 10.0m,         // high, so volume won't block
            MaximumOpenPositions = 1,
            RiskPercentage = 1.0m,
            MaximumDailyLoss = 100.0m
        });
        // One existing open order (Pending counts as open)
        ctx.TradeOrders.Add(new TradeOrder
        {
            Id = Guid.NewGuid(),
            TradingAccountId = accountId,
            Symbol = "EURUSD",
            Direction = TradeDirection.Buy,
            Volume = 0.1m,
            EntryPrice = 1.1000m,
            StopLoss = 1.0900m,
            TakeProfit = 1.1200m,
            Status = TradeOrderStatus.Pending,
            CreatedAt = DateTime.UtcNow
        });
        await ctx.SaveChangesAsync();

        var svc = BuildService(ctx);
        var request = new Boyka.Application.DTOs.Trades.ExecuteTradeRequest
        {
            TradingAccountId = accountId,
            Symbol = "EURUSD",
            Direction = TradeDirection.Buy,
            Volume = 0.1m,
            EntryPrice = 1.1000m,
            StopLoss = 1.0900m,
            TakeProfit = 1.1200m
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.ExecuteTradeAsync(request));
    }
}
