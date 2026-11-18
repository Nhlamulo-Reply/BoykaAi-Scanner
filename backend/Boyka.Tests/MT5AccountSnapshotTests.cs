using Boyka.API.Controllers;
using Boyka.Application.DTOs.MT5;
using Boyka.Application.Interfaces;
using Boyka.Domain.Entities;
using Boyka.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text.Json;

namespace Boyka.Tests;

public class MT5AccountSnapshotTests
{
    private static BoykaDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<BoykaDbContext>()
            .UseInMemoryDatabase("Mt5AccountSnapshotTestDb_" + Guid.NewGuid())
            .Options;
        return new BoykaDbContext(options);
    }

    private static MT5Controller BuildController(BoykaDbContext context, EaRegistration registration)
    {
        var controller = new MT5Controller(
            new NoOpRegistrationService(),
            new NoOpCommandService(),
            context,
            NullLogger<MT5Controller>.Instance)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
        controller.HttpContext.Items["EaRegistration"] = registration;
        return controller;
    }

    private static MT5AccountSnapshotRequest BuildRequest(long accountLogin) => new()
    {
        AccountLogin = accountLogin,
        BrokerCompany = "XM",
        ServerName = "XMGlobal-MT5 10",
        Currency = "ZAR",
        Balance = 1_000m,
        Equity = 1_001m,
        Margin = 20m,
        FreeMargin = 981m,
        Leverage = 1000
    };

    [Fact]
    public void AccountSnapshotRequest_SerializesWithEaJsonPropertyNames()
    {
        var json = JsonSerializer.Serialize(
            BuildRequest(346585694),
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        Assert.Contains("\"accountLogin\":346585694", json);
        Assert.Contains("\"brokerCompany\":\"XM\"", json);
        Assert.Contains("\"serverName\":\"XMGlobal-MT5 10\"", json);
        Assert.Contains("\"currency\":\"ZAR\"", json);
    }

    [Fact]
    public async Task AccountSnapshot_MatchingAuthenticatedAccount_ReturnsOk()
    {
        await using var context = CreateContext();
        var accountId = Guid.NewGuid();
        context.TradingAccounts.Add(new TradingAccount
        {
            Id = accountId,
            AccountName = "XM Demo",
            BrokerName = "XM",
            AccountNumber = "346585694",
            ServerName = "XMGlobal-MT5 10",
            AccountCurrency = "ZAR",
            ExecutionMode = "Demo"
        });
        await context.SaveChangesAsync();

        var result = await BuildController(context, new EaRegistration { TradingAccountId = accountId })
            .AccountSnapshot(BuildRequest(346585694));

        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task AccountSnapshot_MismatchedLogin_IsRejected()
    {
        await using var context = CreateContext();
        var accountId = Guid.NewGuid();
        context.TradingAccounts.Add(new TradingAccount
        {
            Id = accountId,
            AccountName = "XM Demo",
            BrokerName = "XM",
            AccountNumber = "346585694",
            ServerName = "XMGlobal-MT5 10",
            AccountCurrency = "ZAR",
            ExecutionMode = "Demo"
        });
        await context.SaveChangesAsync();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            BuildController(context, new EaRegistration { TradingAccountId = accountId })
                .AccountSnapshot(BuildRequest(999999999)));
    }

    private sealed class NoOpRegistrationService : IEaRegistrationService
    {
        public Task<ProvisionEaResponse> ProvisionAsync(ProvisionEaRequest request) => throw new NotSupportedException();
        public Task RevokeAsync(Guid registrationId) => throw new NotSupportedException();
        public Task<EaRegistration?> ValidateApiKeyAsync(string rawApiKey) => throw new NotSupportedException();
    }

    private sealed class NoOpCommandService : IMT5CommandService
    {
        public Task<MT5Command> QueueCommandAsync(TradeOrder order, string brokerSymbol) => throw new NotSupportedException();
        public Task<MT5CommandResponse?> GetNextPendingCommandAsync(Guid tradingAccountId) => throw new NotSupportedException();
        public Task AcknowledgeCommandAsync(Guid commandId, Guid tradingAccountId, MT5AcknowledgeRequest request) => throw new NotSupportedException();
        public Task ExpireStaleCommandsAsync() => throw new NotSupportedException();
    }
}
