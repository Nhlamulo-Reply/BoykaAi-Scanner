using Boyka.Application.DTOs.MT5;
using Boyka.Application.Services;
using Boyka.Domain.Entities;
using Boyka.Domain.Enums;
using Boyka.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Boyka.Tests;

public class MT5CommandServiceTests
{
    private static BoykaDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<BoykaDbContext>()
            .UseInMemoryDatabase("Mt5CommandTestDb_" + Guid.NewGuid())
            .Options;
        return new BoykaDbContext(options);
    }

    private static MT5CommandService BuildService(BoykaDbContext context)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MT5:CommandExpirySeconds"] = "60"
            })
            .Build();
        return new MT5CommandService(context, configuration);
    }

    private static MT5Command BuildCommand(Guid accountId, MT5CommandStatus status = MT5CommandStatus.Pending) => new()
    {
        Id = Guid.NewGuid(),
        TradeOrderId = Guid.NewGuid(),
        TradingAccountId = accountId,
        Symbol = "XAUUSD",
        Direction = TradeDirection.Buy,
        Volume = 0.01m,
        EntryPrice = 2500m,
        StopLoss = 2490m,
        TakeProfit = 2520m,
        Status = status,
        CreatedAt = DateTime.UtcNow,
        ExpiresAt = DateTime.UtcNow.AddMinutes(1),
        TradeOrder = new TradeOrder
        {
            Id = Guid.NewGuid(),
            TradingAccountId = accountId,
            Symbol = "XAUUSD",
            Direction = TradeDirection.Buy,
            Volume = 0.01m,
            EntryPrice = 2500m,
            StopLoss = 2490m,
            TakeProfit = 2520m,
            Status = TradeOrderStatus.Queued,
            CreatedAt = DateTime.UtcNow
        }
    };

    private static MT5AcknowledgeRequest FilledAcknowledgement(Guid accountId) => new()
    {
        AccountId = accountId,
        Status = "FILLED",
        BrokerTicket = 123456,
        Symbol = "XAUUSD",
        Direction = "BUY",
        RequestedVolume = 0.01m,
        ExecutedVolume = 0.01m,
        ExecutionPrice = 2501m,
        ExecutedAtUtc = DateTime.UtcNow
    };

    [Fact]
    public async Task GetNextPendingCommandAsync_DeliversOnlyTheAccountCommand()
    {
        await using var context = CreateContext();
        var accountA = Guid.NewGuid();
        var accountB = Guid.NewGuid();
        var commandA = BuildCommand(accountA);
        var commandB = BuildCommand(accountB);
        context.MT5Commands.AddRange(commandA, commandB);
        await context.SaveChangesAsync();

        var result = await BuildService(context).GetNextPendingCommandAsync(accountA);

        Assert.NotNull(result);
        Assert.Equal(commandA.Id, result.CommandId);
        Assert.Equal(MT5CommandStatus.Delivered, commandA.Status);
        Assert.Equal(MT5CommandStatus.Pending, commandB.Status);
    }

    [Fact]
    public async Task AcknowledgeCommandAsync_RepeatIsIdempotent()
    {
        await using var context = CreateContext();
        var accountId = Guid.NewGuid();
        var command = BuildCommand(accountId, MT5CommandStatus.Delivered);
        context.MT5Commands.Add(command);
        await context.SaveChangesAsync();
        var service = BuildService(context);
        var acknowledgement = FilledAcknowledgement(accountId);

        await service.AcknowledgeCommandAsync(command.Id, accountId, acknowledgement);
        await service.AcknowledgeCommandAsync(command.Id, accountId, acknowledgement);

        Assert.Equal(1, await context.TradeExecutions.CountAsync());
        Assert.Equal(MT5CommandStatus.Acknowledged, command.Status);
        Assert.Equal(TradeOrderStatus.Executed, command.TradeOrder!.Status);
    }

    [Fact]
    public async Task AcknowledgeCommandAsync_WrongAccountIsRejected()
    {
        await using var context = CreateContext();
        var accountId = Guid.NewGuid();
        var command = BuildCommand(accountId, MT5CommandStatus.Delivered);
        context.MT5Commands.Add(command);
        await context.SaveChangesAsync();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            BuildService(context).AcknowledgeCommandAsync(command.Id, Guid.NewGuid(), FilledAcknowledgement(accountId)));
    }

    [Fact]
    public async Task AcknowledgeCommandAsync_InvalidStatusIsRejectedWithoutExecution()
    {
        await using var context = CreateContext();
        var accountId = Guid.NewGuid();
        var command = BuildCommand(accountId, MT5CommandStatus.Delivered);
        context.MT5Commands.Add(command);
        await context.SaveChangesAsync();
        var acknowledgement = FilledAcknowledgement(accountId);
        acknowledgement.Status = "NOT_A_STATUS";

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            BuildService(context).AcknowledgeCommandAsync(command.Id, accountId, acknowledgement));

        Assert.Equal(0, await context.TradeExecutions.CountAsync());
        Assert.Equal(MT5CommandStatus.Delivered, command.Status);
    }
}
